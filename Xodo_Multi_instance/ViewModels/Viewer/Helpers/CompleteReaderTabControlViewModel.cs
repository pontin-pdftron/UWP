using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.FileOpening;
using pdftron.PDF;
using pdftron.PDF.Tools;
using pdftron.PDF.Tools.Controls;
using pdftron.PDF.Tools.Controls.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using pdftron.Filters;

namespace CompleteReader.ViewModels.Viewer.Helpers
{
    public class TabSetting
    {
        private static Windows.Storage.ApplicationDataContainer _LocalSettings;
        private static Windows.Storage.ApplicationDataContainer LocalSettings
        {
            get
            {
                if (null == _LocalSettings)
                {
                    _LocalSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                }
                return _LocalSettings;
            }
        }

        private const string OPEN_TABS_SETTING_NAME = "CompleteReader_Tabs_NumOpen";
        public static int OpenTabs
        {
            get
            {
                if (LocalSettings.Values.ContainsKey(OPEN_TABS_SETTING_NAME))
                {
                    return (int)LocalSettings.Values[OPEN_TABS_SETTING_NAME];
                }
                return 0;
            }
            set
            {
                LocalSettings.Values[OPEN_TABS_SETTING_NAME] = value;
            }
        }

        public static void SaveSettings(int tabIndex, CompleteReaderPDFViewCtrlTabMetaData metaData)
        {
            string prefix = "CompleteReader_Tabs_" + tabIndex + "_";
            Windows.Foundation.Collections.IPropertySet locals = LocalSettings.Values;
            locals[prefix + "Title"] = metaData.TabTitle;
            locals[prefix + "HS"] = metaData.HScrollPos;
            locals[prefix + "VS"] = metaData.VScrollPos;
            locals[prefix + "Z"] = metaData.Zoom;
            locals[prefix + "Pg"] = metaData.LastPage;
            locals[prefix + "PgR"] = (int)metaData.PageRotation;
            locals[prefix + "PPM"] = (int)metaData.PagePresentationMode;
            locals[prefix + "R"] = (bool)metaData.IsReflow;
            locals[prefix + "TAdded"] = metaData.TabAddedTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
            locals[prefix + "TLView"] = metaData.TabLastViewedTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
            locals[prefix + "PW"] = metaData.Password;
            locals[prefix + "TmpToken"] = metaData.FutureAccessListToken;
            locals[prefix + "OriToken"] = metaData.OriginalFileToken;
            locals[prefix + "SourceToken"] = metaData.FileSourceTokenIfNotSaveable;
            locals[prefix + "TmbLoc"] = metaData.ThumbnailLocation;
            locals[prefix + "TmbToDate"] = metaData.IsThumbnailUpToDate;

            locals[prefix + "DocSt"] = (int)metaData.DocumentState;
            locals[prefix + "SvWarn"] = metaData.HasUserBeenWarnedAboutSaving;
            locals[prefix + "MdSinceO"] = metaData.HasDocumentBeenModifiedSinceOpening;
            locals[prefix + "MRU"] = metaData.MostRecentlyUsedToken;
            locals[prefix + "Drag"] = metaData.IsOpenedThroughDrop;

            locals[prefix + "MdSinceS"] = metaData.IsDocumentModifiedSinceLastSave;
            locals[prefix + "BrkRes"] = metaData.IsBrokenDocumentRestored;

            locals[prefix + "ModDate"] = metaData.LastModifiedDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
            locals[prefix + "FlSize"] = metaData.FileSize;
        }

        public static CompleteReaderPDFViewCtrlTabMetaData GetFromSettings(int tabIndex)
        {
            CompleteReaderPDFViewCtrlTabMetaData data = new CompleteReaderPDFViewCtrlTabMetaData();
            string prefix = "CompleteReader_Tabs_" + tabIndex + "_";
            Windows.Foundation.Collections.IPropertySet locals = LocalSettings.Values;

            data.TabTitle = GetSetting<string>(prefix + "Title", string.Empty);
            data.HScrollPos = GetSetting<double>(prefix + "HS", 0);
            data.VScrollPos = GetSetting<double>(prefix + "VS", 0);
            data.Zoom = GetSetting<double>(prefix + "Z", 1);
            data.LastPage = GetSetting<int>(prefix + "Pg", 1);
            data.PageRotation = (PageRotate)GetSetting<int>(prefix + "PgR", (int)PageRotate.e_0);
            data.PagePresentationMode = (PDFViewCtrlPagePresentationMode)GetSetting<int>(prefix + "PPM", (int)Settings.Settings.Defaults.PagePresentationMode);
            data.IsReflow = (bool)GetSetting<bool>(prefix + "R", false);
            data.IsOpenedThroughDrop = GetSetting<bool>(prefix + "Drag", false);
            bool hasDate = false;
            string dateTimeString = GetSetting<string>(prefix + "TAdded", string.Empty);
            if (!string.IsNullOrWhiteSpace(dateTimeString))
            {
                try
                {
                    data.TabAddedTimeStamp = DateTime.Parse(dateTimeString);
                    hasDate = true;
                }
                catch (Exception) { }
            }
            if (!hasDate)
            {
                data.TabAddedTimeStamp = DateTime.MinValue;
            }

            hasDate = false;
            dateTimeString = GetSetting<string>(prefix + "TLView", string.Empty);
            if (!string.IsNullOrWhiteSpace(dateTimeString))
            {
                try
                {
                    data.TabLastViewedTimeStamp = DateTime.Parse(dateTimeString);
                    hasDate = true;
                }
                catch (Exception) { }
            }
            if (!hasDate)
            {
                data.TabLastViewedTimeStamp = DateTime.MinValue;
            }

            data.Password = GetSetting<string>(prefix + "PW", string.Empty);
            data.FutureAccessListToken = GetSetting<string>(prefix + "TmpToken", string.Empty);
            data.OriginalFileToken = GetSetting<string>(prefix + "OriToken", string.Empty);
            data.FileSourceTokenIfNotSaveable = GetSetting<string>(prefix + "SourceToken", string.Empty);
            data.ThumbnailLocation = GetSetting<string>(prefix + "TmbLoc", string.Empty);
            data.IsThumbnailUpToDate = GetSetting<bool>(prefix + "TmbToDate", false);

            data.DocumentState = (OpenedDocumentStates)GetSetting<int>(prefix + "DocSt", (int)OpenedDocumentStates.Normal);
            data.HasUserBeenWarnedAboutSaving = GetSetting<bool>(prefix + "SvWarn", false);
            data.HasDocumentBeenModifiedSinceOpening = GetSetting<bool>(prefix + "MdSinceO", false);
            data.MostRecentlyUsedToken = GetSetting<string>(prefix + "MRU", string.Empty);

            data.IsDocumentModifiedSinceLastSave = GetSetting<bool>(prefix + "MdSinceS", false);
            data.IsBrokenDocumentRestored = GetSetting<bool>(prefix + "BrkRes", false);

            dateTimeString = GetSetting<string>(prefix + "ModDate", string.Empty);
            hasDate = false;
            if (!string.IsNullOrWhiteSpace(dateTimeString))
            {
                try
                {
                    data.LastModifiedDate = DateTimeOffset.Parse(dateTimeString);
                    hasDate = true;
                }
                catch (Exception) { }
            }
            if (!hasDate)
            {
                data.LastModifiedDate = DateTimeOffset.MinValue;
            }
            data.FileSize = GetSetting<ulong>(prefix + "FlSize", 0);
            

            return data;
        }

        private static T GetSetting<T>(string settingName, T def)
        {
            if (LocalSettings.Values.ContainsKey(settingName))
            {
                object val = LocalSettings.Values[settingName];
                if (val is T)
                {
                    return (T)val;
                }
            }
            return def;
        }
    }


    public class CompleteReaderPDFViewCtrlTabMetaData : ViewModelBase
    {
        public string TabTitle { get; set; }
        public double HScrollPos { get; set; }
        private double _VScrollPos = 0;
        public double VScrollPos 
        {
            get
            {
                return _VScrollPos;
            }
            set
            {
                _VScrollPos = value;
            }
        }
        public double Zoom { get; set; }
        private int _LastPage = 1;
        public int LastPage 
        {
            get { return _LastPage; }
            set { _LastPage = value; }
        }
        private PageRotate _PageRotation;
        public PageRotate PageRotation
        {
            get { return _PageRotation; }
            set { _PageRotation = value; }
        }

        private PDFViewCtrlPagePresentationMode _PagePresentationMode = Utilities.Constants.DefaultPagePresentationMode;
        public PDFViewCtrlPagePresentationMode PagePresentationMode
        {
            get { return _PagePresentationMode; }
            set { _PagePresentationMode = value; }
        }

        private bool _IsReflow = false;
        public bool IsReflow
        {
            get { return _IsReflow; }
            set { _IsReflow = value; }
        }

        public DateTime TabAddedTimeStamp { get; set; }
        public DateTime TabLastViewedTimeStamp { get; set; }
        private string _Password = string.Empty;
        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                _Password = value;
            }
        }
        /// <summary>
        /// The token  of the file in the App's temporary folder that was opened
        /// </summary>
        public string FutureAccessListToken { get; set; }
        /// <summary>
        /// The file that is meant to be opened, which we save to in case of a normal save.
        /// </summary>
        public string OriginalFileToken { get; set; }
        /// <summary>
        /// In case of non-PDFs, and some read-only files, we keep a reference to the file the user initially picked, even if we can't modify it.
        /// This lets us avoid opening duplicates.
        /// </summary>
        public string FileSourceTokenIfNotSaveable { get; set; }
        public string ThumbnailLocation { get; set; }
        public bool IsThumbnailUpToDate { get; set; }

        public OpenedDocumentStates DocumentState { get; set; }
        public bool HasUserBeenWarnedAboutSaving { get; set; }
        public bool HasDocumentBeenModifiedSinceOpening { get; set; }
        public string MostRecentlyUsedToken { get; set; }

        public bool IsDocumentModifiedSinceLastSave { get; set; }
        public bool IsBrokenDocumentRestored { get; set; }

        public ulong FileSize { get; set; }
        private DateTimeOffset _LastModifiedDate = DateTimeOffset.MinValue;
        public DateTimeOffset LastModifiedDate 
        {
            get { return _LastModifiedDate; }
            set { _LastModifiedDate = value; }
        }

        public bool IsOpenedThroughDrop { get; set; }

        public CompleteReaderPDFViewCtrlTabMetaData()
        {
        }

        public void UpdateSettings(PDFViewCtrl ctrl)
        {
            HScrollPos = (int)ctrl.GetHScrollPos();
            VScrollPos = ctrl.GetVScrollPos();
            Zoom = ctrl.GetZoom();
            LastPage = ctrl.GetCurrentPage();
            _PageRotation = ctrl.GetRotation();
            _PagePresentationMode = ctrl.GetPagePresentationMode();
        }
    }


    public class CompleteReaderPDFViewCtrlTabInfo : ViewModelBase
    {
        public enum ShowIfModifedStates
        {
            DoNotShow,
            ShowIfModified,
            ShowIfModidiedAndCantSave
        }


        public delegate void ItemClickedEventHandler(CompleteReaderPDFViewCtrlTabInfo item, bool closeRequested);
        public event ItemClickedEventHandler ItemClicked;
        
        public delegate void SetUpPDFViewCtrlAndToolsHandler(PDFViewCtrl ctrl, ToolManager toolManager);
        public SetUpPDFViewCtrlAndToolsHandler SetUpPDFViewCtrlAndTools;

        public RoutedEventHandler PdfUpdated;

        public RoutedEventHandler PdfIsReady;

        public event Windows.UI.Xaml.Input.TappedEventHandler ReflowTapped;

        public event Windows.UI.Xaml.Input.TappedEventHandler TappedBeforeTools;
        public event Windows.UI.Xaml.Input.TappedEventHandler TappedAfterTools;

        public const string TabPreviewFolderName = "Tab_Preview_Images";
        public string ThumbnailFolder { get; set; }
        public string ThumbnailFallback { get; set; }

        private bool _Active = false;

        #region Visible Properties

        private bool _IsFixedItem = false;
        public bool IsFixedItem
        {
            get { return _IsFixedItem; }
            set
            {
                if (value != _IsFixedItem)
                {
                    _IsFixedItem = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string PreviewSource
        {
            get 
            {
                if (!string.IsNullOrEmpty(MetaData.ThumbnailLocation))
                {
                    return System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, TabPreviewFolderName, MetaData.ThumbnailLocation);
                }
                return ThumbnailFallback; 
            }
        }

        private bool _ShowPreview;
        public bool ShowPreview
        {
            get { return _ShowPreview; }
            set
            {
                if (value != _ShowPreview)
                {
                    _ShowPreview = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Title
        {
            get { return MetaData.TabTitle; }
            set
            {
                if (value != null)
                {
                    if (!value.Equals(MetaData.TabTitle))
                    {
                        MetaData.TabTitle = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private bool _First;
        public bool First
        {
            get { return _First; }
            set
            {
                if (value != _First)
                {
                    _First = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _Last;
        public bool Last
        {
            get { return _Last; }
            set
            {
                if (value != _Last)
                {
                    _Last = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                if (value != _IsSelected)
                {
                    _IsSelected = value;
                    RaisePropertyChanged();
                    if (_IsSelected)
                    {
                        Activate();
                    }
                    else
                    {
                        Pause();
                        if (ToolManager != null)
                        {
                            ToolManager.CreateDefaultTool();
                        }
                    }
                }
            }
        }

        private PDFViewCtrl _PDFViewCtrl;
        public PDFViewCtrl PDFViewCtrl
        { 
            get { return _PDFViewCtrl; }
             set
            {
                _PDFViewCtrl = value;
                RaisePropertyChanged();
            }
        }

        private OutlineDialogViewModel.OutlineDialogStateBundle _OutlineState = null;
        private OutlineDialogViewModel _OutlineDialogViewModel = null;
        public OutlineDialogViewModel OutlineDialogViewModel
        {
            get
            {
                if (_OutlineDialogViewModel == null)
                {
                    _OutlineDialogViewModel = new OutlineDialogViewModel(PDFViewCtrl);
                    _OutlineDialogViewModel.Isconverting = PDFViewCtrl.IsConverting;
                    _OutlineDialogViewModel.NavigationStack = NavigationStack;
                    if (_OutlineState != null)
                    {
                        _OutlineDialogViewModel.RestoreState(_OutlineState);
                    }
                    else
                    {
                        OutlineDialogViewModel.OutlineDialogStateBundle outlineState = new OutlineDialogViewModel.OutlineDialogStateBundle((OutlineDialogViewModel.SubViews)Settings.Settings.OutlineDefautlView, 0, null, 0);
                        _OutlineDialogViewModel.RestoreState(outlineState);
                    }
                    _OutlineDialogViewModel.AnnotationList._ViewModel.ToolManager = ToolManager;
                    _OutlineDialogViewModel.Outline.DocumentTitle = Title;
                }
                return _OutlineDialogViewModel;
            }
            internal set { _OutlineDialogViewModel = value; }
        }

        private ReflowView _ReflowView;
        public ReflowView ReflowView
        {
            get { return _ReflowView; }
            set { Set(ref _ReflowView, value); }
        }

        public bool IsReflow
        {
            get
            {
                return MetaData.IsReflow;
            }
            set
            {
                if (MetaData.IsReflow != value)
                {
                    MetaData.IsReflow = value;
                    NavigationStack.CleanPositionData();
                    if (!MetaData.IsReflow)
                    {
                        PDFViewCtrl.SetCurrentPage(ReflowView.CurrentPage);
                        PDFViewCtrl.RequestRendering();
                    }
                    UpdateReflowState();
                    RaisePropertyChanged();
                }
            }
        }

        private ToolManager _ToolManager;
        public ToolManager ToolManager 
        { 
            get { return _ToolManager; }
            set 
            {
                _ToolManager = value;
                RaisePropertyChanged();
            }
        }

        private StorageFile _PDFFile;
        public StorageFile PDFFile
        {
            get { return _PDFFile; }
            set 
            {
                if (value != _PDFFile)
                {
                    _PDFFile = value;
                    RaisePropertyChanged();
                }
            }
        }

        public StorageFile OriginalFile { get; set; }
        public StorageFile FileSourceIfNotSaveable { get; set; }

        public Windows.Storage.Streams.IRandomAccessStream StreamForconversion { get; set; }

        private PDFDoc _Doc;
        public PDFDoc Doc
        {
            get { return _Doc; }
            set
            {
                _Doc = value;
            }
        }

        private NavigationStack _NavigationStack;
        public NavigationStack NavigationStack
        {
            get { return _NavigationStack; }
            set { _NavigationStack = value; }
        }

        private bool _FileLoadingError = false;
        public bool FileLoadingError
        {
            get { return _FileLoadingError; }
            set
            {
                if (value != _FileLoadingError)
                {
                    _FileLoadingError = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _HasUnsavedChanged = false;
        public bool ShowHasUnsavedChanged
        {
            get { return _HasUnsavedChanged; }
            set
            {
                if (value != _HasUnsavedChanged)
                {
                    _HasUnsavedChanged = value;
                    RaisePropertyChanged();
                }
            }
        }

        //private bool _IsDocumentModifiedSinceLastSave = false;
        [System.Xml.Serialization.XmlElement(ElementName = "ModSinceSave")]
        public bool IsDocumentModifiedSinceLastSave
        {
            get { return MetaData.IsDocumentModifiedSinceLastSave; }
            set
            {
                if (value != MetaData.IsDocumentModifiedSinceLastSave)
                {
                    MetaData.IsDocumentModifiedSinceLastSave = value;
                    ShowHasUnsavedChanged = value && ShouldShowIfDocumentModified;
                }
            }
        }

        private ShowIfModifedStates _ShowIfDocumentModified = ShowIfModifedStates.DoNotShow;

        /// <summary>
        /// If false, auto-saving is implied
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public ShowIfModifedStates ShowIfDocumentModified
        {
            get { return _ShowIfDocumentModified; }
            set
            {
                if (value != _ShowIfDocumentModified)
                {
                    _ShowIfDocumentModified = value;
                    if (ShouldShowIfDocumentModified && !ShowHasUnsavedChanged)
                    {
                        ShowHasUnsavedChanged = IsDocumentModifiedSinceLastSave;
                    }
                    else
                    {
                        ShowHasUnsavedChanged = false;
                    }
                }
            }
        }

        private bool ShouldShowIfDocumentModified
        {
            get
            {
                if (_ShowIfDocumentModified == ShowIfModifedStates.ShowIfModified)
                {
                    return true;
                }
                else if (_ShowIfDocumentModified == ShowIfModifedStates.ShowIfModidiedAndCantSave)
                {
                    return (DocumentState != OpenedDocumentStates.Normal && DocumentState != OpenedDocumentStates.NeedsFullSave);
                }
                return false;
            }
        }

        public OpenedDocumentStates DocumentState
        {
            get { return MetaData.DocumentState; }
            set
            {
                MetaData.DocumentState = value;
                RaisePropertyChanged("IsConverting");
            }
        }

        public bool HasDocumentBeenModifiedSinceOpening
        {
            get { return MetaData.HasDocumentBeenModifiedSinceOpening; }
            set { MetaData.HasDocumentBeenModifiedSinceOpening = value; }
        }

        public bool HasUserBeenWarnedAboutSaving
        {
            get { return MetaData.HasUserBeenWarnedAboutSaving; }
            set { MetaData.HasUserBeenWarnedAboutSaving = value; }
        }

        private DateTimeOffset _SystemLastModifiedDate = DateTimeOffset.MinValue;
        public DateTimeOffset SystemLastModifiedDate 
        {
            get { return _SystemLastModifiedDate; }
            set { _SystemLastModifiedDate = value; } 
        }
        private ulong _SystemFileSize = 0;
        public ulong SystemFileSize
        {
            get { return _SystemFileSize; }
            set { _SystemFileSize = value; }
        }
        public bool IsUpToDate
        {
            get 
            {
                if (DocumentState == OpenedDocumentStates.Corrupted || DocumentState == OpenedDocumentStates.CorruptedAndModified
                    || DocumentState == OpenedDocumentStates.Normal || DocumentState == OpenedDocumentStates.NeedsFullSave)
                {
                    return SystemLastModifiedDate == MetaData.LastModifiedDate || SystemFileSize == MetaData.FileSize;
                }
                return true;
            }
        }

        private bool _UpdatingFile = false;
        /// <summary>
        /// True if the original file is out of date and we have to load a new file
        /// </summary>
        public bool UpdatingFile
        {
            get { return _UpdatingFile; }
            set
            { Set(ref _UpdatingFile, value); }
        }

        private bool _PDFReady = false;
        public bool PDFReady
        {
            get { return _PDFReady; }
            private set
            {
                if (Set(ref _PDFReady, value))
                {
                    if (_PDFReady && PdfIsReady != null)
                    {
                        PdfIsReady(this, new RoutedEventArgs());
                    }
                }
            }
        }

        private bool _IsWaitingForConversionToStart = false;
        public bool IsWaitingForConversionToStart
        {
            get { return _IsWaitingForConversionToStart; }
            set { Set(ref _IsWaitingForConversionToStart, value); }
        }

        public bool CanBeSaved
        {
            get
            {
                return DocumentState != OpenedDocumentStates.Universal && DocumentState != OpenedDocumentStates.Uneditable;
            }
        }

        public bool IsConverting
        {
            get { return this.MetaData.DocumentState == OpenedDocumentStates.Universal; }
        }

        private bool _ShowConversionProgress = false;
        public bool ShowConversionProgress
        {
            get { return _ShowConversionProgress; }
            set { Set(ref _ShowConversionProgress, value); }
        }

        #endregion Visible Properties

        public RelayCommand ItemClickedCommand { get; private set; }

        private void ItemClickedCommandImpl(object parameter)
        {
            string param = parameter as string;
            bool close = false;
            if (!string.IsNullOrEmpty(param))
            {
                if (param.Equals("close", StringComparison.OrdinalIgnoreCase))
                {
                    close = true;
                }
            }
            if (ItemClicked != null)
            {
                ItemClicked(this, close);
            }
        }

        public static CompleteReaderPDFViewCtrlTabInfo CreateFixedTab()
        {
            return new CompleteReaderPDFViewCtrlTabInfo();
        }

        private CompleteReaderPDFViewCtrlTabInfo()
        {
            IsFixedItem = true;
            this.MetaData = new CompleteReaderPDFViewCtrlTabMetaData();
            ItemClickedCommand = new RelayCommand(ItemClickedCommandImpl);
        }

        public CompleteReaderPDFViewCtrlTabInfo(StorageFile orig, StorageFile tmp, PDFDoc doc, string title)
        {
            MetaData = new CompleteReaderPDFViewCtrlTabMetaData();

            if (title == null)
            {
                Title = string.Empty;
            }
            else
            {
                Title = title;
            }
            _PDFFile = tmp;
            OriginalFile = orig;
            _Doc = doc;

            ItemClickedCommand = new RelayCommand(ItemClickedCommandImpl);
        }

        /// <summary>
        /// Helper method to restore opened tabs
        /// </summary>
        /// <param name="orig"> Original File </param>
        /// <param name="tmp"> Temperory File </param>
        /// <param name="restoredData"></param>
        public CompleteReaderPDFViewCtrlTabInfo(StorageFile orig, StorageFile tmp, CompleteReaderPDFViewCtrlTabMetaData restoredData)
        {
            MetaData = restoredData;
            _PDFFile = tmp;
            OriginalFile = orig;
            if (tmp == null)
            {
                FileLoadingError = true;
            }

            ItemClickedCommand = new RelayCommand(ItemClickedCommandImpl);
        }

        public static CompleteReaderPDFViewCtrlTabInfo CreateUniversalTabInfo(CompleteReaderPDFViewCtrlTabMetaData restoredData, StorageFile sourceFile)
        {
            CompleteReaderPDFViewCtrlTabInfo info = new CompleteReaderPDFViewCtrlTabInfo(null, null, restoredData);
            info.FileLoadingError = false;
            info.FileSourceIfNotSaveable = sourceFile;
            info.OriginalFile = sourceFile;
            info.DocumentState = OpenedDocumentStates.Universal;
            return info;
        }

        public CompleteReaderPDFViewCtrlTabMetaData MetaData { get; set; }

        #region Public Functions

        public void RefreshState()
        {
            if (_Active && PDFViewCtrl != null && !_SettingDoc)
            {
                this.MetaData.UpdateSettings(PDFViewCtrl);
            }
        }

        private enum OpeningType
        {
            DocExists = 0,
            PDFTempFileExists,
            OpenUniversalFromStorageFile,
            OpenUniveralFromPath,
        }

        public void Activate()
        {
            if (IsFixedItem)
            {
                return;
            }

            if (!IsUpToDate)
            {
                if (!IsDocumentModifiedSinceLastSave)
                {
                    UpdateFile();
                }
                return;
            }

            Settings.Settings.FileOpeningType openingType = Settings.Settings.FileOpeningType.PDF;

            if (Doc == null && !FileLoadingError)
            {
                if (PDFFile != null)
                {
                    bool success = false;
                    try
                    {
                        Doc = new PDFDoc(PDFFile);
                        bool hasOC = Doc.HasOC();
                        if (!string.IsNullOrEmpty(MetaData.Password))
                        {
                            success = Doc.InitStdSecurityHandler(MetaData.Password);
                        }
                        else
                        {
                            Doc.InitStdSecurityHandler("");
                        }
                        success = true;
                    }
                    catch (Exception)
                    { }
                    FileLoadingError = !success;
                }
                else
                {
                    if (PDFFile == null && OriginalFile != null)
                    {
                        openingType = Settings.Settings.GetFileOpeningType(OriginalFile);
                    }
                }
            }

            if (PDFViewCtrl == null)
            {
                PDFViewCtrl = new PDFViewCtrl();
                PDFViewCtrl.Opacity = 0;
                PDFViewCtrl.SetPageSpacing(2, 2, 2, 0);
                _ThumbnailHandler = new OnThumbnailGeneratedEventHandler(PDFViewCtrl_OnThumbnailGenerated);
                PDFViewCtrl.OnThumbnailGenerated += _ThumbnailHandler;

                if (Settings.Settings.RememberLastPage)
                {
                    PDFViewCtrl.SetPagePresentationMode(MetaData.PagePresentationMode);
                    switch (MetaData.PageRotation)
                    {
                        case PageRotate.e_90:
                            PDFViewCtrl.RotateClockwise();
                            break;
                        case PageRotate.e_180:
                            PDFViewCtrl.RotateClockwise();
                            PDFViewCtrl.RotateClockwise();
                            break;
                        case PageRotate.e_270:
                            PDFViewCtrl.RotateCounterClockwise();
                            break;
                    }
                }
                else
                {
                    PDFViewCtrl.SetPagePresentationMode(Utilities.Constants.DefaultPagePresentationMode);
                }
            }

            if (ToolManager == null)
            {
                PDFViewCtrl.Tapped += PDFViewCtrlBeforeTools_Tapped;
                ToolManager = new ToolManager(PDFViewCtrl);
                ToolManager.SetUndoRedoManager(new UndoRedoManager());

                PDFViewCtrl.Tapped += PDFViewCtrlAfterTools_Tapped;
            }

            if (NavigationStack == null)
            {
                NavigationStack = new NavigationStack(PDFViewCtrl);
                ToolManager.NavigationStack = NavigationStack;
            }

            if (!_Active && SetUpPDFViewCtrlAndTools != null)
            {
                SetUpPDFViewCtrlAndTools(PDFViewCtrl, ToolManager);
                ToolManager.DocumentName = Title;
                ToolManager.ReadOnlyMode = (DocumentState == OpenedDocumentStates.Uneditable);
                ToolManager.CopyAnnotatedTextToNote = Settings.Settings.CopyAnnotatedTextToNote;
            }

            if (!_Active && Doc != null)
            {
                _Active = true;

                if (_SetDocHandler == null)
                {
                    _SetDocHandler = new OnSetDocEventHandler(PDFViewCtrl_OnSetDoc);
                    PDFViewCtrl.OnSetDoc += _SetDocHandler;
                    _SettingDoc = true;
                }
                try
                {
                    PDFViewCtrl.SetDoc(Doc);
                }
                catch (Exception)
                {
                    FileLoadingError = true;
                }
                if (FileLoadingError)
                {
                    return;
                }
                PDFReady = true;
                if (!MetaData.IsThumbnailUpToDate && ShowPreview)
                {
                    UpdatePreview();
                }

                if (StreamForconversion != null)
                {
                    StreamForconversion.Dispose();
                }
            }
            else if (!_Active && (openingType == Settings.SharedSettings.FileOpeningType.UniversalConversion || openingType == Settings.SharedSettings.FileOpeningType.UniversalConversionFromString))
            {
                _Active = true;
                try
                {
                    ShowConversionProgress = true;
                    IsWaitingForConversionToStart = true;
                    _ConversionChangedHandler = new OnConversionEventHandler(PDFViewCtrl_OnConversionChanged);
                    PDFViewCtrl.OnConversionChanged += _ConversionChangedHandler;
                    DocumentConversion conversion = null;
                    {
                        WordToPDFOptions opts = new WordToPDFOptions();
                        opts.SetLayoutResourcesPluginPath(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Resources"));
                        opts.SetSmartSubstitutionPluginPath(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Resources"));
                        if (openingType == Settings.SharedSettings.FileOpeningType.UniversalConversion && StreamForconversion != null)
                        {
                            pdftron.Filters.IFilter filter = new pdftron.Filters.RandomAccessStreamFilter(StreamForconversion);
                            conversion = pdftron.PDF.Convert.UniversalConversion(filter, null);
                            StreamForconversion = null;
                        }
                        else if (openingType == Settings.SharedSettings.FileOpeningType.UniversalConversion && FileSourceIfNotSaveable != null)
                        {
                            ActivateConversion(FileSourceIfNotSaveable);
                            return;
                        }
                        else
                        {
                            conversion = pdftron.PDF.Convert.UniversalConversion(OriginalFile.Path, opts);
                        }
                    }
                    if (conversion != null)
                    {
                        PDFViewCtrl.OpenUniversalDocument(conversion);
                    }
                    else
                    {
                        FileLoadingError = true;
                    }
                }
                catch (Exception)
                {
                    FileLoadingError = true;
                }
                if (FileLoadingError)
                {
                    return;
                }
               
            }

            UpdateReflowState();

            MetaData.TabLastViewedTimeStamp = DateTime.Now;
        }

        private bool _ActivatingConversion = false;
        private async void ActivateConversion(StorageFile file)
        {
            if (_ActivatingConversion)
            {
                return;
            }
            _ActivatingConversion = true;
            DocumentConversion conversion = null;
            try
            {
                WordToPDFOptions opts = new WordToPDFOptions();
                opts.SetLayoutResourcesPluginPath(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Resources"));
                opts.SetSmartSubstitutionPluginPath(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Resources"));
                Windows.Storage.Streams.IRandomAccessStream iras = await file.OpenReadAsync();
                pdftron.Filters.IFilter filter = new pdftron.Filters.RandomAccessStreamFilter(iras);
                conversion = pdftron.PDF.Convert.UniversalConversion(filter, null);
            }
            catch (Exception)
            {

            }
            finally
            {
                _ActivatingConversion = false;
            }
            if (conversion != null)
            {
                PDFViewCtrl.OpenUniversalDocument(conversion);
            }
            else
            {
                FileLoadingError = true;
            }

            UpdateReflowState();
            MetaData.TabLastViewedTimeStamp = DateTime.Now;
        }

        public void CloseOutline()
        {
            if (_OutlineDialogViewModel != null)
            {
                _OutlineState = _OutlineDialogViewModel.GetState();
                _OutlineDialogViewModel.SaveBookmarks();
                _OutlineDialogViewModel.NavigationStack = null;
                _OutlineDialogViewModel.PDFViewCtrl = null;
                _OutlineDialogViewModel.AnnotationList._ViewModel.ClearViewModel();
                _OutlineDialogViewModel.Thumbnails.ViewModel.CleanUp();
            }
            _OutlineDialogViewModel = null;
        }

        public async Task CloseOutlineAsync()
        {
            if (_OutlineDialogViewModel != null)
            {
                OutlineDialogViewModel vmToClose = _OutlineDialogViewModel;
                _OutlineState = _OutlineDialogViewModel.GetState();
                _OutlineDialogViewModel.SaveBookmarks();
                _OutlineDialogViewModel.NavigationStack = null;
                _OutlineDialogViewModel.PDFViewCtrl = null;
                _OutlineDialogViewModel.AnnotationList._ViewModel.ClearViewModel();
                _OutlineDialogViewModel.Thumbnails.ViewModel.CleanUp();
                _OutlineDialogViewModel = null;
                await vmToClose.UserBookmarks.WaitForBookmarkSavingAsync();
                await vmToClose.AnnotationList.WaitForAnnotationListToFinish();
            }

        }

        private void UpdateReflowState()
        {
            if (ReflowView == null && IsReflow && !_IsPaused && !IsWaitingForConversionToStart)
            {
                ReflowView = new ReflowView(Doc, PDFViewCtrl.GetCurrentPage());
                ReflowView.ReflowTapped += ReflowView_ReflowTapped;
            }
            else if (ReflowView != null && (!IsReflow || _IsPaused))
            {
                ReflowView.DeactivateView();
                ReflowView.ReflowTapped -= ReflowView_ReflowTapped;
                ReflowView = null;
            }
        }

        private void ReflowView_PageChanged(int page)
        {
            _PDFViewCtrl.SetCurrentPage(page);
        }

        private void ReflowView_ReflowTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ReflowTapped?.Invoke(sender, e);
        }

        private void PDFViewCtrlBeforeTools_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (TappedBeforeTools != null)
            {
                TappedBeforeTools(sender, e);
            }
        }

        private void PDFViewCtrlAfterTools_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (TappedAfterTools != null)
            {
                TappedAfterTools(sender, e);
            }
        }

        public void ReActivate()
        {
            CleanUp();
            Doc = null;
            DisposeWhenSaved = false;
            _Active = false;
            Activate();
        }

        public void Pause()
        {
            _IsPaused = true;
            UpdateReflowState();
        }

        public void Resume()
        {
            _IsPaused = false;
            UpdateReflowState();
        }

        //public bool IsSaving = false;
        private SemaphoreSlim _DocSavingSemaphore;
        public SemaphoreSlim DocSavingSemaphore
        {
            get 
            { 
                if (_DocSavingSemaphore == null)
                {
                    _DocSavingSemaphore = new SemaphoreSlim(1);
                }
                return _DocSavingSemaphore;
            }
        }

        public void DocSemWait([System.Runtime.CompilerServices.CallerMemberName] string member = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
#if DEBUG
            if (CompleteReaderTabControlViewModel._ShowSemaphoreLogging)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} waiting for doc Semaphore for {2}", member, line, Title);
            }
#endif
            SemaphoreSlim tmpSema = DocSavingSemaphore;
            tmpSema.Wait();
#if DEBUG
            if (CompleteReaderTabControlViewModel._ShowSemaphoreLogging)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} got doc Semaphore for {2}", member, line, Title);
            }
#endif
        }

        public async Task DocSemWaitAsync([System.Runtime.CompilerServices.CallerMemberName] string member = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
#if DEBUG
            if (CompleteReaderTabControlViewModel._ShowSemaphoreLogging)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} waiting async for doc Semaphore for {2}", member, line, Title);
            }
#endif
            SemaphoreSlim tmpSema = DocSavingSemaphore;
            await tmpSema.WaitAsync().ConfigureAwait(false);
#if DEBUG
            if (CompleteReaderTabControlViewModel._ShowSemaphoreLogging)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} got async doc Semaphore for {2}", member, line, Title);
            }
#endif
        }

        public void DocSemRelease([System.Runtime.CompilerServices.CallerMemberName] string member = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
#if DEBUG
            if (CompleteReaderTabControlViewModel._ShowSemaphoreLogging)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} released doc Semaphore for {2}", member, line, Title);
            }
#endif
            SemaphoreSlim tmpSema = DocSavingSemaphore;
            tmpSema.Release();
        }

        public bool DisposeWhenSaved = false;
        public void CleanUp()
        {
            if (PDFViewCtrl != null)
            {
                if (_ConversionChangedHandler != null)
                {
                    _PDFViewCtrl.OnConversionChanged -= _ConversionChangedHandler;
                    _ConversionChangedHandler = null;
                }
                PDFViewCtrl.Tapped -= PDFViewCtrlBeforeTools_Tapped;
                PDFViewCtrl.Tapped -= PDFViewCtrlAfterTools_Tapped;
                PDFViewCtrl.CloseDoc();
                ShowConversionProgress = false;
            }
            if (ReflowView != null)
            {
                ReflowView.ReflowTapped -= ReflowView_ReflowTapped;
            }
            if (Doc != null)
            {
                DocSemWait();
                try
                {
                    PDFDoc doc = Doc;
                    Doc = null;
                    CloseDoc(doc);
                }
                catch (Exception) { }
                finally
                {
                    DocSemRelease();
                }
            }
            if (_SetDocHandler != null)
            {
                if (PDFViewCtrl != null)
                {
                    PDFViewCtrl.OnSetDoc -= _SetDocHandler;
                }
                _SetDocHandler = null;
            }
            if (_ThumbnailHandler != null)
            {
                if (PDFViewCtrl != null)
                {
                    PDFViewCtrl.OnThumbnailGenerated -= _ThumbnailHandler;
                }
                _ThumbnailHandler = null;
            }

            if (ToolManager != null)
            {
                ToolManager.CreateDefaultTool();
                ToolManager.Dispose();
                ToolManager = null;
            }
            if (NavigationStack != null)
            {
                NavigationStack = null;
            }
            PDFViewCtrl = null;
            _Active = false;
        }

        public async void SaveDocument()
        {
            await SaveDocumentAsync();
        }

        public async Task SaveDocumentAsync()
        {
            if (DocumentState == OpenedDocumentStates.Uneditable || DocumentState == OpenedDocumentStates.Universal)
            {
                return;
            }
            await DocSemWaitAsync().ConfigureAwait(false);
            bool locked = false;
            try
            {
                if (_OutlineDialogViewModel != null && _OutlineDialogViewModel.HasUnsavedUserbookmarks)
                {
                    _OutlineDialogViewModel.UserBookmarks.SaveBookmarks();
                    await _OutlineDialogViewModel.CleanUpSubViewsAsync();
                }
                if (Doc != null)
                {
                    Doc.LockRead();
                    locked = true;
                    if (Doc.IsModified())
                    {
                        Doc.UnlockRead();
                        locked = false;
                        if ((MetaData.DocumentState == OpenedDocumentStates.Corrupted || MetaData.DocumentState == OpenedDocumentStates.CorruptedAndModified)
                            && !MetaData.IsBrokenDocumentRestored)
                        {
                            await Doc.SaveAsync(pdftron.SDF.SDFDocSaveOptions.e_remove_unused).AsTask().ConfigureAwait(false);
                        }
                        else
                        {
                            await Doc.SaveAsync().AsTask().ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_APP, pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex));
            } 
            finally
            {
                if (locked)
                {
                    Doc.UnlockRead();
                }
                DocSemRelease();
            }
        }

        public void UpdateIsReflow()
        {
            RaisePropertyChanged("IsReflow");
            UpdateReflowState();
        }

        public void UpdatePreview()
        {
            MetaData.IsThumbnailUpToDate = false;
            if (PDFViewCtrl != null)
            {
                if (PDFViewCtrl.GetColorPostProcessMode() == PDFRasterizerColorPostProcessMode.e_postprocess_none)
                {
                    try
                    {
                        PDFViewCtrl.GetThumbAsync(1);
                    }
                    catch (Exception) { }
                }
            }
        }

        public async void UpdateFile()
        {
            await DocSemWaitAsync();
            bool didUpdateDoc = false;
            try
            {
                if (!IsUpToDate)
                {
                    PDFReady = false;
                    UpdatingFile = true;
                    if (Doc != null) // need to set it to null so that CleanUp doesn't try to lock.
                    {
                        if (PDFViewCtrl != null)
                        {
                            PDFViewCtrl.CloseDoc();
                        }
                        Doc.Dispose();
                        Doc = null;
                    }

                    if (_Active)
                    {
                        CleanUp();
                    }
                    await this.OriginalFile.CopyAndReplaceAsync(this.PDFFile);
                    this.MetaData.LastModifiedDate = SystemLastModifiedDate;
                    this.MetaData.FileSize = this.SystemFileSize;
                    this.ShowHasUnsavedChanged = false;
                    this.IsDocumentModifiedSinceLastSave = false;
                    didUpdateDoc = true;
                }
            }
            catch (Exception e) { System.Diagnostics.Debug.WriteLine("Error updating tab: " + e.ToString()); }
            finally
            {
                UpdatingFile = false;
                DocSemRelease();
            }
            if (didUpdateDoc)
            {
                if (PdfUpdated != null)
                {
                    PdfUpdated(this, new RoutedEventArgs());
                }
                Activate();
            }
        }

        #endregion Pulbic Functions

        private bool _IsPaused = false;
        private bool _SettingDoc = false;
        OnSetDocEventHandler _SetDocHandler = null;
        void PDFViewCtrl_OnSetDoc()
        {
            _SettingDoc = false;

            if (Settings.Settings.RememberLastPage)
            {
                PDFViewCtrl.SetCurrentPage(this.MetaData.LastPage);
                double desiredHPos = MetaData.HScrollPos;
                double desiredVPos = MetaData.VScrollPos;
                if (MetaData.Zoom > 0)
                {
                    PDFViewCtrl.SetZoom(MetaData.Zoom);
                    if (Math.Abs(PDFViewCtrl.GetZoom() - MetaData.Zoom) > 0.01)
                    {
                        double zoomDifference = PDFViewCtrl.GetZoom() / MetaData.Zoom;
                        desiredHPos *= zoomDifference;
                        desiredVPos *= zoomDifference;
                    }
                }

                PDFViewCtrl.SetHScrollPos(desiredHPos);
                PDFViewCtrl.SetVScrollPos(desiredVPos);

                if (IsReflow && ReflowView != null)
                {
                    ReflowView.CurrentPage = this.MetaData.LastPage;
                    PDFViewCtrl.CancelRendering();
                }
            }

            PDFViewCtrl.Opacity = 1;
        }

        private OnConversionEventHandler _ConversionChangedHandler= null;
        private async void PDFViewCtrl_OnConversionChanged(PDFViewCtrlConversionType type, int totalPagesConverted)
        {
            bool wasWaitingForConversion = IsWaitingForConversionToStart;
            IsWaitingForConversionToStart = false;
            if (type == PDFViewCtrlConversionType.e_conversion_failed || type == PDFViewCtrlConversionType.e_conversion_finished)
            {
                PDFViewCtrl.OnConversionChanged -= _ConversionChangedHandler;
                _ConversionChangedHandler = null;
                ShowConversionProgress = false;
            }
            //System.Diagnostics.Debug.WriteLine("conversion changed!!!! " + type + ", total pages: " + totalPagesConverted);
            PDFViewCtrl.Opacity = 1;
            switch(type)
            {
                case PDFViewCtrlConversionType.e_conversion_progress:
                    Doc = PDFViewCtrl.GetDoc();
                    if (wasWaitingForConversion)
                    {
                        UpdateReflowState();
                        PDFViewCtrl.GetThumbAsync(1);
                    }
                    else if (IsReflow && ReflowView != null)
                    {
                        ReflowView?.ReflowViewModel.UpdatePageNumberProperties();
                    }
                    // update page numbers
                    break;
                case PDFViewCtrlConversionType.e_conversion_finished:
                    // save and update status
                    PDFDoc doc = PDFViewCtrl.GetDoc();
                    StorageFile tempPDFFile = null;
                    try
                    {
                        Utilities.DocumentManager docManager = Utilities.DocumentManager.Instance;
                        if (!Utilities.DocumentManager.IsReady)
                        {
                            CompleteReader.Utilities.AnalyticsHandler.CURRENT.SendEvent(CompleteReader.Utilities.AnalyticsHandler.Category.FILEBROWSER,
                                "DocumentOpener invoked without DocumentManager ready");
                            System.Diagnostics.Debug.WriteLine("DocumentOpener invoked without DocumentManager ready");
                            docManager = await Utilities.DocumentManager.GetInstanceAsync().ConfigureAwait(false);
                        }
                        System.Diagnostics.Debug.WriteLine("About to ask for temp file");
                        tempPDFFile = await docManager.OpenTemporaryCopyAsync(FileSourceIfNotSaveable, false).ConfigureAwait(false);

                        if (tempPDFFile == null)
                        {
                            tempPDFFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("FromConversion.pdf", CreationCollisionOption.GenerateUniqueName).AsTask().ConfigureAwait(false);
                        }
                    }
                    catch (Exception) { }

                    if (tempPDFFile != null)
                    {

                        await DocSemWaitAsync().ConfigureAwait(false);
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("Trying to create temp doc");
                            System.Diagnostics.Debug.WriteLine("Got a temp file");
                            if (tempPDFFile != null)
                            {
                                System.Diagnostics.Debug.WriteLine("About to save");
                                await doc.SaveToNewLocationAsync(tempPDFFile, pdftron.SDF.SDFDocSaveOptions.e_remove_unused).AsTask().ConfigureAwait(false);
                                System.Diagnostics.Debug.WriteLine("Save completed");
                            }
                        }
                        catch (Exception ex)
                        {
                            AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_APP, pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex));
                        }
                        finally
                        {
                            DocSemRelease();
                        }

                        if (PDFViewCtrl != null)
                        {
                            await PDFViewCtrl.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                if (PDFViewCtrl != null) // can happen if someone closes a tab that just finished converting
                                {

                                    PDFFile = tempPDFFile;
                                    Doc = doc;
                                    DocumentState = OpenedDocumentStates.ReadOnly;
                                    PdfUpdated?.Invoke(this, new RoutedEventArgs());
                                    PDFViewCtrl.GetThumbAsync(1);
                                    PDFReady = true;

                                    if (_OutlineDialogViewModel != null)
                                    {
                                        _OutlineDialogViewModel.Isconverting = false;
                                    }
                                }
                            });
                        }
                    }
                    else
                    {
                        DocumentState = OpenedDocumentStates.Uneditable;
                    }
                    break;
                case PDFViewCtrlConversionType.e_conversion_failed:
                    // notify
                    break;
            }
        }


        private OnThumbnailGeneratedEventHandler _ThumbnailHandler = null;
        async void PDFViewCtrl_OnThumbnailGenerated(int pageNumber, bool wasThumbGenerated, byte[] thumb, int w, int h)
        {
            if (pageNumber != 1 || !ShowPreview || MetaData.IsThumbnailUpToDate || 
                PDFViewCtrl.GetColorPostProcessMode() != PDFRasterizerColorPostProcessMode.e_postprocess_none)
            {
                return;
            }

            if (!wasThumbGenerated)
            {
                UpdatePreview();
                return;
            }

            StorageFolder folder = null;

            try
            {
                folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(TabPreviewFolderName, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception) {}
            
            if (folder == null)
            {
                return;
            }

            string filename = Guid.NewGuid().ToString() + ".bmp";
            StorageFile file = null;
            Windows.Storage.Streams.IRandomAccessStream fileStream = null;
            try
            {
                file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
            }
            catch (Exception) { }

            if (fileStream == null)
            {
                return;
            }

            try
            {
                BitmapEncoder enco = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, fileStream);

                uint maxSide = 500;
                uint pixW = maxSide;
                uint pixH = maxSide;

                if (w > h)
                {
                    pixH = (uint)(h * (maxSide / (double)w));
                }
                else
                {
                    pixW = (uint)(w * (maxSide / (double)h));
                }

                enco.BitmapTransform.ScaledHeight = pixH;
                enco.BitmapTransform.ScaledWidth = pixW;
                enco.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)w, (uint)h, 72, 72, thumb);

                await enco.FlushAsync();
                fileStream.Dispose();
                try
                {
                    if (!string.IsNullOrEmpty(MetaData.ThumbnailLocation))
                    {
                        StorageFile oldFile = await folder.GetFileAsync(MetaData.ThumbnailLocation);
                        await oldFile.DeleteAsync();
                    }
                }
                catch (Exception) { }

                MetaData.ThumbnailLocation = file.Name;
                MetaData.IsThumbnailUpToDate = true;
                RaisePropertyChanged("PreviewSource");
            }
            catch (Exception) {}

        }

        private async void CloseDoc(PDFDoc doc)
        {
            await Task.Run(() =>
                {
                    try
                    {
                        doc.Dispose();
                    }
                    catch (Exception) { }
                });
        }
    }

    /// <summary>
    /// This class lets you put multiple PDFViewCtrl's into a tab like control.
    /// You can use the TabControlViewModel.TabControlButtonViewModel property to get a
    /// complimentary view model for Tab Selection.
    /// 
    /// Note: Always dispose the TabControlViewModel explicitly when done with it, as this will free up the ToolManagers.
    /// </summary>
    public class CompleteReaderTabControlViewModel : ViewModelBase
    {
        private const double MAX_RELATIVE_ZOOM_LIMIT = 20;
        private const double MIN_RELATIVE_ZOOM_LIMIT =  1.0;

        private static CompleteReaderTabControlViewModel _Instance = null;
        public static CompleteReaderTabControlViewModel Instance
        {
            get 
            {
                if (IsReady)
                {
                    return _Instance;
                }
                return null;
            }
        }

        private static bool _IsReady = false;
        public static bool IsReady
        {
            get { return _IsReady; }
        }

        private bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { Set(ref _IsEnabled, value); }
        }

        public static async Task<CompleteReaderTabControlViewModel> GetInstanceAsync()
        {
            await LoadingSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsReady)
                {
                    if (_Instance == null)
                    {
                        _Instance = new CompleteReaderTabControlViewModel();
                        List<CompleteReaderPDFViewCtrlTabInfo> tabList = await _Instance.LoadTabStateAsync();
                        if (tabList.Count > 0)
                        {
                            if (Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                            {
                                foreach (CompleteReaderPDFViewCtrlTabInfo tab in tabList)
                                {
                                    _Instance.Tabs.Add(tab);
                                }
                            }
                            else
                            {
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                   {
                                       foreach (CompleteReaderPDFViewCtrlTabInfo tab in tabList)
                                       {
                                           _Instance.Tabs.Add(tab);
                                       }
                                   });
                            }
                        }
                        _Instance._LastFilePropertyUpdatedTime = DateTime.Now;
                        _Instance.ShowIfDocumentModified = CompleteReaderPDFViewCtrlTabInfo.ShowIfModifedStates.DoNotShow;
                    }
                    _IsReady = true;
                }
            }
            catch (Exception e) { System.Diagnostics.Debug.WriteLine("Error: " + e.ToString()); }
            finally
            {
                LoadingSemaphore.Release();
            }
            return _Instance;
        }

        private static SemaphoreSlim _LoadingSemaphore = null;
        private static SemaphoreSlim LoadingSemaphore
        {
            get
            {
                if (_LoadingSemaphore == null)
                {
                    _LoadingSemaphore = new SemaphoreSlim(1);
                }
                return _LoadingSemaphore;
            }
        }

        internal const bool _ShowSemaphoreLogging = false;
        private SemaphoreSlim _AccessSemaphore;
        private void SemWait(bool supress = false, [System.Runtime.CompilerServices.CallerMemberName] string member = "", 
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
#if DEBUG
            if (_ShowSemaphoreLogging && !supress)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} waiting for Semaphore", member, line);
            }
#endif
            SemaphoreSlim tmpSema = _AccessSemaphore;
            tmpSema.Wait();
#if DEBUG
            if (_ShowSemaphoreLogging && !supress)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} got Semaphore", member, line);
            }
#endif
        }

        private async Task SemWaitAsync(bool supress = false, [System.Runtime.CompilerServices.CallerMemberName] string member = "", 
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
#if DEBUG
            if (_ShowSemaphoreLogging && !supress)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} waiting async for Semaphore", member, line);
            }
#endif
            SemaphoreSlim tmpSema = _AccessSemaphore;
            await tmpSema.WaitAsync().ConfigureAwait(false);
#if DEBUG
            if (_ShowSemaphoreLogging && !supress)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} got async Semaphore", member, line);
            }
#endif
        }

        private void SemRelease(bool supress = false, [System.Runtime.CompilerServices.CallerMemberName] string member = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
#if DEBUG
            if (_ShowSemaphoreLogging && !supress)
            {
                System.Diagnostics.Debug.WriteLine("Function {0}, line {1} released Semaphore", member, line);
            }
#endif
            SemaphoreSlim tmpSema = _AccessSemaphore;
            tmpSema.Release();
        }

        public CompleteReaderTabControlViewModel()
        {
            _Tabs = new ObservableCollection<CompleteReaderPDFViewCtrlTabInfo>();
            _FutureAccessListTokensToDelete = new List<string>();
            _AccessSemaphore = new SemaphoreSlim(1);
        }

        public void CleanUp()
        {
            foreach (CompleteReaderPDFViewCtrlTabInfo tab in Tabs)
            {
                tab.CleanUp();
            }
            StopAutoSaving();
        }

        #region Events

        public delegate void VisibleTabChangedDelegate(CompleteReaderTabControlViewModel tabControlViewModel);
        public event VisibleTabChangedDelegate VisibleTabChanged;

        public delegate void FixedButtonClickedDelegate(CompleteReaderTabControlViewModel tabControlViewModel);
        public event FixedButtonClickedDelegate FixedButtonClicked;

        public delegate void CloseButtonClickedDelegate(CompleteReaderTabControlViewModel tabControlViewModel, CompleteReaderPDFViewCtrlTabInfo clickedTab);
        public event CloseButtonClickedDelegate CloseButtonClicked;

        #endregion Events


        #region Properties

        private ObservableCollection<CompleteReaderPDFViewCtrlTabInfo> _Tabs;
        public ObservableCollection<CompleteReaderPDFViewCtrlTabInfo> Tabs { get { return _Tabs; } }


        private string _DefaultImageURL = "ms-appx:///pdftron.PDF.Tools/Controls/Resources/DefaultFileIcon.png";
        public string DefaultImageURL
        {
            get { return _DefaultImageURL; }
            set { _DefaultImageURL = value; }
        }

        public int OpenTabs
        {
            get
            {
                if (Tabs.Count > 0 && Tabs[Tabs.Count - 1].IsFixedItem)
                {
                    return Tabs.Count - 1;
                }
                return Tabs.Count;
            }
        }

        public CompleteReaderPDFViewCtrlTabInfo OldestViewedTab
        {
            get
            {
                if (Tabs.Count == 0 || (Tabs.Count == 1 && Tabs[0].IsFixedItem))
                {
                    return null;

                }
                CompleteReaderPDFViewCtrlTabInfo oldest = Tabs[0];
                foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
                {
                    if (!info.IsFixedItem)
                    {
                        if (info.MetaData.TabLastViewedTimeStamp < oldest.MetaData.TabLastViewedTimeStamp)
                        {
                            oldest = info;
                        }
                    }

                }
                return oldest;
            }
        }

        private bool _AutoSaveState = false;
        public bool AutoSaveState
        {
            get { return _AutoSaveState; }
            set
            {
                if (value != _AutoSaveState)
                {
                    _AutoSaveState = value;
                    if (_AutoSaveState)
                    {
                        StartAutoSaving();
                    }
                    else
                    {
                        StopAutoSaving();
                    }
                }
            }
        }

        private CompleteReaderPDFViewCtrlTabInfo.ShowIfModifedStates _ShowIfDocumentModified = CompleteReaderPDFViewCtrlTabInfo.ShowIfModifedStates.DoNotShow;
        [System.Xml.Serialization.XmlIgnore]
        public CompleteReaderPDFViewCtrlTabInfo.ShowIfModifedStates ShowIfDocumentModified
        {
            get { return _ShowIfDocumentModified; }
            set
            {
                if (value != _ShowIfDocumentModified)
                {
                    _ShowIfDocumentModified = value;
                    foreach (CompleteReaderPDFViewCtrlTabInfo tab in Tabs)
                    {
                        tab.ShowIfDocumentModified = _ShowIfDocumentModified;
                    }
                }
            }
        }

        #endregion Properties


        #region Public Functions

        public void AddFixedItemToEnd()
        {
            CompleteReaderPDFViewCtrlTabInfo info = CompleteReaderPDFViewCtrlTabInfo.CreateFixedTab();
            info.ItemClicked += TabItem_ItemClicked;
            if (Tabs.Count > 0)
            {
                Tabs.Insert(Tabs.Count - 1, info);
            }
            else
            {
                Tabs.Add(info);
            }
        }

        public CompleteReaderPDFViewCtrlTabInfo AddTab(StorageFile orig, StorageFile tmp, PDFDoc doc, string title)
        {
            CompleteReaderPDFViewCtrlTabInfo newInfo = new CompleteReaderPDFViewCtrlTabInfo(orig, tmp, doc, title);
            AddTab(newInfo);
            return newInfo;
        }

        public void AddTab(CompleteReaderPDFViewCtrlTabInfo newInfo)
        {
            newInfo.SetUpPDFViewCtrlAndTools = SetUpPDFViewCtrlAndTools;
            bool usingFixedTab = Tabs.Count > 0 && Tabs[Tabs.Count - 1].IsFixedItem;

            newInfo.ShowPreview = ShowImagePreviews;
            newInfo.ThumbnailFallback = DefaultImageURL;
            newInfo.ItemClicked += TabItem_ItemClicked;
            newInfo.PdfUpdated += TabItem_PdfUpdated;
            newInfo.MetaData.TabAddedTimeStamp = DateTime.Now;
            newInfo.ShowIfDocumentModified = ShowIfDocumentModified;

            if (usingFixedTab)
            {
                Tabs.Insert(Tabs.Count - 1, newInfo);
            }
            else
            {
                Tabs.Add(newInfo);
            }

            ResolveFixedItem();
            ResolveFirstAndLast();

            SaveFilesForFutureUse(newInfo);
            CleanUpFutureAccessList();

        }

        public CompleteReaderPDFViewCtrlTabInfo ReplaceTab(int index, StorageFile orig, StorageFile tmp, PDFDoc doc, string title)
        {
            if (index < 0 || index >= OpenTabs)
            {
                throw new IndexOutOfRangeException(string.Format("Tabs only has {0} items, index {1} is out of range.", Tabs.Count, index));
            }

            CompleteReaderPDFViewCtrlTabInfo newInfo = new CompleteReaderPDFViewCtrlTabInfo(orig, tmp, doc, title);
            ReplaceTab(index, newInfo);
            return newInfo;
        }

        protected void ReplaceTab(int index, CompleteReaderPDFViewCtrlTabInfo newInfo)
        {
            if (index < 0 || index >= OpenTabs)
            {
                throw new IndexOutOfRangeException(string.Format("Tabs only has {0} items, index {1} is out of range.", OpenTabs, index));
            }

            CompleteReaderPDFViewCtrlTabInfo oldTab = Tabs[index];
            oldTab.CleanUp();

            newInfo.SetUpPDFViewCtrlAndTools = SetUpPDFViewCtrlAndTools;
            Tabs[index] = newInfo;
            newInfo.ShowPreview = ShowImagePreviews;
            newInfo.ThumbnailFallback = DefaultImageURL;
            newInfo.ItemClicked += TabItem_ItemClicked;
            newInfo.MetaData.TabAddedTimeStamp = DateTime.Now;

            ResolveFirstAndLast();

            SaveFilesForFutureUse(newInfo);

            RemoveTabFromFutureAccessList(oldTab);

            SemWait();
            try
            {
                SaveTabStateNoLock(Tabs.IndexOf(newInfo));
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void CloseTab(int index, bool activateNewTab = true)
        {
            if (index < 0 || index >= OpenTabs)
            {
                return;
            }
            CloseTab(Tabs[index], activateNewTab);
        }

        public void CloseTab(CompleteReaderPDFViewCtrlTabInfo tab, bool activateNewTab = true)
        {
            Utilities.AnalyticsHandler.CURRENT.SendEvent("Tabs", "Tab Closed");
            RemoveTabFromFutureAccessList(tab);
            tab.CleanUp();
            Tabs.Remove(tab);

            if (activateNewTab)
            {
                UpdateSelectionIfNecessary();
            }
            ResolveFixedItem();
            ResolveFirstAndLast();

            SaveTabState();
        }

        public void CloseOldestAddedTab()
        {
            if (Tabs.Count == 0)
            {
                return;
            }
            CompleteReaderPDFViewCtrlTabInfo oldest = Tabs[0];
            foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
            {
                if (info.MetaData.TabAddedTimeStamp < oldest.MetaData.TabAddedTimeStamp)
                {
                    oldest = info;
                }
            }
            CloseTab(Tabs.IndexOf(oldest));
        }

        public void CloseOldestViewedTab()
        {
            if (Tabs.Count == 0)
            {
                return;
            }
            CompleteReaderPDFViewCtrlTabInfo oldest = Tabs[0];
            foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
            {
                if (info.MetaData.TabLastViewedTimeStamp < oldest.MetaData.TabLastViewedTimeStamp)
                {
                    oldest = info;
                }
            }
            CloseTab(Tabs.IndexOf(oldest));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void SelectTab(int index)
        {
            for (int i = 0; i < Tabs.Count; ++i)
            {
                if (i != index)
                {
                    if (Tabs[i].IsSelected && Tabs[i].PDFViewCtrl != null)
                    {
                        if (Tabs[i].IsReflow)
                        {
                            Tabs[i].PDFViewCtrl.SetCurrentPage(Tabs[i].ReflowView.CurrentPage);
                        }
                        Tabs[i].PDFViewCtrl.Deactivate();
                        SemWait();
                        try
                        {
                            SaveTabStateNoLock(i);
                        }
                        catch (Exception) { }
                        finally
                        {
                            SemRelease();
                        }
                        Tabs[i].SaveDocument();
                    }
                    Tabs[i].IsSelected = false;
                }
            }
            if (index < Tabs.Count && !Tabs[index].IsFixedItem)
            {
                if (!Tabs[index].IsSelected)
                {
                    Utilities.AnalyticsHandler.CURRENT.SendEvent("Tabs", "Tab clicked");
                }
                Tabs[index].IsSelected = true;
                SelectedTab = Tabs[index];
                if (SelectedTab.PDFViewCtrl != null)
                {
                    SelectedTab.PDFViewCtrl.Activate();
                }
            }
            AutoSaveState = SelectedTab.CanBeSaved;
        }

        public void SelectTab(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            SelectTab(Tabs.IndexOf(tab));
        }

        public void ShowLastViewedTab()
        {
            UpdateSelectionIfNecessary();
        }

        public bool ContainsFile(StorageFile file)
        {
            SemWait();
            try
            {
                foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
                {
                    if (!info.IsFixedItem && (
                        (info.PDFFile != null && Utilities.UtilityFunctions.AreFilesEqual(info.PDFFile, file))
                        ||
                        (info.OriginalFile != null && Utilities.UtilityFunctions.AreFilesEqual(info.OriginalFile, file))
                        ||
                        (info.FileSourceIfNotSaveable != null && Utilities.UtilityFunctions.AreFilesEqual(info.FileSourceIfNotSaveable, file))
                        ))
                    {
                        return true;
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }
            return false;
        }

        public bool ContainsFilePath(string filePath)
        {
            SemWait();
            try
            {
                foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
                {
                    if (!info.IsFixedItem
                        &&
                        ((info.PDFFile != null && info.PDFFile.Path.Equals(filePath))
                        ||
                        (info.OriginalFile != null && info.OriginalFile.Path.Equals(filePath))))
                    {
                        return true;
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }
            return false;
        }

        /// <summary>
        /// If the file is already in a tab, activates that tab and returns true. Otherwise, returns false.
        /// This function is virtual as it may not behave as expected with files from all sources, such as remote storage which may create a new temp file each time.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public virtual bool CheckIfFileIsOpenAndActivate(StorageFile file)
        {
            SemWait();
            CompleteReaderPDFViewCtrlTabInfo tabtoActivate = null;
            try
            {
                foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
                {
                    if (!info.IsFixedItem)
                    {
                        if (info.PDFFile != null && Utilities.UtilityFunctions.AreFilesEqual(info.PDFFile, file))
                        {
                            tabtoActivate = info;
                            break;
                        }
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }
            if (tabtoActivate != null)
            {
                SelectTab(Tabs.IndexOf(tabtoActivate));
                return true;
            }

            return false;
        }

        public bool CheckIfOriginalIsOpenAndActivate(StorageFile file)
        {
            SemWait();
            CompleteReaderPDFViewCtrlTabInfo tabtoActivate = null;
            try
            {
                foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
                {
                    if (!info.IsFixedItem)
                    {
                        if (info.FileSourceIfNotSaveable != null && Utilities.UtilityFunctions.AreFilesEqual(info.FileSourceIfNotSaveable, file))
                        {
                            tabtoActivate = info;
                            break;
                        }
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }
            if (tabtoActivate != null)
            {
                SelectTab(Tabs.IndexOf(tabtoActivate));
                return true;
            }
            return false;
        }

        public void SaveTabState()
        {
            SemWait();
            try
            {
                TabSetting.OpenTabs = OpenTabs;
                for (int i = 0; i < OpenTabs; i++)
                {
                    SaveTabStateNoLock(i);
                }
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }
        }

        /// <summary>
        /// Makes no tab selected. Reactivate by selecting a tab.
        /// It is up to you to save the document in the current tab before Deactivating it.
        /// </summary>
        public void Deactivate()
        {
            if (SelectedTab != null)
            {
                try
                {
                    SemWait();
                    SaveTabStateNoLock(VisibleTabIndex);
                }
                catch (Exception) { }
                finally
                {
                    SemRelease();
                }
                SelectedTab.IsSelected = false;
            }
            foreach (CompleteReaderPDFViewCtrlTabInfo tab in Tabs)
            {
                if (!tab.IsFixedItem)
                {
                    tab.CleanUp();
                }
            }
            SelectedTab = null;
            AutoSaveState = false;
        }

        private DateTime _LastFilePropertyUpdatedTime = DateTime.MinValue;
        public async Task UpdateFilePropertiesAsync()
        {
            await SemWaitAsync().ConfigureAwait(false);
            try
            {
                TimeSpan timeSinceUpdate = DateTime.Now - _LastFilePropertyUpdatedTime;
                if (timeSinceUpdate > TimeSpan.FromSeconds(3))
                {
                    foreach (CompleteReaderPDFViewCtrlTabInfo tab in Tabs)
                    {
                        if (!tab.IsFixedItem)
                        {
                            if (tab.OriginalFile != null)
                            {
                                Windows.Storage.FileProperties.BasicProperties props = await tab.OriginalFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
                                DateTimeOffset origOffset = tab.SystemLastModifiedDate;
                                tab.SystemFileSize = props.Size;
                                tab.SystemLastModifiedDate = props.DateModified;

                                if (origOffset != tab.SystemLastModifiedDate)
                                {
                                    // Note: optional code
                                }
                            }
                        }
                    }
                    _LastFilePropertyUpdatedTime = DateTime.Now;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Skipping");
                }
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }
        }

        #endregion Public Functions


        #region Visual Properties

        private CompleteReaderPDFViewCtrlTabInfo _SelectedTab;
        public CompleteReaderPDFViewCtrlTabInfo SelectedTab
        {
            get { return _SelectedTab; }
            private set
            {
                if (value != _SelectedTab)
                {

                    _SelectedTab = value;
                    VisibleTabIndex = Tabs.IndexOf(_SelectedTab);
                    RaisePropertyChanged();
                    RaiseVisibleTabChanged();
                }
            }
        }

        private int _VisibleTabIndex = 0;
        public int VisibleTabIndex
        {
            get { return _VisibleTabIndex; }
            private set
            {
                if (value != _VisibleTabIndex)
                {
                    _VisibleTabIndex = value;
                    if (value >= 0)
                    {
                        SelectedTab = Tabs[_VisibleTabIndex];
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private bool _ShowImagePreviews = false;
        public bool ShowImagePreviews
        {
            get { return _ShowImagePreviews; }
            set
            {
                if (value != _ShowImagePreviews)
                {
                    _ShowImagePreviews = value;
                    foreach (CompleteReaderPDFViewCtrlTabInfo item in Tabs)
                    {
                        item.ShowPreview = _ShowImagePreviews;
                    }
                }
            }
        }

        private bool _HasFixedItemAtEnd = false;
        public bool HasFixedItemAtEnd
        {
            get { return _HasFixedItemAtEnd; }
            set
            {
                if (value != _HasFixedItemAtEnd)
                {
                    _HasFixedItemAtEnd = value;
                    ResolveFixedItem();
                }
            }
        }

        private int _MaximumItems = 6;
        public int MaximumItems
        {
            get { return _MaximumItems; }
            set
            {
                if (value != _MaximumItems)
                {
                    _MaximumItems = value;
                    ResolveFixedItem();
                }
            }
        }

        private static int _VMNumberPool = 0;
        private int _VMNumber = ++_VMNumberPool;
        private bool _ShowFixedItem = false;
        public bool ShowFixedItem
        {
            get { return _ShowFixedItem; }
            set
            {
                if (value != _ShowFixedItem)
                {
                    _ShowFixedItem = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion Visual Properties


        #region Impl

        private void RaiseVisibleTabChanged()
        {
            if (VisibleTabChanged != null)
            {
                VisibleTabChanged(this);
            }
        }

        private void RaiseFixedButtonClicked()
        {
            if (FixedButtonClicked != null)
            {
                FixedButtonClicked(this);
            }
        }

        private void TabItem_ItemClicked(CompleteReaderPDFViewCtrlTabInfo item, bool closeRequested)
        {
            int index = Tabs.IndexOf(item);
            if (closeRequested)
            {
                if (CloseButtonClicked != null)
                {
                    CloseButtonClicked(this, item);
                }
                else
                {
                    CloseTab(index);
                }
            }
            else if (item.IsFixedItem)
            {
                Utilities.AnalyticsHandler.CURRENT.SendEvent("Tabs", "Tab Plus Button clicked");
                RaiseFixedButtonClicked();
            }
            else
            {
                SelectTab(index);
            }
        }

        private async void TabItem_PdfUpdated(object sender, RoutedEventArgs e)
        {
            await SemWaitAsync();
            try
            {
                CompleteReaderPDFViewCtrlTabInfo tab = sender as CompleteReaderPDFViewCtrlTabInfo;
                if (tab != null)
                {
                    SaveFilesForFutureUse(tab);
                    int index = Tabs.IndexOf(tab);
                    if (index >= 0 && index < OpenTabs)
                    {
                        TabSetting.SaveSettings(index, tab.MetaData);
                    }
                }
            }
            catch (Exception) {}
            finally
            {
                SemRelease();
            }


        }

        private void ResolveFirstAndLast()
        {
            if (Tabs.Count > 1)
            {
                Tabs[0].First = true;
                Tabs[1].First = false;
                Tabs[Tabs.Count - 1].Last = true;
                Tabs[Tabs.Count - 2].Last = false;
            }
            else if (Tabs.Count == 1)
            {
                Tabs[0].First = true;
                Tabs[0].Last = true;
            }
        }

        private void UpdateSelectionIfNecessary()
        {
            if (Tabs.Count == 0 || (Tabs.Count == 1 && Tabs[0].IsFixedItem))
            {
                SelectedTab = null;
                return;
            }
            CompleteReaderPDFViewCtrlTabInfo lastViewed = Tabs[0];
            foreach (CompleteReaderPDFViewCtrlTabInfo info in Tabs)
            {
                if (info.IsSelected)
                {
                    VisibleTabIndex = Tabs.IndexOf(info);
                    return;
                }
                if (info.IsFixedItem)
                {
                    VisibleTabIndex = -1;
                }
                if (info.MetaData.TabLastViewedTimeStamp > lastViewed.MetaData.TabLastViewedTimeStamp)
                {
                    lastViewed = info;
                }
            }
            SelectTab(lastViewed);
        }

        private void ResolveFixedItem()
        {
            if (HasFixedItemAtEnd && OpenTabs < MaximumItems)
            {
                if (!ShowFixedItem)
                {
                    CompleteReaderPDFViewCtrlTabInfo info = CompleteReaderPDFViewCtrlTabInfo.CreateFixedTab();
                    info.ItemClicked += TabItem_ItemClicked;
                    Tabs.Add(info);
                }
                ShowFixedItem = true;
            }
            else
            {
                if (ShowFixedItem)
                {
                    if (Tabs.Count > 0)
                    {
                        Tabs.RemoveAt(Tabs.Count - 1);
                    }
                }
                ShowFixedItem = false;
            }
            ResolveFirstAndLast();
        }

        #endregion Impl


        #region Saving State

        protected bool _SavingInProgress = false;
        protected List<string> _FutureAccessListTokensToDelete;

        /// <summary>
        /// Always lock around this, as this will not lock
        /// </summary>
        /// <param name="tabIndex"></param>
        private void SaveTabStateNoLock(int tabIndex)
        {
            TabSetting.OpenTabs = OpenTabs;
            if (tabIndex < OpenTabs)
            {
                Tabs[tabIndex].RefreshState();
                SaveFilesForFutureUse(Tabs[tabIndex]);
                TabSetting.SaveSettings(tabIndex, Tabs[tabIndex].MetaData);
            }
        }

        private async Task<List<CompleteReaderPDFViewCtrlTabInfo>> LoadTabStateAsync()
        {
            List<CompleteReaderPDFViewCtrlTabInfo> tabs = new List<CompleteReaderPDFViewCtrlTabInfo>();
            int numTabs = TabSetting.OpenTabs;
            for (int i = 0; i < numTabs; i++)
            {
                try
                {
                    CompleteReaderPDFViewCtrlTabInfo info = null;
                    CompleteReaderPDFViewCtrlTabMetaData metaData = TabSetting.GetFromSettings(i);
                    if (!string.IsNullOrWhiteSpace(metaData.FutureAccessListToken))
                    {
                        
                        StorageFile tmpFile = await GetSavedFileAsync(metaData.FutureAccessListToken).ConfigureAwait(false);
                        if (tmpFile != null)
                        {
                            StorageFile origFile = await GetSavedFileAsync(metaData.OriginalFileToken).ConfigureAwait(false);
                            info = new CompleteReaderPDFViewCtrlTabInfo(origFile, tmpFile, metaData);
                            if (!string.IsNullOrWhiteSpace(metaData.FileSourceTokenIfNotSaveable))
                            {
                                StorageFile sourceFile = await GetSavedFileAsync(metaData.FileSourceTokenIfNotSaveable).ConfigureAwait(false);
                                info.FileSourceIfNotSaveable = sourceFile;
                            }

                            if (origFile != null)
                            {
                                Windows.Storage.FileProperties.BasicProperties props = await origFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
                                info.SystemFileSize = props.Size;
                                info.SystemLastModifiedDate = props.DateModified;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(metaData.FileSourceTokenIfNotSaveable))
                    {
                        StorageFile sourceFile = await GetSavedFileAsync(metaData.FileSourceTokenIfNotSaveable).ConfigureAwait(false);
                        if (sourceFile != null)
                        {
                            if (info == null)
                            {
                                info = CompleteReaderPDFViewCtrlTabInfo.CreateUniversalTabInfo(metaData, sourceFile);
                            }
                            else
                            {
                                info.FileSourceIfNotSaveable = sourceFile;
                            }
                        }
                    }
                    if (info != null)
                    {
                        tabs.Add(info);
                        info.ItemClicked += TabItem_ItemClicked;
                        info.SetUpPDFViewCtrlAndTools = SetUpPDFViewCtrlAndTools;
                    }
                }
                catch (Exception ex)
                {
                    AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_APP, pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex));
                }
            }
            await CleanUpTabStateAsync(tabs).ConfigureAwait(false);
            return tabs;
        }

        //private async void LogAccessListStuff(string title, int pre_count, int post_count)
        //{
        //    StorageItemAccessList accessList = StorageApplicationPermissions.FutureAccessList;
        //    for (int i = 0; i < accessList.Entries.Count; ++i)
        //    {
        //        StorageFile file = null;
        //        try
        //        {
        //            file = await accessList.GetFileAsync(accessList.Entries[i].Token);
        //        }
        //        catch (Exception e)
        //        {

        //        }
        //        System.Diagnostics.Debug.WriteLine("Entry: " + (file == null ? "Not there" : file.Path));
        //    }
        //}

        protected virtual void SaveFilesForFutureUse(CompleteReaderPDFViewCtrlTabInfo info)
        {
            StorageItemAccessList accessList = StorageApplicationPermissions.FutureAccessList;

            //int pre_count = accessList.Entries.Count;

            // Ideally, this should never happen. We're supposed to have clean up code running before we get here.
            if (accessList.Entries.Count + 2 >= accessList.MaximumItemsAllowed)
            {
                accessList.Remove(accessList.Entries[accessList.Entries.Count - 1].Token);
                accessList.Remove(accessList.Entries[accessList.Entries.Count - 1].Token);
                accessList.Remove(accessList.Entries[accessList.Entries.Count - 1].Token);
            }

            if (info.PDFFile != null)
            {
                info.MetaData.FutureAccessListToken = accessList.Add(info.PDFFile);
            }
            else
            {
                info.MetaData.FutureAccessListToken = string.Empty;
            }

            if (info.OriginalFile != null)
            {
                info.MetaData.OriginalFileToken = accessList.Add(info.OriginalFile);
            }
            else
            {
                info.MetaData.OriginalFileToken = string.Empty;
            }

            if (info.FileSourceIfNotSaveable != null)
            {
                info.MetaData.FileSourceTokenIfNotSaveable = accessList.Add(info.FileSourceIfNotSaveable);
            }
            else
            {
                info.MetaData.FileSourceTokenIfNotSaveable = string.Empty;
            }

            //int post_count = accessList.Entries.Count;

            //LogAccessListStuff(info.Title, pre_count, post_count);
        }

        protected virtual async Task<StorageFile> GetSavedFileAsync(string token)
        {
            StorageItemAccessList accessList = StorageApplicationPermissions.FutureAccessList;
            if (accessList.ContainsItem(token))
            {
                StorageFile file = await accessList.GetFileAsync(token);
                return file;
            }
            return null;
        }

        protected virtual void SetUpPDFViewCtrlAndTools(PDFViewCtrl ctrl, ToolManager toolManager)
        {
            ctrl.SetRelativeZoomLimits(PDFViewCtrlPageViewMode.e_fit_page, MIN_RELATIVE_ZOOM_LIMIT, MAX_RELATIVE_ZOOM_LIMIT);
            ctrl.SetProgressiveRendering(true);
            bool isPhone = Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone;
            PDFViewCtrlPageViewMode preferredMode = PDFViewCtrlPageViewMode.e_fit_page;
            if (isPhone)
            {
                preferredMode = PDFViewCtrlPageViewMode.e_fit_width;
            }
            if (Settings.Settings.MaintainZoom)
            {
                ctrl.SetPageRefViewMode(PDFViewCtrlPageViewMode.e_zoom);
            }
            else
            {
                ctrl.SetPageRefViewMode(preferredMode);
            }
            if (ctrl.GetPagePresentationMode() == PDFViewCtrlPagePresentationMode.e_single_continuous)
            {
                ctrl.SetPageViewMode(PDFViewCtrlPageViewMode.e_fit_width);
            }
            else
            {
                ctrl.SetPageViewMode(preferredMode);
            }
            ctrl.SetUrlExtraction(true);
            ctrl.SetupThumbnails(false, true, true, 500, 100 * 1024 * 1024, 0.1);
            Windows.UI.Color viewerBackground = (Windows.UI.Color)App.Current.Resources["MainViewerBackgroundColor"];
            ctrl.SetBackgroundColor(viewerBackground);
            ctrl.SetHighlightFields(true);
            ctrl.SetPageBox(PageBox.e_user_crop);

            if (isPhone)
            {
                // Phones have much less memory than tablets, so we set the memory limit accordingly
                ulong mem = Windows.System.MemoryManager.AppMemoryUsageLimit / (5000000 * 2);
                ctrl.SetRenderedContentCacheSize(mem);
            }
            else
            {
                ulong mem = Windows.System.MemoryManager.AppMemoryUsageLimit / (5000000 * 2);
                mem = Math.Min(mem, 196);
                mem = Math.Max(mem, 96);
                ctrl.SetRenderedContentCacheSize(mem);
            }
            toolManager.EnablePopupMenuOnLongPress = true;
            toolManager.UseSmallPageNumberIndicator = false;
            toolManager.PopupAuthorDialogFirstTime = false;
        }

        const int MAX_ACCESS_LIST_ENTRIES = 60;
        private SemaphoreSlim _FutureAccessListCleanupSemaphore = new SemaphoreSlim(1);
        /// <summary>
        /// Will remove every FutureAccessList item except those that are currently in the tabs.
        /// Override or delete in case you handle this list yourself.
        /// </summary>
        public virtual async void CleanUpFutureAccessList()
        {
            await _FutureAccessListCleanupSemaphore.WaitAsync();
            try
            {
                Collections.PinnedRootFolders rootFolders = await Collections.PinnedRootFolders.GetItemSourceAsync();
                StorageItemAccessList accessList = StorageApplicationPermissions.FutureAccessList;

                if (accessList.Entries.Count < MAX_ACCESS_LIST_ENTRIES + rootFolders.PinnedItems.Count)
                {
                    return;
                }

                List<string> tokensToRemove = new List<string>();
                foreach (AccessListEntry entry in accessList.Entries)
                {
                    bool removeEntry = true;
                    foreach (CompleteReaderPDFViewCtrlTabInfo tab in Tabs)
                    {
                        if (!tab.IsFixedItem &&
                            (entry.Token.Equals(tab.MetaData.FutureAccessListToken) || 
                            entry.Token.Equals(tab.MetaData.OriginalFileToken) || 
                            (tab.MetaData.FileSourceTokenIfNotSaveable != null && entry.Token.Equals(tab.MetaData.FileSourceTokenIfNotSaveable))
                            ))
                        {
                            removeEntry = false;
                            break;
                        }
                    }
                    if (removeEntry)
                    {
                        if (!rootFolders.ContainsToken(entry.Token))
                        {
                            tokensToRemove.Add(entry.Token);
                        }
                    }
                }

                foreach (string token in tokensToRemove)
                {
                    if (accessList.ContainsItem(token))
                    {
                        accessList.Remove(token);
                    }
                }

            }
            catch (Exception) { }
            finally
            {
                _FutureAccessListCleanupSemaphore.Release();
            }
        }

        private static bool _HasCleaned = false;
        private async Task CleanUpTabStateAsync(List<CompleteReaderPDFViewCtrlTabInfo> tabs)
        {
            if (_HasCleaned)
            {
                return;
            }
            _HasCleaned = true;

            try
            {
                IStorageItem storageItem = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(CompleteReaderPDFViewCtrlTabInfo.TabPreviewFolderName);
                StorageFolder folder = null;
                if (storageItem != null && storageItem is StorageFolder)
                {
                    folder = storageItem as StorageFolder;
                }
                else
                {
                    return;
                }
                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
                if (fileList.Count > 20)
                {
                    List<StorageFile> filesToDelete = new List<StorageFile>();
                    foreach (StorageFile file in fileList)
                    {
                        bool toDelete = true;
                        foreach (CompleteReaderPDFViewCtrlTabInfo tab in tabs)
                        {
                            if (!tab.IsFixedItem && !string.IsNullOrEmpty(tab.MetaData.ThumbnailLocation) && tab.MetaData.ThumbnailLocation.Equals(file.Name))
                            {
                                toDelete = false;
                            }
                        }
                        if (toDelete)
                        {
                            filesToDelete.Add(file);
                        }
                    }

                    foreach (StorageFile file in filesToDelete)
                    {
                        try
                        {
                            await file.DeleteAsync();
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception) { }
        }

        protected virtual void RemoveTabFromFutureAccessList(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            if (!string.IsNullOrEmpty(tab.MetaData.FutureAccessListToken))
            {
                _FutureAccessListTokensToDelete.Add(tab.MetaData.FutureAccessListToken);
            }
            if (!string.IsNullOrEmpty(tab.MetaData.OriginalFileToken))
            {
                _FutureAccessListTokensToDelete.Add(tab.MetaData.OriginalFileToken);
            }
            UpdateFutureAccessListWithRemovedTabs();
        }

        protected virtual void UpdateFutureAccessListWithRemovedTabs()
        {
            if (_FutureAccessListTokensToDelete != null && _FutureAccessListTokensToDelete.Count > 0)
            {
                StorageItemAccessList accessList = StorageApplicationPermissions.FutureAccessList;

                foreach (string token in _FutureAccessListTokensToDelete)
                {
                    if (!string.IsNullOrEmpty(token) && accessList.ContainsItem(token))
                    {
                        accessList.Remove(token);
                    }
                }
                _FutureAccessListTokensToDelete.Clear();
            }
        }

        #endregion Saving State


        #region Document Auto-Saving

        private DispatcherTimer _AutoSaveTimer;
        private bool _BlockWhileSaving = false;

        private void StartAutoSaving()
        {
            if (_BlockWhileSaving)
            {
                return;
            }
            _AutoSaveTimer = new DispatcherTimer();
            _AutoSaveTimer.Interval = TimeSpan.FromSeconds(30);
            _AutoSaveTimer.Tick += AutoSaveTimer_Tick;
            _AutoSaveTimer.Start();
        }

        private void StopAutoSaving()
        {
            if (_AutoSaveTimer != null)
            {
                _AutoSaveTimer.Stop();
                _AutoSaveTimer = null;
            }
        }

        private void RefreshAutoSaver()
        {
            if (_BlockWhileSaving)
            {
                return;
            }
            if (AutoSaveState && _AutoSaveTimer != null)
            {
                _AutoSaveTimer.Stop();
                _AutoSaveTimer.Start();
            }
        }

        protected virtual async void AutoSaveTimer_Tick(object sender, object e)
        {
            //return;
            if (_AutoSaveTimer != null)
            {
                _AutoSaveTimer.Stop();
            }
            _BlockWhileSaving = true;
            CompleteReaderPDFViewCtrlTabInfo tab = SelectedTab;
            if (tab != null && !tab.IsFixedItem)
            {
                if (tab != null && tab.Doc != null && tab.Doc.IsModified())
                {
                    await tab.DocSemWaitAsync().ConfigureAwait(false);
                    bool locked = false;
                    try
                    {
                        if (tab.Doc != null)
                        {
                            tab.Doc.LockRead();
                            locked = true;
                            if (tab.Doc.IsModified())
                            {
                                tab.Doc.UnlockRead();
                                locked = false;
                                if ((tab.MetaData.DocumentState == OpenedDocumentStates.Corrupted || tab.MetaData.DocumentState == OpenedDocumentStates.CorruptedAndModified)
                                    && !tab.MetaData.IsBrokenDocumentRestored)
                                {
                                    await tab.Doc.SaveAsync(pdftron.SDF.SDFDocSaveOptions.e_remove_unused).AsTask().ConfigureAwait(false);
                                }
                                else
                                {
                                    await tab.Doc.SaveAsync().AsTask().ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        if (locked)
                        {
                            tab.Doc.UnlockRead();
                        }
                        tab.DocSemRelease();
                    }
                }
            }

            await SemWaitAsync();
            try
            {
                if (Tabs.Contains(tab))
                {
                    int tabIndex = Tabs.IndexOf(tab);
                    tab.RefreshState();
                    TabSetting.SaveSettings(tabIndex, tab.MetaData);
                }
            }
            catch (Exception) { }
            finally
            {
                SemRelease();
            }

            if (Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                if (AutoSaveState && _AutoSaveTimer != null)
                {
                    _AutoSaveTimer.Start();
                }
            }
            else
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        if (AutoSaveState && _AutoSaveTimer != null)
                        {
                            _AutoSaveTimer.Start();
                        }
                    });
            }

            _BlockWhileSaving = false;
        }

        #endregion Document Auto-Saving
    }
}
