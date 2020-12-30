using CompleteReader.Collections;
using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.Document.SubViews;
using CompleteReader.ViewModels.FileOpening;
using CompleteReader.ViewModels.Viewer;
using pdftron.PDF;
using pdftron.SDF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;

namespace CompleteReader.ViewModels.Document
{
    public class ActionToPerformOnActivating
    {
        public enum Actions
        {
            OpenDocument,
        }

        public Actions Action { get; set; }

        public StorageFile FileToOpen { get; set; }
    }


    abstract class DocumentViewModelBase : ViewModelBase, INavigable
    {
        const string PRIVACY_XODO = "http://www.xodo.com/legal";
        const string PRIVACY_COMPLETE_READER = "https://www.pdftron.com/pdfnet/mobile/windowsmobile_pdf_library.html";
        const string HELP_XODO = "http://feedback.xodo.com/knowledgebase/topics/50061-windows";
        const string HELP_COMPLETE_READER = "https://www.pdftron.com/documentation/uwp";
        const string FEEDBACK_XODO = "http://xodo.com/winrt/support.html";

        public static ActionToPerformOnActivating ActionToPerform { get; set; }

        protected DocumentViewModelBase()
        {
            InitCommands();
        }

        public event NewINavigableAvailableDelegate NewINavigableAvailable;

        public virtual void Activate(object parameter)
        {
            DocumentOpener = new DocumentOpener();
            DocumentOpener.ModalityChanged += SubView_ModalityChanged;
            DocumentOpener.DocumentReady += DocumentOpener_DocumentReady;
        }

        void DocumentOpener_DocumentReady(NewDocumentProperties selectedFileProperties)
        {
            CreateNewViewer(selectedFileProperties);
        }

        protected void SubView_ModalityChanged(bool isModal)
        {
            this.IsModal = isModal;
        }

        public virtual void Deactivate(object parameter)
        {
            IsModal = false;
        }

#region SubViewModels

        protected void DeactivateSubview()
        {

        }

        protected void recentVM_RecentFileSelected(RecentDocumentProperties recentFileProperties)
        {
            NewDocumentProperties props = new NewDocumentProperties();
            props.ApplyRecentProperties(recentFileProperties);

            DocumentOpener.Open(recentFileProperties.File as StorageFile, props);
        }

        protected void OpenedDocumentsVM_OpenedFileSelected(StorageFile file)
        {
            DocumentOpener.Open(file);
        }

        protected void FoldersVM_FolderDocumentSelected(StorageFile file)
        {
            DocumentOpener.Open(file);
        }

        protected void createVM_NewDocumentCreated(StorageFile file)
        {
            NewDocumentProperties docProperties = new NewDocumentProperties();
            docProperties.OpenedDocumentState = OpenedDocumentStates.Created;

            DocumentOpener.Open(file, docProperties);
        }

#endregion SubViewModels


#region Commands

        protected virtual void InitCommands()
        {
            BrowseFilesCommand = new RelayCommand(BrowseFilesCommandImpl);
            GettingStartedCommand = new RelayCommand(GettingStartedCommandImpl);
            HamburgerButtonCommand = new RelayCommand(HamburgerButtonCommandImpl);
            SettingsPrivacyCommand = new RelayCommand(SettingsPrivacyCommandImpl);
            HelpCommand = new RelayCommand(HelpCommandImpl);
#if XODO
            SettingsFeedbackCommand = new RelayCommand(SettingsFeedbackCommandImpl);
#endif
        }

        public RelayCommand BrowseFilesCommand { get; protected set; }
        public RelayCommand GettingStartedCommand { get; protected set; }
        public RelayCommand HamburgerButtonCommand { get; protected set; }
        public RelayCommand SettingsPrivacyCommand { get; protected set; }
        public RelayCommand HelpCommand { get; protected set; }
#if XODO
        public RelayCommand SettingsFeedbackCommand { get; protected set; }
#endif

        protected void BrowseFilesCommandImpl(object commandName)
        {
            Browse();
        }

        protected void GettingStartedCommandImpl(object commandName)
        {
            OpenGettingStarted();
        }

        protected void HamburgerButtonCommandImpl(object commandName)
        {
            IsSplitViewOpen = !IsSplitViewOpen;
        }

        protected async void SettingsPrivacyCommandImpl(object commandName)
        {
#if XODO
            Uri uri = new Uri(PRIVACY_XODO);
#else
            Uri uri = new Uri(PRIVACY_COMPLETE_READER);
#endif
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            if (!success)
            {
                // Take appropriate action if desirable
            }
        }

        protected async void HelpCommandImpl(object commandName)
        {
#if XODO
            Uri uri = new Uri(HELP_XODO);
#else
            Uri uri = new Uri(HELP_COMPLETE_READER);
#endif
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            if (!success)
            {
                // Take appropriate action if desirable
            }
        }

#if XODO
        protected async void SettingsFeedbackCommandImpl(object commandName)
        {
            Uri uri = new Uri(FEEDBACK_XODO);  
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            if (!success)
            {
                // Take appropriate action if desirable
            }
        }
#endif

#endregion Commands


#region Visible Properties

        /// <summary>
        /// All the subview panels in the document's page
        /// </summary>
        public enum SubViewState
        {
            None,
            RecentPage,
            FoldersPage,
            CreateDocument,
            ImageFromFile,
            ImageFromCamera,
            Settings,
            About,
            OpenedDocuments
        }

        /// <summary>
        /// All the different layers of the split view panels 
        /// </summary>
        public enum SplitViewPanelState
        {
            Main,
            CreateDocument,
            Settings,
        }
        protected Windows.UI.Xaml.Controls.SplitViewDisplayMode _SplitViewDisplayMode = Windows.UI.Xaml.Controls.SplitViewDisplayMode.CompactOverlay;
        public Windows.UI.Xaml.Controls.SplitViewDisplayMode SplitViewDisplayMode
        {
            get { return _SplitViewDisplayMode; }
            set { Set(ref _SplitViewDisplayMode, value); }
        }

        protected bool _IsSplitViewOpen = false;
        public bool IsSplitViewOpen
        {
            get { return _IsSplitViewOpen; }
            set { Set(ref _IsSplitViewOpen, value); }
        }

        protected bool _IsModal = false;
        // gets or sets whether the current dialog is modal or not.
        abstract public bool IsModal { get; set; }

        protected bool _IsInputDisabled = false;
        public bool IsInputDisabled
        {
            get { return _IsInputDisabled; }
            set { Set(ref _IsInputDisabled, value); }
        }

        protected bool _IsFileOpeningProgressBarVisible = false;
        public bool IsFileOpeningProgressBarVisible
        {
            get { return _IsFileOpeningProgressBarVisible; }
            set { Set(ref _IsFileOpeningProgressBarVisible, value); }
        }

        protected bool _IsPasswordDialogOpen = false;
        public bool IsPasswordDialogOpen
        {
            get { return _IsPasswordDialogOpen; }
            set 
            { 
                Set(ref _IsPasswordDialogOpen, value);
            }
        }

        protected bool _IsPackageDialogOpen = false;
        public bool IsPackageDialogOpen
        {
            get { return _IsPackageDialogOpen; }
            set
            {
                Set(ref _IsPackageDialogOpen, value);
                IsModal = value;
            }
        }

        protected DocumentOpener _DocumentOpener = null;
        public DocumentOpener DocumentOpener
        {
            get { return _DocumentOpener; }
            set { Set(ref _DocumentOpener, value); }
        }

        private bool _mainNavigationEnabled;
        public bool MainNavigationEnabled
        {
            get => _mainNavigationEnabled;
            set => Set(ref _mainNavigationEnabled, value);
        }

        private bool _createNavigationEnabled;
        public bool CreateNavigationEnabled
        {
            get => _createNavigationEnabled;
            set => Set(ref _createNavigationEnabled, value);
        }

        private bool _settingNavigationEnabled;
        public bool SettingNavigationEnabled
        {
            get => _settingNavigationEnabled;
            set => Set(ref _settingNavigationEnabled, value);
        }

        public bool IsDocumentOpenedVisible
        {
            get
            {
                if (Viewer.Helpers.CompleteReaderTabControlViewModel.Instance == null)
                    return false;

                if (Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.Tabs.Count > 0)
                {
                    // Note: In case only 1 item is in the tab and has no OriginalFile, it is considered no files opened
                    if (Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.Tabs.Count == 1
                        && Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.Tabs[0].OriginalFile == null)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                    return false;
            }
        }

        #endregion Visible Properties
        
        #region Public Interface
        /// <summary>
        /// Open the file
        /// </summary>
        /// <param name="file"></param>
        public void OpenFile(StorageFile file)
        {
            DocumentOpener.Open(file);
        }

#endregion Public Interface


#region Impl

        abstract protected void Browse();

#region Getting Started

        protected async void OpenGettingStarted()
        {
            DocumentOpener.CancelDocumentOpening();
            StorageFile file = null;
            try
            {
                StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Resources");
                file = await folder.GetFileAsync("GettingStarted.pdf");
            }
            catch (Exception) { }

            if (file != null)
            {
                StorageFile newFile = await Utilities.UtilityFunctions.GetTemporarySaveFileAsync("GettingStarted.pdf");
                if (newFile != null)
                {
                    await file.CopyAndReplaceAsync(newFile);
                    file = newFile;
                }
                NewDocumentProperties properties = new NewDocumentProperties();
                properties.OpenedDocumentState = OpenedDocumentStates.ReadOnly;

                DocumentOpener.Open(file, properties);
            }
            else
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                MessageDialog md = new MessageDialog(loader.GetString("DocumentsPage_FileOpeningError_UsedByAnother_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
            }
        }

#endregion Getting Started


#region Navigation


        protected void CreateNewViewer(NewDocumentProperties selectedFileProperties)
        {
            ViewerViewModel model = new ViewerViewModel(selectedFileProperties);
            if (NewINavigableAvailable != null)
            {
                NewINavigableAvailable(this, model);
            }
        }

        protected void RaiseNewNavigable(INavigable newNavigable)
        {
            if (NewINavigableAvailable != null)
            {
                NewINavigableAvailable(this, newNavigable);
            }

            // NOTE: Ensure to update Opened Documents visibility
            RaisePropertyChanged(nameof(IsDocumentOpenedVisible));
        }

        #endregion Navigation
        
        #endregion Impl
    }
}
