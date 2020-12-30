using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.Document.SubViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Input;

namespace CompleteReader.ViewModels.Document
{
    class DocumentViewModel : DocumentViewModelBase
    {
        private static DocumentViewModel _Current;

        /// <summary>
        /// The instance of the DocumentViewModel
        /// </summary>
        public static DocumentViewModel Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new DocumentViewModel();
                }
                return _Current;
            }
        }

        bool _CancelActivation = false;
        public override async void Activate(object parameter)
        {
            _CancelActivation = false;
            base.Activate(parameter);

            if (!CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.IsReady)
            {
                IsModal = true;
                await CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.GetInstanceAsync();
                IsModal = false;
            }

            if (!DocumentManager.IsReady)
            {
                IsModal = true;
                await DocumentManager.GetInstanceAsync();
                IsModal = false;
            }

            if (!CompleteReader.Collections.RecentItemsData.IsReady)
            {
                IsModal = true;
                await CompleteReader.Collections.RecentItemsData.GetItemSourceAsync();
                IsModal = false;
            }


            if (SubView == SubViewState.None)
            {
                SubViewState result;
                Enum.TryParse(Settings.Settings.MainPagePanel, out result);
                SubView = result;
            }
            else
            {
                ActivateSubView();
            }

            if (ActionToPerform != null && ActionToPerform.Action == ActionToPerformOnActivating.Actions.OpenDocument)
            {
                DocumentOpener.Open(ActionToPerform.FileToOpen);
            }


            ActionToPerform = null;

            _HotKeyHandler = CompleteReader.Utilities.HotKeyHandler.Current;
            if (_CancelActivation)
            {
                return;
            }
            _HotKeyHandler.KeyPressedEvent += HotKeyHandler_KeyPressedEvent;
            _HotKeyHandler.HotKeyPressedEvent += HotKeyHandler_HotKeyPressedEvent;
            _HotKeyHandler.AltHotKeyPressedEvent += HotKeyHandler_AltHotKeyPressedEvent;
        }

        public override void Deactivate(object parameter)
        {
            _CancelActivation = true;
            base.Deactivate(parameter);

            if (_HotKeyHandler != null)
            {
                _HotKeyHandler.KeyPressedEvent -= HotKeyHandler_KeyPressedEvent;
                _HotKeyHandler.HotKeyPressedEvent -= HotKeyHandler_HotKeyPressedEvent;
                _HotKeyHandler.AltHotKeyPressedEvent -= HotKeyHandler_AltHotKeyPressedEvent;
            }
        }

        #region SubViewModels

        protected INavigable _CurrentSubView = null;
        public INavigable CurrentSubView
        {
            get { return _CurrentSubView; }
            set
            {
                if (value != _CurrentSubView)
                {
                    if (_CurrentSubView != null)
                    {
                        _CurrentSubView.Deactivate(null);
                    }
                    _CurrentSubView = value;
                    if (_CurrentSubView != null)
                    {
                        _CurrentSubView.Activate(null);
                        RaiseNewNavigable(_CurrentSubView);
                    }
                }
            }
        }

        /// <summary>
        /// Activates SubView after user command
        /// </summary>
        protected void ActivateSubView()
        {
            if ((_SubViewState == SubViewState.RecentPage) || (_SubViewState == SubViewState.OpenedDocuments && IsDocumentOpenedVisible == false))
            {
                SubViews.RecentDocumentsViewModel recentVM = new SubViews.RecentDocumentsViewModel();
                recentVM.RecentFileSelected += recentVM_RecentFileSelected;

                CurrentSubView = recentVM;
            }
            else if (_SubViewState == SubViewState.OpenedDocuments)
            {
                SubViews.OpenedDocumentsViewModel openedVM = new SubViews.OpenedDocumentsViewModel();
                openedVM.OpenedFileSelected += OpenedDocumentsVM_OpenedFileSelected;

                CurrentSubView = openedVM;
            }
            else if (_SubViewState == SubViewState.FoldersPage)
            {
                SubViews.FolderDocumentsViewModel foldersVM = new SubViews.FolderDocumentsViewModel();
                foldersVM.FolderDocumentSelected += FoldersVM_FolderDocumentSelected;

                CurrentSubView = foldersVM;
            }
            else if (_SubViewState == SubViewState.CreateDocument ||
                _SubViewState == SubViewState.ImageFromCamera ||
                _SubViewState == SubViewState.ImageFromFile)
            {
                if (SubView != SubViewState.CreateDocument)
                {
                    SubView = SubViewState.CreateDocument;
                }
                else
                {
                    SubViews.DocumentCreationPageViewModel createVM = new SubViews.DocumentCreationPageViewModel();
                    createVM.NewDocumentCreated += createVM_NewDocumentCreated;
                    CurrentSubView = createVM;
                }
            }

            else if (_SubViewState == SubViewState.Settings)
            {
                CurrentSubView = Settings.SettingsViewModel.Current;
            }
        }
        
        #endregion SubViewModels


        #region Commands

        protected override void InitCommands()
        {
            base.InitCommands();

            SubViewSelectionCommand = new RelayCommand(SubViewSelectionCommandImpl);
            DocumentCreationNavigationCommand = new RelayCommand(DocumentCreationNavigationCommandImpl);
            SettingsNavigationCommand = new RelayCommand(SettingsNavigationCommandImpl);
        }

        private const int SPLIT_VIEW_WIDTH_THRESHOLD = 700;

        protected void SubViewSelectionCommandImpl(object commandName)
        {
            string str_cmdName = commandName as string;
            if (!string.IsNullOrWhiteSpace(str_cmdName))
            {
                bool isExpandedPanel = false;
                if (str_cmdName.Equals("RecentPage", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.RecentPage;
                }
                else if (str_cmdName.Equals("OpenedDocuments", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.OpenedDocuments;
                }
                else if (str_cmdName.Equals("FoldersPage", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.FoldersPage;
                }
                else if (str_cmdName.Equals("CreateNew", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.CreateDocument;
                    isExpandedPanel = true;
                }
                else if (str_cmdName.Equals("Browse", StringComparison.OrdinalIgnoreCase))
                {
                    Browse();
                }
                else if (str_cmdName.Equals("Settings", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.Settings;
                    isExpandedPanel = true;
                }

                if (Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Width > SPLIT_VIEW_WIDTH_THRESHOLD)
                {
                    if (isExpandedPanel)
                    {
                        SplitViewDisplayMode = Windows.UI.Xaml.Controls.SplitViewDisplayMode.CompactInline;
                        IsSplitViewOpen = true;
                    }
                    else
                    {
                        SplitViewDisplayMode = Windows.UI.Xaml.Controls.SplitViewDisplayMode.CompactOverlay;
                        IsSplitViewOpen = false;
                    }
                }
                else
                {
                    SplitViewDisplayMode = Windows.UI.Xaml.Controls.SplitViewDisplayMode.Overlay;
                    IsSplitViewOpen = false;
                }
            }
        }

        protected void DocumentCreationNavigationCommandImpl(object command)
        {
            string str_cmdName = command as string;
            if (!string.IsNullOrWhiteSpace(str_cmdName))
            {
                if (str_cmdName.Equals("BlankDoc", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.CreateDocument;
                }
                else if (str_cmdName.Equals("ImageFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.ImageFromFile;
                }
                else if (str_cmdName.Equals("ImageFromCamera", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.ImageFromCamera;
                }

                if (SplitViewDisplayMode != Windows.UI.Xaml.Controls.SplitViewDisplayMode.CompactInline)
                {
                    IsSplitViewOpen = false;
                }

                DocumentCreationPageViewModel.Current.NavigationCommand.Execute(str_cmdName);
            }
        }

        protected void SettingsNavigationCommandImpl(object command)
        {
            string str_cmdName = command as string;
            if (!string.IsNullOrWhiteSpace(str_cmdName))
            {
                if (str_cmdName.Equals("Options", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.Settings;
                }
                else if (str_cmdName.Equals("About", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViewState.About;
                }

                if (SplitViewDisplayMode != Windows.UI.Xaml.Controls.SplitViewDisplayMode.CompactInline)
                {
                    IsSplitViewOpen = false;
                }

                Settings.SettingsViewModel.Current.NavigationCommand.Execute(str_cmdName);
            }
        }

        public RelayCommand SubViewSelectionCommand { get; protected set; }
        public RelayCommand DocumentCreationNavigationCommand { get; protected set; }

        public RelayCommand SettingsNavigationCommand { get; protected set; }

        #endregion Commands


        #region Visual Properties

        override public bool IsModal
        {
            get { return _IsModal; }
            set
            {
                if (Set(ref _IsModal, value))
                {
                    if (CurrentSubView is RecentDocumentsViewModel)
                    {
                        (CurrentSubView as RecentDocumentsViewModel).IsModal = _IsModal;
                    }
                    else if (CurrentSubView is FolderDocumentsViewModel)
                    {
                        (CurrentSubView as FolderDocumentsViewModel).IsModal = _IsModal;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the current sub view
        /// </summary>
        public SplitViewPanelState SplitViewPanel
        {
            get
            {
                if (SubView == SubViewState.RecentPage || SubView == SubViewState.FoldersPage)
                {
                    Settings.Settings.MainPagePanel = SubView.ToString();
                    return SplitViewPanelState.Main;
                }
                else if (SubView == SubViewState.CreateDocument || SubView == SubViewState.ImageFromFile || SubView == SubViewState.ImageFromCamera)
                {
                    return SplitViewPanelState.CreateDocument;
                }
                else if (SubView == SubViewState.Settings || SubView == SubViewState.About)
                {
                    return SplitViewPanelState.Settings;
                }

                return SplitViewPanelState.Main;
            }
        }

        protected SubViewState _SubViewState = SubViewState.None;
        /// <summary>
        /// Gets or sets the current sub view
        /// </summary>
        public SubViewState SubView
        {
            get
            {
                return _SubViewState;
            }
            set
            {
                if (Set(ref _SubViewState, value))
                {
                    RaisePropertyChanged("SplitViewPanel");
                    DocumentOpener.CancelDocumentOpening();
                    ActivateSubView();
                }
            }
        }

        #endregion Visual Properties


        #region Browse Files

        protected bool _FilePickerOpen = false;
        protected override async void Browse()
        {
            AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.FILEBROWSER, "File Opened from Browser");

            if (_FilePickerOpen)
            {
                return;
            }
            DocumentOpener.CancelDocumentOpening();
            IsInputDisabled = true;
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.ViewMode = PickerViewMode.List;
            StorageFile file = null;
            foreach (string fileType in Settings.Settings.AssociatedFileTypes)
            {
                fileOpenPicker.FileTypeFilter.Add(fileType);
            }
            try
            {
                _FilePickerOpen = true;
                // NOTE: apparently, this sometimes throws a System.Exception "Element not found" for no apparent reason. We want to catch that.
                file = await fileOpenPicker.PickSingleFileAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                _FilePickerOpen = false;
                IsInputDisabled = false;
            }

            if (file != null)
            {
                DocumentOpener.Open(file);
            }
        }

        #endregion Browse Files
        
        #region HotKeys

        protected CompleteReader.Utilities.HotKeyHandler _HotKeyHandler;

        /// <summary>
        /// The users pressed a button without any modifier keys down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void HotKeyHandler_KeyPressedEvent(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                //RequestAction("CloseDialogs");
            }
        }

        /// <summary>
        /// Ctrl + something was pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void HotKeyHandler_HotKeyPressedEvent(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {

        }

        /// <summary>
        /// ctrl + alt + something was pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void HotKeyHandler_AltHotKeyPressedEvent(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {

        }

        #endregion HotKeys

    }
}
