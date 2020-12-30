using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.Viewer;
using pdftron.PDF;
using pdftron.SDF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;

namespace CompleteReader.ViewModels.FileOpening
{
    public enum OpenedDocumentStates
    {
        /// <summary>
        /// The default state. Should never be in this state when a document is passed to the Viewer
        /// </summary>
        None,
        /// <summary>
        /// Normal document, can read/write.
        /// </summary>
        Normal,
        /// <summary>
        /// This document can be saved to, but requires a full save (mostly for dropbox).
        /// </summary>
        NeedsFullSave,
        /// <summary>
        /// This is a corrupt document, the user will have to save it manually
        /// </summary>
        Corrupted,
        /// <summary>
        /// The user has edited a corrupt document. This means we need to notify them when they try to navigate away.
        /// </summary>
        CorruptedAndModified,
        /// <summary>
        /// This is a read only document. If it is modified, we should prompt the user to save it.
        /// </summary>
        ReadOnly,
        /// <summary>
        /// This is a document created by the app, we should always prompt the user to save it.
        /// </summary>
        Created,
        /// <summary>
        /// This is a non-pdf document, so we can't save back to it.
        /// </summary>
        NonePDF,
		/// <summary>
        /// This is a state in which we do not allow the document to be edited at all.
        /// This because we could not make a copy of it.
        /// </summary>
        Uneditable,
        /// <summary>
        /// This state signifies that we are opening a universal document.
        /// </summary>
        Universal,
    }

    public class NewDocumentProperties
    {
        public StorageFile File { get; set; }
        public StorageFile TemporaryFile { get; set; }
        public StorageFile OriginalFileFromSystem { get; set; }
        public IRandomAccessStream FileStream { get; set; }
        public PDFDoc Doc { get; set; }
        public OpenedDocumentStates OpenedDocumentState { get; set; }
        public string MRUToken { get; set; }
        public string Password { get; set; }
        public int StartPage { get; set; }
        private PDFViewCtrlPagePresentationMode _PresentationMode = Settings.Settings.Defaults.PagePresentationMode;
        public PDFViewCtrlPagePresentationMode PresentationMode
        {
            get { return _PresentationMode; }
            set { _PresentationMode = value; }
        }

        public bool IsReflow { get; set; }

        private pdftron.PDF.PageRotate _PageRotation = Settings.Settings.Defaults.PageRotation;
        public pdftron.PDF.PageRotate PageRotation
        {
            get { return _PageRotation; }
            set { _PageRotation = value; }
        }
        public double Zoom { get; set; }
        public double HorizontalScrollPosition { get; set; }
        public double VerticalScrollPosition { get; set; }

        private DateTimeOffset _LastModifiedDate = DateTimeOffset.MinValue;
        public DateTimeOffset LastModifiedDate
        {
            get { return _LastModifiedDate; }
            set { _LastModifiedDate = value; }
        }
        public ulong Filesize { get; set; }

        public bool OpenedThroughDrop { get; set; }

        public bool RecentPropertiesRestored { get; set; }

        public NewDocumentProperties()
        {
            OpenedDocumentState = OpenedDocumentStates.None;
            MRUToken = "";
            Password = "";
            StartPage = 1;

            PresentationMode = Settings.Settings.PagePresentationMode;
        }

        public void ApplyRecentProperties(Collections.RecentDocumentProperties recentFileProperties)
        {
            StartPage = recentFileProperties.PageNumber;
            PageRotation = recentFileProperties.PageRotation;
            PresentationMode = recentFileProperties.PagePresentationMode;
            Zoom = recentFileProperties.Zoom;
            HorizontalScrollPosition = recentFileProperties.HorizontalScrollPos;
            VerticalScrollPosition = recentFileProperties.VerticalScrollPos;
            IsReflow = recentFileProperties.IsInReflowMode;
            RecentPropertiesRestored = true;
        }
    }

    public class DocumentOpener : ViewModelBase
    {
        private static DocumentOpener _Current = null;
        private static DocumentOpener Current
        {
            get { return _Current; }
            set
            {
                if (_Current != value)
                {
                    if (_Current != null)
                    {
                        _Current.CancelConversionCommandImpl(null);
                    }
                    _Current = value;
                }
            }
        }

        public delegate void DocumentReadyDelegate(NewDocumentProperties selectedFileProperties);

        /// <summary>
        /// Raised when a document is ready to be opened.
        /// </summary>
        public event DocumentReadyDelegate DocumentReady;

        /// <summary>
        /// Raised when a document couldn't be opened.
        /// </summary>
        public event Windows.UI.Xaml.RoutedEventHandler DocumentLoadingFailed;

        public delegate void ModalityChangedDelegate(bool isModal);

        /// <summary>
        /// Raised when the modality is changed.
        /// </summary>
        public event ModalityChangedDelegate ModalityChanged;

        public DocumentOpener()
        {
            InitCommands();
            Current = this;
        }


        #region Public Interface

        public void CancelDocumentOpening()
        {
            CancelCurrentDocLoad();
            CancelConversionCommandImpl(null);
        }

        public void Open(StorageFile file, NewDocumentProperties documentProperties = null)
        {
            OpenInternal(file, documentProperties);
        }

        #endregion Public InterFace


        #region Properties

        private bool _IsModal = false;
        // gets or sets whether the current dialog is modal or not.
        public bool IsModal
        {
            get { return _IsModal; }
            set
            {
                if (Set(ref _IsModal, value))
                {
                    if (ModalityChanged != null)
                    {
                        ModalityChanged(_IsModal);
                    }
                }
            }
        }

        private bool _IsInputDisabled = false;
        public bool IsInputDisabled
        {
            get { return _IsInputDisabled; }
            set { Set(ref _IsInputDisabled, value); }
        }

        private bool _IsFileOpeningProgressBarVisible = false;
        public bool IsFileOpeningProgressBarVisible
        {
            get { return _IsFileOpeningProgressBarVisible; }
            set { Set(ref _IsFileOpeningProgressBarVisible, value); }
        }

        private bool _IsPasswordDialogOpen = false;
        public bool IsPasswordDialogOpen
        {
            get { return _IsPasswordDialogOpen; }
            set
            {
                Set(ref _IsPasswordDialogOpen, value);
                IsModal = value;
            }
        }

        private bool _IsPackageDialogOpen = false;
        public bool IsPackageDialogOpen
        {
            get { return _IsPackageDialogOpen; }
            set
            {
                Set(ref _IsPackageDialogOpen, value);
                IsModal = value;
            }
        }

        private bool _IsConvertingDocument = false;
        /// <summary>
        /// Gets or sets whether a document is currently being converted. Setting this to false when it was true will cancel conversion.
        /// </summary>
        public bool IsConvertingDocument
        {
            get { return _IsConvertingDocument; }
            set
            {
                if (value != _IsConvertingDocument)
                {
                    _IsConvertingDocument = value;
                    RaisePropertyChanged();
                    if (!_IsConvertingDocument)
                    {
                        CancelCurrentConversion();
                    }
                    IsModal = value;
                }
            }
        }

        #endregion Properties


        #region Commands

        private void InitCommands()
        {
            PackageDialogBackCommand = new RelayCommand(PackageDialogBackCommandImpl);
            CancelConversionCommand = new RelayCommand(CancelConversionCommandImpl);
        }

        public RelayCommand PackageDialogBackCommand { get; private set; }
        public RelayCommand CancelConversionCommand { get; private set; }

        private void PackageDialogBackCommandImpl(object sender)
        {
            PackageDialogGoBack();
        }

        private void CancelConversionCommandImpl(object sender)
        {
            IsConvertingDocument = false;
        }

        #endregion Commands


        #region Opening Document

        private FileOpeningCanceler _CurrentFileOpeningCanceler;
        private NewDocumentProperties _SelectedFileProperties = null;

        // This lets us pass a cancel flag as reference.
        private class FileOpeningCanceler
        {
            public bool Cancel = false;
            public StorageFile File;
        }

        private async void OpenInternal(StorageFile file, NewDocumentProperties documentProperties = null) 
        {
            if (IsAlreadyOpeningfile(file))
            {
                return;
            }
            if (!file.FileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                if (file.FileType.Equals(".oxps", StringComparison.OrdinalIgnoreCase) 
                    || file.FileType.Equals(".xps", StringComparison.OrdinalIgnoreCase)
                    || file.FileType.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                    || file.FileType.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                    || file.FileType.Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    OpenNonPDFPreConvert(file);
                }
                else
                {
                    OpenNonPDFUniversalConversion(file);
                }
                return;
            }

            if (documentProperties == null)
            {
                _SelectedFileProperties = new NewDocumentProperties();
                _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.Normal;
            }
            else
            {
                _SelectedFileProperties = documentProperties;
            }
            if (_SelectedFileProperties.OpenedDocumentState == OpenedDocumentStates.None)
            {
                _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.Normal;
            }

            CancelCurrentDocLoad();
            FileOpeningCanceler canceler = new FileOpeningCanceler();
            canceler.File = file;
            _CurrentFileOpeningCanceler = canceler;
            StartProgressBarAfterWait(canceler);

            if ((file.Attributes & FileAttributes.Temporary) == FileAttributes.Temporary)
            {
                _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.ReadOnly;
                StorageFile temporaryFile = await UtilityFunctions.GetTemporarySaveFileAsync(file.DisplayName);
                if (temporaryFile == null)
                {
                    Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    MessageDialog md = new MessageDialog(loader.GetString("DocumentsPage_FileOpeningError_FailedToCreateTempFile"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                    RaiseDocumentLoadingFailed();
                    return;
                }
                await file.CopyAndReplaceAsync(temporaryFile);
                file = temporaryFile;
                await UtilityFunctions.RemoveReadOnlyAndTemporaryFlagsAsync(file);
            }
            else if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly && !_SelectedFileProperties.OpenedThroughDrop)
            {
                _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.ReadOnly;
                StorageFile temporaryFile = await UtilityFunctions.GetTemporarySaveFileAsync(file.DisplayName);
                if (temporaryFile == null)
                {
                    Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    MessageDialog md = new MessageDialog(loader.GetString("DocumentsPage_FileOpeningError_FailedToCreateTempFile"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                    RaiseDocumentLoadingFailed();
                    return;
                }
                await file.CopyAndReplaceAsync(temporaryFile);
                file = temporaryFile;
                await UtilityFunctions.RemoveReadOnlyAndTemporaryFlagsAsync(file);
            }
            else if (_SelectedFileProperties.OpenedThroughDrop)
            {
                _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.ReadOnly;
            }

            if (file != null)
            {
                _SelectedFileProperties.File = file;
                _SelectedFileProperties.Password = null;
                _SelectedFileProperties.MRUToken = null;

                if (!_SelectedFileProperties.RecentPropertiesRestored && 
                    _SelectedFileProperties.OpenedDocumentState == OpenedDocumentStates.Normal
                    && Collections.RecentItemsData.IsReady)
                {
                    Collections.RecentDocumentProperties recentProps = await Collections.RecentItemsData.Instance.GetPropertiesForFileIfInList(file);
                    if (recentProps != null)
                    {
                        _SelectedFileProperties.ApplyRecentProperties(recentProps);
                    }
                }

                DocumentManager docManager = DocumentManager.Instance;
                if (!DocumentManager.IsReady)
                {
                    CompleteReader.Utilities.AnalyticsHandler.CURRENT.SendEvent(CompleteReader.Utilities.AnalyticsHandler.Category.FILEBROWSER,
                        "DocumentOpener invoked without DocumentManager ready");
                    docManager = await DocumentManager.GetInstanceAsync();
                }
                StorageFile tempFile = await docManager.OpenTemporaryCopyAsync(file);
                if (tempFile == null)
                {
                    _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.Uneditable;
                    tempFile = file;
                }
                _SelectedFileProperties.TemporaryFile = tempFile;

                Windows.Storage.FileProperties.BasicProperties basicProps = await file.GetBasicPropertiesAsync();
                _SelectedFileProperties.LastModifiedDate = basicProps.DateModified;
                _SelectedFileProperties.Filesize = basicProps.Size;

                bool success = true;
                bool isUsedByOtherProgram = false;
                bool pdfDocCreatedSuccessfully = false;

                PDFDoc doc = null;
                try
                {
                    string path = file.Path;

                    bool isBox = UtilityFunctions.IsBox(path);
                    bool isDropBox = UtilityFunctions.IsDropBox(path);
                    bool isOneDrive = UtilityFunctions.IsOneDrive(path);
                    bool isOneDriveTemp = Utilities.UtilityFunctions.IsOneDriveTempFile(file.Path);

                    if (_SelectedFileProperties.OpenedDocumentState != OpenedDocumentStates.Uneditable)
                    {
                        if (isDropBox || isOneDriveTemp)
                        {
                            _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.ReadOnly;
                        }
                        if (isBox)
                        {
                            _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.ReadOnly;
                        }
                    }

                    if (canceler.Cancel)
                    {
                        return;
                    }

                    pdftron.Common.PDFNetException pdfEX = null;
                    try
                    {
                        {
                            isUsedByOtherProgram = true;
                            try
                            {
                                using (IRandomAccessStream iras = await file.OpenReadAsync())
                                {
                                    isUsedByOtherProgram = false;
                                }
                            }
                            catch (Exception) { }

                            try
                            {
                                doc = await GetPDFDocAsync(tempFile);
                            }
                            catch (Exception e) 
                            {
                                pdftron.Common.PDFNetException pdfNetEx = new pdftron.Common.PDFNetException(e.HResult);
                                if (pdfNetEx.IsPDFNetException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Error creating PDFDoc: " + pdfNetEx);
                                }
                            }
                        }

                        if (doc != null)
                        {
                            pdfDocCreatedSuccessfully = true;

                            if (doc.IsModified() && _SelectedFileProperties.OpenedDocumentState != OpenedDocumentStates.Uneditable)
                            {
                                _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.Corrupted;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        pdfEX = new pdftron.Common.PDFNetException(e.HResult);
                        string errormessage = pdfEX.ToString();
                        System.Diagnostics.Debug.WriteLine("Failed to create PDFDoc:\n" + e.ToString());
                    }

                    if (pdfDocCreatedSuccessfully)
                    {
                        _SelectedFileProperties.Doc = doc;
                        if (canceler.Cancel)
                        {
                            return;
                        }
                        success = true;
                        _SelectedFileProperties.File = file;
                    }

                }
                catch (Exception)
                {
                    success = false;
                }

                bool corrupt = false;
                if (pdfDocCreatedSuccessfully)
                {
                    try
                    {
                        if (!doc.InitStdSecurityHandler(""))
                        {
                            AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.VIEWER, "Document Opened Encrypted");

                            CancelCurrentDocLoad();
                            HandlePassword();
                            return;
                        }
                    }
                    catch (System.Exception)
                    {
                        corrupt = true;
                    }
                }
                else if (!pdfDocCreatedSuccessfully)
                {
                    CancelCurrentDocLoad();
                    IsModal = false;

                    if (isUsedByOtherProgram)
                    {
                        CancelCurrentDocLoad();
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        MessageDialog md = new MessageDialog(loader.GetString("DocumentsPage_FileOpeningError_UsedByAnother_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                        await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        RaiseDocumentLoadingFailed();
                        return;
                    }
                    else
                    {
                        CancelCurrentDocLoad();
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        MessageDialog md = new MessageDialog(string.Format(loader.GetString("DocumentsPage_FileOpeningError_CorruptFile_Info"), Settings.Settings.DisplayName), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                        await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        RaiseDocumentLoadingFailed();
                        return;
                    }
                }
                if (corrupt)
                {
                    IsModal = false;
                    CancelCurrentDocLoad();
                    Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    MessageDialog md = new MessageDialog(string.Format(loader.GetString("DocumentsPage_FileOpeningError_CorruptFile_Info"), Settings.Settings.DisplayName), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                    RaiseDocumentLoadingFailed();
                    return;
                }
                if (success && !corrupt)
                {
                    pdftron.PDFNet.EnableJavaScript(Settings.Settings.EnableJavaScript);
                    if (IsXFA())
                    {
                        OpenXFA();
                    }
                    else if (IsPackage())
                    {
                        OpenPackage();
                    }
                    else
                    {
                        CheckIntegrity();
                    }
                }
                else
                {
                    RaiseDocumentLoadingFailed();
                }

            }
        }

        private IAsyncOperation<PDFDoc> GetPDFDocAsync(IRandomAccessStream stream, string path)
        {
            Task<PDFDoc> t = new Task<PDFDoc>(() =>
            {
                return GetPDFDoc(stream, path);
            });
            t.Start();
            return t.AsAsyncOperation<PDFDoc>();
        }


        private PDFDoc GetPDFDoc(IRandomAccessStream stream, string path)
        {
            return new PDFDoc(stream, path);
        }

        private IAsyncOperation<PDFDoc> GetPDFDocAsync(StorageFile file)
        {
            Task<PDFDoc> t = new Task<PDFDoc>(() =>
            {
                return GetPDFDoc(file);
            });
            t.Start();
            return t.AsAsyncOperation<PDFDoc>();
        }


        private PDFDoc GetPDFDoc(StorageFile file)
        {
            PDFDoc doc = new PDFDoc(file);
            bool mod = doc.IsModified();
            return doc;
        }

        private async void TemporaryilyBlockInput()
        {
            try
            {
                IsInputDisabled = true;
                await Task.Delay(1000);
            }
            catch (Exception)
            {
            }
            finally
            {
                IsInputDisabled = false;
            }
        }

        private async void StartProgressBarAfterWait(FileOpeningCanceler canceler)
        {
            await Task.Delay(1000);
            if (!canceler.Cancel)
            {
                IsFileOpeningProgressBarVisible = true;
            }

            // TODO Phone (likely not needed)
            //await Task.Delay(200);
            //if (!canceler.Cancel)
            //{
            //    IsFileOpeningProgressBarVisible = true;
            //    IsModal = true;
            //}
        }

        private void CloseProgressBar()
        {
            IsFileOpeningProgressBarVisible = false;
        }

        private bool IsAlreadyOpeningfile(StorageFile file)
        {
            if (_CurrentFileOpeningCanceler != null && _CurrentFileOpeningCanceler.File != null)
            {
                return _CurrentFileOpeningCanceler.File.Equals(file);
            }
            return false;
        }

        private void CancelCurrentDocLoad()
        {
            if (_CurrentFileOpeningCanceler != null)
            {
                _CurrentFileOpeningCanceler.Cancel = true;
                _CurrentFileOpeningCanceler.File = null;
            }
            CloseProgressBar();
        }

        private void RaiseDocumentReady()
        {
            if (DocumentReady != null)
            {
                DocumentReady(_SelectedFileProperties);
            }
        }

        private void RaiseDocumentLoadingFailed()
        {
            if (DocumentLoadingFailed != null)
            {
                DocumentLoadingFailed(this, new Windows.UI.Xaml.RoutedEventArgs());
            }
        }

        #endregion Opening Document


        #region Handle Password

        private PasswordViewModel _PasswordViewmModel;
        public PasswordViewModel PasswordViewModel
        {
            get { return _PasswordViewmModel; }
            set { Set(ref _PasswordViewmModel, value); }
        }




        private const int DEFAULT_ATTEMPTS_AT_PASSWORD = 3;
        private int _AttemptsAtPassword = 0;

        private string _CurrentPassword = string.Empty;
        public string CurrentPassword
        {
            get { return _CurrentPassword; }
            set
            {
                if (Set(ref _CurrentPassword, value))
                {
                    RaisePropertyChanged("HasPasswordBoxGotContent");
                    IsIncorrectPasswordNotificationVisible = false;
                }
            }
        }

        private bool _IsIncorrectPasswordNotificationVisible = false;
        public bool IsIncorrectPasswordNotificationVisible
        {
            get { return _IsIncorrectPasswordNotificationVisible; }
            set
            {
                Set(ref _IsIncorrectPasswordNotificationVisible, value);
                RaisePropertyChanged("HasPasswordBoxGotContent");
            }
        }

        public bool HasPasswordBoxGotContent
        {
            get { return !string.IsNullOrEmpty(CurrentPassword) && CurrentPassword.Length > 0 && IsIncorrectPasswordNotificationVisible == false; }
        }

        private void HandlePassword()
        {
            IsPasswordDialogOpen = true;
            PasswordViewModel = new PasswordViewModel(_SelectedFileProperties.Doc);
            PasswordViewModel.PasswordHandled += PasswordViewModel_PasswordHandled;
        }

        void PasswordViewModel_PasswordHandled(bool success, string password)
        {
            IsModal = false;
            if (success)
            {
                // check for XFA or package
                if (IsXFA()) OpenXFA();
                if (IsPackage()) OpenPackage();

                _SelectedFileProperties.Password = password;

                CheckIntegrity();
            }
            else
            {
                CancelCurrentDocLoad();
                _SelectedFileProperties.Doc.Dispose();
                RaiseDocumentLoadingFailed();
            }

            // On Windows Phone 8.1, it's important that IsPasswordDialogOpen is set to false after CheckIntegrity.
            // IsPasswordDialogOpen = false will trigger modality to go away, causing the phone to reattach the AppBar
            // CheckIntegrity will trigger navigation, which for some reason breaks the app bar if it's just been attached.
            IsPasswordDialogOpen = false;
            PasswordViewModel = null; 
        }

        #endregion Handle Password


        #region PDF Package


        private PDFPackageViewModel _PackageViewModel;
        public PDFPackageViewModel PackageViewModel
        {
            get { return _PackageViewModel; }
            set { Set(ref _PackageViewModel, value); }
        }


        private bool IsPackage()
        {
            return _SelectedFileProperties.Doc.GetRoot().FindObj("Collection") != null;
        }

        private void OpenPackage()
        {
            Obj collectionObj = _SelectedFileProperties.Doc.GetRoot().FindObj("Collection");
            if (collectionObj != null)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.VIEWER, "Document Opened Package");
                HandlePackage();
            }
            else
            {
                CheckIntegrity();
                //this.Frame.Navigate(typeof(ViewerPage), new Data.NavigationParameter(_CurrentPDFDoc, _SelectedStorageFile, _SelectedFileStream, OpenedDocumentStates.ReadOnly));
            }
        }


        private void HandlePackage()
        {
            CancelCurrentDocLoad();
            IsPackageDialogOpen = true;
            _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.ReadOnly;
            PDFPackageViewModel vm = new PDFPackageViewModel(_SelectedFileProperties.Doc, _SelectedFileProperties.File.Name);
            vm.FileSelcted += PackageDialog_FileSelcted;
            PackageViewModel = vm;
        }

        private async void PackageDialog_FileSelcted(PDFDoc doc, StorageFile file, bool err, string errorMessage)
        {
            if (doc != null)
            {
                _SelectedFileProperties.Doc = doc;
                _SelectedFileProperties.File = file;
                _SelectedFileProperties.TemporaryFile = await DocumentManager.Instance.OpenTemporaryCopyAsync(file);
                if (!doc.InitStdSecurityHandler(""))
                {
                    HandlePassword();
                }
                else
                {
                    CheckIntegrity();
                }
            }
            else if (err)
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                MessageDialog msg = new MessageDialog(errorMessage, loader.GetString("PackageDialog_FileExtractionError_Title"));
                await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(msg);
                RaiseDocumentLoadingFailed();
            }

            // On Windows Phone 8.1, it's important that IsPackageDialogOpen is set to false after CheckIntegrity.
            // IsPackageDialogOpen = false will trigger modality to go away, causing the phone to reattach the AppBar
            // CheckIntegrity will trigger navigation, which for some reason breaks the app bar if it's just been attached.
            PackageViewModel = null;
            IsPackageDialogOpen = false;
        }

        private void PackageDialogGoBack()
        {
            IsPackageDialogOpen = false;
            RaiseDocumentLoadingFailed();
        }

        #endregion PDFPackage


        #region Handle XFA

        private bool IsXFA()
        {
            Obj needsRenderingObj = _SelectedFileProperties.Doc.GetRoot().FindObj("NeedsRendering");
            return (needsRenderingObj != null && needsRenderingObj.IsBool() && needsRenderingObj.GetBool());
        }

        private async void OpenXFA()
        {
            AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.VIEWER, "Document Opened XFA");
            CancelCurrentDocLoad();
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog md = new MessageDialog(loader.GetString("DocumentsPage_FileOpeningError_XFA_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
            await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);

            // TODO Phone
            //IsModal = false;

            RaiseDocumentLoadingFailed();
        }

        #endregion Handle XFA


        #region Check Document Integrity

        private async void CheckIntegrity()
        {
            CancelCurrentDocLoad();
            if (_SelectedFileProperties.Doc != null)
            {
                try
                {
                    _SelectedFileProperties.Doc.LockRead();
                    int pageCount = _SelectedFileProperties.Doc.GetPageCount();
                    if (pageCount <= 0)
                    {
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        MessageDialog md = new MessageDialog(loader.GetString("DocumentsPage_FileOpeningError_NoPages_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                        await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        RaiseDocumentLoadingFailed();
                        return;
                    }
                }
                catch (Exception) { }
                finally
                {
                    _SelectedFileProperties.Doc.UnlockRead();
                }

                RaiseDocumentReady();
            }

            
        }



        #endregion Check Document Integrity


        #region File Opening (all but PDF for now)

        private class ConvertCanceller
        {
            public bool Cancel { get; set; }
            public ConvertCanceller()
            {
                Cancel = false;
            }
        }

        const double DEFAULT_TEXT_CONVERSION_FONT_SIZE = 12;
        const double DEFAULT_TEXT_CONVERSION_PAGE_WIDTH = 9;
        const double DEFAULT_TEXT_CONVERSION_PAGE_HEIGHT = 12;

        private bool _OpeningNonePDF = false;
        private ConvertCanceller _CurrentConvertCanceller;
        public async void OpenNonPDFPreConvert(StorageFile file)
        {
            if (_OpeningNonePDF)
            {
                return;
            }
            try
            {
                _OpeningNonePDF = true;
                IsConvertingDocument = true;
                ConvertCanceller canceller = new ConvertCanceller();
                _CurrentConvertCanceller = canceller;
                StorageFile originalFile = file;

                if (file.FileType == ".xps" 
                    || file.FileType == ".oxps" 
                    || file.FileType == ".txt"
                    || file.FileType == ".xml"
                    || file.FileType == ".md")
                {
                    string name = file.DisplayName;
                    string cancelMsg = "XPS", tempFileName = "FromXPS.pdf";
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = Settings.Settings.DefaultTempPDFName;
                    }
                    else
                    {
                        name += ".pdf";
                    }

                    StorageFile tempFile = null;
                    if ((file.Attributes & FileAttributes.Temporary) == FileAttributes.Temporary)
                    {
                        Guid guid = Guid.NewGuid();
                        string tempName = guid.ToString();
                        tempFile = await file.CopyAsync(ApplicationData.Current.TemporaryFolder, tempName);
                        file = tempFile;
                    }
                    PDFDoc doc = null;
                    try
                    {
                        if (file.FileType == ".txt")
                        {
                            doc = await OpenFromTXTAsync(file);
                            tempFileName = "FromTXT.pdf";
                            cancelMsg = "TXT";
                        }
                        else if (file.FileType == ".xml")
                        {
                            doc = await OpenFromTXTAsync(file);
                            tempFileName = "FronXML.pdf";
                            cancelMsg = "XML";
                        }
                        else if (file.FileType == ".md")
                        {
                            doc = await OpenFromTXTAsync(file);
                            tempFileName = "FromMD.pdf";
                            cancelMsg = "MD";
                        }
                        else
                            doc = await OpenFromXPSAsync(file);
                    }
                    catch (Exception e)
                    {
                        CompleteReader.Utilities.AnalyticsHandler.CURRENT.SendException(e);
                    }
                    if (canceller.Cancel)
                    {
                        System.Diagnostics.Debug.WriteLine("Cancelled " + cancelMsg + " Conversion");
                        return;
                    }
                    if (tempFile != null)
                    {
                        try
                        {
                            await tempFile.DeleteAsync();
                        }
                        catch (Exception e)
                        {
                            CompleteReader.Utilities.AnalyticsHandler.CURRENT.SendException(e);
                        }
                    }

                    if (doc != null)
                    {
                        doc.InitStdSecurityHandler(""); // we know there is no password

                        StorageFile tempPDFFile = null;
                        try
                        {
                            DocumentManager docManager = DocumentManager.Instance;
                            if (!DocumentManager.IsReady)
                            {
                                CompleteReader.Utilities.AnalyticsHandler.CURRENT.SendEvent(CompleteReader.Utilities.AnalyticsHandler.Category.FILEBROWSER,
                                    "DocumentOpener invoked without DocumentManager ready");
                                docManager = await DocumentManager.GetInstanceAsync().ConfigureAwait(false);
                            }
                            tempPDFFile = await docManager.OpenTemporaryCopyAsync(file, false);
                        }
                        catch (Exception) { }

                        if (tempPDFFile == null)
                        {
                            tempPDFFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(tempFileName, CreationCollisionOption.GenerateUniqueName);
                        }

                        if (tempPDFFile != null)
                        {
                            await doc.SaveToNewLocationAsync(tempPDFFile, pdftron.SDF.SDFDocSaveOptions.e_remove_unused);
                        }

                        if (canceller.Cancel)
                        {
                            System.Diagnostics.Debug.WriteLine("Cancelled " + cancelMsg + " Conversion");
                            return;
                        }

                        if (tempPDFFile != null)
                        {
                            _SelectedFileProperties = new NewDocumentProperties();
                            _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.NonePDF;
                            _SelectedFileProperties.Doc = doc;
                            _SelectedFileProperties.File = file;
                            _SelectedFileProperties.TemporaryFile = tempPDFFile;
                            _SelectedFileProperties.OriginalFileFromSystem = originalFile;

                            CheckIntegrity();
                        }
                    }
                    else
                    {
                        RaiseDocumentLoadingFailed();
                        try
                        {
                            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                            MessageDialog md = new MessageDialog(
                                string.Format(loader.GetString("DocumentsPage_FileOpeningError_ConversionFailed_Content"), Settings.Settings.DisplayName),
                                loader.GetString("DocumentsPage_FileOpeningError_Title"));
                            await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        }
                        catch (Exception e)
                        {
                            CompleteReader.Utilities.AnalyticsHandler.CURRENT.SendException(e);
                        }
                    }
                }                                
            }
            finally
            {
                _OpeningNonePDF = false;
                IsConvertingDocument = false;
            }
        }

        private async Task<PDFDoc> OpenFromXPSAsync(StorageFile file)
        {
            PDFDoc doc = new PDFDoc();
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                pdftron.Filters.IFilter filter = new pdftron.Filters.RandomAccessStreamFilter(stream);
                await pdftron.PDF.Convert.FromXpsAsync(doc, file).AsTask().ConfigureAwait(false);
            }
            return doc;
        }

        private async Task<PDFDoc> OpenFromTXTAsync(StorageFile file)
        {
            PDFDoc doc = new PDFDoc();

            // Create objects to add Conversion Options
            var mObjSet = new ObjSet();
            var mObj = mObjSet.CreateDict();

            // Add formating options
            mObj.PutNumber("FontSize", DEFAULT_TEXT_CONVERSION_FONT_SIZE);
            mObj.PutBool("UseSourceCodeFormatting", true);
            mObj.PutNumber("PageWidth", DEFAULT_TEXT_CONVERSION_PAGE_WIDTH);
            mObj.PutNumber("PageHeight", DEFAULT_TEXT_CONVERSION_PAGE_HEIGHT);

            await pdftron.PDF.Convert.FromTextAsync(doc, file, mObj);

            return doc;
        }

        private void CancelCurrentConversion()
        {
            if (_CurrentConvertCanceller != null)
            {
                _CurrentConvertCanceller.Cancel = true;
                _OpeningNonePDF = false;
                _CurrentConvertCanceller = null;
            }
        }

        private async void OpenNonPDFUniversalConversion(StorageFile file)
        {
            try
            {
                _SelectedFileProperties = new NewDocumentProperties();
                _SelectedFileProperties.OpenedDocumentState = OpenedDocumentStates.Universal;
                _SelectedFileProperties.OriginalFileFromSystem = file;

                CancelCurrentDocLoad();
                FileOpeningCanceler canceler = new FileOpeningCanceler();
                canceler.File = file;
                _CurrentFileOpeningCanceler = canceler;
                StartProgressBarAfterWait(canceler);

                bool mustCopy = (file.Attributes & FileAttributes.Temporary) == FileAttributes.Temporary || (file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                mustCopy |= Settings.Settings.GetFileOpeningType(file) == Settings.SharedSettings.FileOpeningType.UniversalConversionFromString;

                if (mustCopy)
                {
                    StorageFile temporaryFile = await UtilityFunctions.GetTemporarySaveFileAsync(file.Name);
                    if (canceler.Cancel)
                    {
                        return;
                    }
                    if (temporaryFile == null)
                    {
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        MessageDialog md = new MessageDialog(loader.GetString("DocumentsPage_FileOpeningError_FailedToCreateTempFile"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                        await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        RaiseDocumentLoadingFailed();
                        return;
                    }
                    await file.CopyAndReplaceAsync(temporaryFile);
                    file = temporaryFile;
                    await UtilityFunctions.RemoveReadOnlyAndTemporaryFlagsAsync(file);
                    if (canceler.Cancel)
                    {
                        return;
                    }
                }

                if (Settings.Settings.GetFileOpeningType(file) != Settings.SharedSettings.FileOpeningType.UniversalConversionFromString)
                {
                    IRandomAccessStream stream = await file.OpenReadAsync();
                    _SelectedFileProperties.FileStream = stream;
                }

                _SelectedFileProperties.File = file;
                RaiseDocumentReady();
            }
            catch (Exception ex)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_APP, pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex));
            }
        }

        #endregion File Opening (all but PDF for now)



        #region Back Key

        public override bool GoBack()
        {
            if (IsPasswordDialogOpen)
            {
                PasswordViewModel_PasswordHandled(false, null); // mimic failing
                return true;
            }
            if (IsPackageDialogOpen)
            {
                if (PackageViewModel != null && PackageViewModel.GoBack())
                {
                    return true;
                }
            }
            if (_CurrentFileOpeningCanceler != null)
            {
                _CurrentFileOpeningCanceler.Cancel = true;
                _CurrentFileOpeningCanceler = null;
                IsFileOpeningProgressBarVisible = false;
                IsModal = false;
            }
            return false;
        }

        #endregion Back Key

    }
}
