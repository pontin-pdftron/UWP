using CompleteReader.Collections;
using CompleteReader.Utilities;
using CompleteReader.Viewer.Dialogs;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.FileOpening;
using CompleteReader.ViewModels.Viewer.Helpers;
using pdftron.PDF;
using pdftron.PDF.Tools;
using pdftron.PDF.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using pdftron.PDF.Tools.Controls.ViewModels;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using pdftron.Common;
using CompleteReader.Viewer;

namespace CompleteReader.ViewModels.Viewer
{
    public class ViewerViewModel : ViewModelBase, INavigable
    {
        private const int MAX_TABS_WINDOWS = 20;
        private const int MAX_TABS_PHONE = 6;
        private const int PAGES_NEEDED_FOR_THUMB_SLIDER = 5;
        private int MAX_TABS = MAX_TABS_WINDOWS; // TODO - do a hardware check here?

        private static ViewerViewModel _Current;
        public ViewerViewModel(NewDocumentProperties properties = null)
        {
            _Current = this;

            InitToolControls();
            InitCommands();
            InitAppBarManagement();

            _PropertiesToActivate = properties;
        }

        public event NewINavigableAvailableDelegate NewINavigableAvailable;
        public event FindTextViewModel.FindTextResultFoundDelegate FindTextResultFound;

        private NewDocumentProperties _PropertiesToActivate = null;
        public async void Activate(object parameter)
        {
            App.Current.ActiveViewer = this;

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

            if (CompleteReaderTabControlViewModel.IsReady)
            {
                IsModal = true;
                await CompleteReaderTabControlViewModel.Instance.UpdateFilePropertiesAsync();
                IsModal = false;
            }

            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone
                && Settings.Settings.PhoneFullScreen == 1)
            {
                await Utilities.UtilityFunctions.SetFullScreenModeAsync(true);
            }

            //WhatsNew = Settings.WhatsNewManager.Current;
            if (_PropertiesToActivate != null)
            {
                InitPDFView(_PropertiesToActivate);
            }
            _UseTimedAppBar = true;

            _HotKeyHandler = CompleteReader.Utilities.HotKeyHandler.Current;
            _HotKeyHandler.KeyPressedEvent += HotKeyHandler_KeyPressedEvent;
            _HotKeyHandler.HotKeyPressedEvent += HotKeyHandler_HotKeyPressedEvent;
            _HotKeyHandler.AltHotKeyPressedEvent += HotKeyHandler_AltHotKeyPressedEvent;

            SystemNavigationManager.GetForCurrentView().BackRequested += BackButtonHandler_BackPressed;

            _SharingHelper = Utilities.SharingHelper.GetSharingHelper();
            _SharingHelper.RetrieveSharingString += SharingHelper_RetrieveSharingString;
            _SharingHelper.RetrieveSharingStorageFile += SharingHelper_RetrieveSharingStorageFile;
            _SharingHelper.DocumentSaver = SaveHelperAsync;

            if (Settings.Settings.ScreenSleepLock)
            {
                _DisplayRequest = new Windows.System.Display.DisplayRequest();
                _DisplayRequest.RequestActive();
            }
        }

        public async void Deactivate(object parameter)
        {
            App.Current.ActiveViewer = null;

            //WhatsNew = null;

            ThumbnailViewer = null;
            _HotKeyHandler.KeyPressedEvent -= HotKeyHandler_KeyPressedEvent;
            _HotKeyHandler.HotKeyPressedEvent -= HotKeyHandler_HotKeyPressedEvent;
            _HotKeyHandler.AltHotKeyPressedEvent -= HotKeyHandler_AltHotKeyPressedEvent;

            PDFPrintManager.UnRegisterForPrintingContract();

            SystemNavigationManager.GetForCurrentView().BackRequested -= BackButtonHandler_BackPressed;

            _SharingHelper.RetrieveSharingString -= SharingHelper_RetrieveSharingString;
            _SharingHelper.RetrieveSharingStorageFile -= SharingHelper_RetrieveSharingStorageFile;
            _SharingHelper.DocumentSaver = null;

            if (_VisibleTabChangedHandler != null)
            {
                TabControlViewModel.VisibleTabChanged -= _VisibleTabChangedHandler;
                _VisibleTabChangedHandler = null;
            }
            if (_TabFixedButtonClickedHandler != null)
            {
                TabControlViewModel.FixedButtonClicked -= _TabFixedButtonClickedHandler;
                _TabFixedButtonClickedHandler = null;
            }
            if (_TabCloseButtonClickedHandler != null)
            {
                TabControlViewModel.CloseButtonClicked -= _TabCloseButtonClickedHandler;
                _TabCloseButtonClickedHandler = null;
            }
            foreach (CompleteReaderPDFViewCtrlTabInfo tab in TabControlViewModel.Tabs)
            {
                tab?.NavigationStack?.Unsubscribe();
            }
            if (TabControlViewModel.SelectedTab != null)
            {
                await DeactivateTabAsync(TabControlViewModel.SelectedTab);
            }
            if (Settings.Settings.ScreenSleepLock)
            {
                _DisplayRequest.RequestRelease();
            }

            try
            {
                if (IsFullScreen)
                {
                    await Utilities.UtilityFunctions.SetFullScreenModeAsync(false);
                }
            }
            catch (Exception) { }

            if (FindTextViewModel != null)
            {
                await FindTextViewModel.WaitForTextSearchToCancel();
            }
            if (OutlineDialogViewModel != null)
            {
                await OutlineDialogViewModel.CleanUpSubViewsAsync();
                OutlineDialogViewModel = null;
            }
            TabControlViewModel.Deactivate();
            ViewerPageSettingsViewModel = null;

            foreach (CompleteReaderPDFViewCtrlTabInfo tab in _TabRightTapHandlers.Keys)
            {
                tab.ReflowTapped -= _TabRightTapHandlers[tab];
            }
            _TabRightTapHandlers.Clear();

            if ((ToolManager != null) && (ToolManager.UndoRedoAction != null))
            {
                ToolManager.UndoRedoAction.OnUndoRedoStatusChanged -= UndoRedoAction_OnUndoRedoStatusChanged;
            }
        }

        private void UndoRedoAction_OnUndoRedoStatusChanged(object sender, UndoRedoStatusEventArgs e)
        {
            if (PageNumberIndicator != null)
            {
                PageNumberIndicator.UpdatePageNumbers();
            }

            // Notify document has changed and allow/enable Save button
            HandleDocumentEditing();

            // Make sure to update modified page thumbnail
            if (IsOutlineDialogOpen)
                OutlineDialogViewModel.Thumbnails.PageModified(e.ModifiedPage);
        }

        private void InitPDFView(NewDocumentProperties properties)
        {
            if (!CompleteReaderTabControlViewModel.IsReady)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.VIEWER, "TabControlViewModel was not ready");
                return;
            }

            TabControlViewModel = GetTabControlViewModel();
            TabControlViewModel.MaximumItems = MAX_TABS;

            if (TabControlViewModel.CheckIfFileIsOpenAndActivate(properties.TemporaryFile) || 
                (properties.OriginalFileFromSystem != null && TabControlViewModel.CheckIfOriginalIsOpenAndActivate(properties.OriginalFileFromSystem)))
            {
                CompleteReaderPDFViewCtrlTabInfo tabInfo = TabControlViewModel.SelectedTab;
                if (tabInfo.MetaData.IsOpenedThroughDrop
                    && (properties == null || !properties.OpenedThroughDrop)
                    && properties.OpenedDocumentState == OpenedDocumentStates.Normal
                    && properties.File != null)
                {
                    tabInfo.OriginalFile = properties.File;
                    tabInfo.DocumentState = OpenedDocumentStates.Normal;
                    tabInfo.MetaData.IsOpenedThroughDrop = false;
                    UpdateTabDependantProperties();
                }
            }
            else
            {
                if (TabControlViewModel.OpenTabs >= MAX_TABS)
                {
                    _FileToActivate = properties.File;
                    _FileToActivateDocumentProperties = properties;
                    _SuppressRecentListUpdate = true;

                    CloseOldestTab();

                    return;
                }

                if (!Settings.Settings.RememberLastPage)
                {
                    properties.StartPage = 1;
                }
                CreateNewTab(properties);
            }
        }

        private void CloseOldestTab()
        {
            CompleteReaderPDFViewCtrlTabInfo tabToclose = TabControlViewModel.OldestViewedTab;
            SaveNeededStates status = CheckSavingRequirements(tabToclose);
            bool conflict = !tabToclose.IsUpToDate && tabToclose.IsDocumentModifiedSinceLastSave;
            if (status == SaveNeededStates.saveNeeded)
            {
                TabControlViewModel.SelectTab(tabToclose);
            }
            if (!conflict)
            {
                CloseTabRequested(status, tabToclose, true);
            }
        }

        private void CreateNewTab(NewDocumentProperties properties)
        {
            CompleteReaderPDFViewCtrlTabInfo tab = new CompleteReaderPDFViewCtrlTabInfo(properties.File, properties.TemporaryFile, properties.Doc, properties.File.DisplayName);
            tab.FileSourceIfNotSaveable = properties.OriginalFileFromSystem;
            tab.StreamForconversion = properties.FileStream;
            TabControlViewModel.AddTab(tab);
            tab.DocumentState = properties.OpenedDocumentState;
            tab.MetaData.IsOpenedThroughDrop = properties.OpenedThroughDrop;
            tab.HasDocumentBeenModifiedSinceOpening = false;
            tab.HasUserBeenWarnedAboutSaving = false;
            tab.IsDocumentModifiedSinceLastSave = false;
            if (!string.IsNullOrEmpty(properties.Password))
            {
                tab.MetaData.Password = properties.Password;
            }
            TabControlViewModel.SelectTab(tab);
            if (Settings.Settings.RememberLastPage)
            {
                tab.MetaData.PageRotation = properties.PageRotation;
                switch (tab.MetaData.PageRotation)
                {
                    case PageRotate.e_90:
                        tab.PDFViewCtrl.RotateClockwise();
                        break;
                    case PageRotate.e_180:
                        tab.PDFViewCtrl.RotateClockwise();
                        tab.PDFViewCtrl.RotateClockwise();
                        break;
                    case PageRotate.e_270:
                        tab.PDFViewCtrl.RotateCounterClockwise();
                        break;
                }
                tab.MetaData.PagePresentationMode = properties.PresentationMode;
                tab.PDFViewCtrl.SetPagePresentationMode(tab.MetaData.PagePresentationMode);
                tab.IsReflow = properties.IsReflow;

                tab.MetaData.LastPage = properties.StartPage;
                tab.MetaData.Zoom = properties.Zoom;
                tab.MetaData.HScrollPos = properties.HorizontalScrollPosition;
                tab.MetaData.VScrollPos = properties.VerticalScrollPosition;
            }
            tab.MetaData.LastPage = properties.StartPage;
            tab.MetaData.LastModifiedDate = properties.LastModifiedDate;
            tab.MetaData.FileSize = properties.Filesize;
            tab.SystemLastModifiedDate = properties.LastModifiedDate;
            tab.SystemFileSize = properties.Filesize;
            UpdateTabDependantProperties();
            ResolveReflow();

            if ((ToolManager != null) && (ToolManager.UndoRedoAction != null))
            {
                ToolManager.UndoRedoAction.OnUndoRedoStatusChanged += UndoRedoAction_OnUndoRedoStatusChanged;
            }
        }

        private void InitToolControls()
        {
            _PageNumberIndicator = new ClickablePageNumberIndicator();
            _PageNumberIndicator.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
            PageNumberIndicator.ShowingBehaviour = ClickablePageNumberIndicator.ShowingBehaviours.AlwaysShow;
            PageNumberIndicator.GotFocus += (a, b) => { _AppBarClosingTimer?.Stop(); };
            PageNumberIndicator.Show(); 

            AnnotationToolbar = new AnnotationCommandBar();
            AnnotationToolbar.ControlClosed += AnnotationToolbar_ControlClosed;
            AnnotationToolbar.ButtonsStayDown = Settings.Settings.ButtonsStayDown;
        }



        private CompleteReaderTabControlViewModel.VisibleTabChangedDelegate _VisibleTabChangedHandler;
        private CompleteReaderTabControlViewModel.FixedButtonClickedDelegate _TabFixedButtonClickedHandler;
        private CompleteReaderTabControlViewModel.CloseButtonClickedDelegate _TabCloseButtonClickedHandler;
        private CompleteReaderTabControlViewModel GetTabControlViewModel()
        {
            if (this.TabControlViewModel == null)
            {
                CompleteReaderTabControlViewModel viewModel = CompleteReaderTabControlViewModel.Instance;
                viewModel.DefaultImageURL = "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png";
                viewModel.ShowImagePreviews = true;
                viewModel.AutoSaveState = true;
                _VisibleTabChangedHandler = new CompleteReaderTabControlViewModel.VisibleTabChangedDelegate(TabControlViewModel_VisibleTabChanged);
                viewModel.VisibleTabChanged += _VisibleTabChangedHandler;
                _TabFixedButtonClickedHandler = new CompleteReaderTabControlViewModel.FixedButtonClickedDelegate(TabControlViewModel_FixedButtonClicked);
                viewModel.FixedButtonClicked += _TabFixedButtonClickedHandler;
                _TabCloseButtonClickedHandler = new CompleteReaderTabControlViewModel.CloseButtonClickedDelegate(TabControlViewModel_CloseButtonClicked);
                viewModel.CloseButtonClicked += _TabCloseButtonClickedHandler;

                viewModel.HasFixedItemAtEnd = true;
                viewModel.MaximumItems = MAX_TABS;

                this.TabControlViewModel = CompleteReaderTabControlViewModel.Instance;
            }
            this.TabControlViewModel.ShowIfDocumentModified = CompleteReaderPDFViewCtrlTabInfo.ShowIfModifedStates.DoNotShow;
            return CompleteReaderTabControlViewModel.Instance;
        }

        void OutlineDialogViewModel_RequestClosing(object sender, EventArgs e)
        {
            IsOutlineDialogOpen = false;
        }

        private void OutlineDialogViewModel_RequestSwitchSide(object sender, EventArgs e)
        {
            if (Settings.SharedSettings.OutlineDialogAnchorSide == OutlineDialog.AnchorSides.Left)
            {
                Settings.SharedSettings.OutlineDialogAnchorSide = OutlineDialog.AnchorSides.Right;
            }
            else if (Settings.SharedSettings.OutlineDialogAnchorSide == OutlineDialog.AnchorSides.Right)
            {
                Settings.SharedSettings.OutlineDialogAnchorSide = OutlineDialog.AnchorSides.Left;
            }
        }

        private void OutlineDialogViewModel_DocumentModified(PDFDoc doc)
        {
            HandleDocumentEditing();
        }

        private void OutlineDialogViewModel_UserBookmarksEdited(PDFDoc doc)
        {
            HandleDocumentEditing();
        }

        private void NavigationStack_NavigationStackChanged()
        {
            RaisePropertyChanged("IsUndoEnabled");
            RaisePropertyChanged("IsRedoEnabled");
        }

        void PDFViewCtrl_OnPageNumberChanged(int current_page, int num_pages)
        {
            SavePageNumberInRecentList();
        }

        void PDFViewCtrl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((bool)e.NewValue) == true)
            {
                IsModal = false;
            }
            else
            {
                IsModal = true;
            }
        }

        private void AnnotationToolbar_ControlClosed()
        {
            IsAnnotationToolbarOpen = false;
            AppbarInteraction();
            CloseCurrentTool();
            UpdateUIVisiblity();
        }

        #region Member Variables

        private Utilities.SharingHelper _SharingHelper;

        private NewToolCreatedDelegate _ToolManager_NewToolCreated;
        private SingleTapDelegate _ToolManager_SingleTap;
        private pdftron.PDF.PDFPrintManager _PDFPrintManager;
        private PDFPrintManager PDFPrintManager
        {
            get
            {
                if (_PDFPrintManager == null)
                {

                    // printing
                    _PDFPrintManager = PDFPrintManager.GetInstance();
                    _PDFPrintManager.UnRegisterForPrintingContract();

                    Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("Printing");
                    //new Windows.ApplicationModel.Resources.ResourceLoader();
                    _PDFPrintManager.SetResourceLoader(loader);

                    // standard options
                    _PDFPrintManager.AddStandardPrintOption(Windows.Graphics.Printing.StandardPrintTaskOptions.MediaSize);
                    _PDFPrintManager.AddStandardPrintOption(Windows.Graphics.Printing.StandardPrintTaskOptions.Orientation);
                    _PDFPrintManager.AddStandardPrintOption(Windows.Graphics.Printing.StandardPrintTaskOptions.Copies);

                    // PDFTron options
                    _PDFPrintManager.AddUserOptionAnnotations();
                    _PDFPrintManager.AddUserOptionAutoRotate();
                    _PDFPrintManager.AddUserOptionPageRange();
                }
                return _PDFPrintManager;
            }
        }

        // dealing with text to speech event
        pdftron.PDF.Tools.ToolManager.TextToSpeechDelegate _TextToSpeechListener = null;

        // dealing with modifying documents
        pdftron.PDF.Tools.ToolManager.AnnotationModificationHandler _AnnotAddedListener = null;
        pdftron.PDF.Tools.ToolManager.AnnotationModificationHandler _AnnotEditedListener = null;
        pdftron.PDF.Tools.ToolManager.AnnotationModificationHandler _AnnotRemovedListener = null;

        pdftron.PDF.Tools.ToolManager.AnnotationGroupModificationHandler _AnnotationGroupAddedListener = null;
        pdftron.PDF.Tools.ToolManager.AnnotationGroupModificationHandler _AnnotationGroupEditedListener = null;
        pdftron.PDF.Tools.ToolManager.AnnotationGroupModificationHandler _AnnotationGroupRemovedListener = null;

        private bool _HasPage1ChangeSinceThumbnailViewOpened = false;
        private bool _EditedSinceThumbnailViewerOpened = false;
        private pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageMovedDelegate _ThumbnailViewerPageMovedDelegate = null;
        private pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageDeletedDelegate _ThumbnailViewerPageDeletedDelegate = null;
        private pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageAddedDelegate _ThumbnailViewerPageAddedDelegate = null;
        private pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageRotatedDelegate _ThumbnailViewerPageRotatedDelegate = null;

        private bool _SuppressRecentListUpdate = false;
        private bool _RemovingTab = false;

        private Windows.System.Display.DisplayRequest _DisplayRequest;

        CompleteReader.Collections.RecentItemsData _RecentItems;

        private List<CompleteReaderPDFViewCtrlTabInfo> _ActiveTabs = new List<CompleteReaderPDFViewCtrlTabInfo>();

        #endregion MemberVariables


        #region Non-Visual Properties

        private bool _SaveDocumentAfterClosing = false;
        public bool SaveDocumentAfterClosing
        {
            get { return _SaveDocumentAfterClosing; }
            set { _SaveDocumentAfterClosing = value; }
        }

        public bool HasDocumentBeenModifiedSinceOpening
        {
            get { return TabControlViewModel.SelectedTab.HasDocumentBeenModifiedSinceOpening; }
            set { TabControlViewModel.SelectedTab.HasDocumentBeenModifiedSinceOpening = value; }
        }

        public ToolManager ToolManager
        {
            get
            {
                if (TabControlViewModel == null || TabControlViewModel.SelectedTab == null)
                {
                    return null;
                }
                return TabControlViewModel.SelectedTab.ToolManager;
            }
        }

        private OutlineDialogViewModel _OutlineDialogViewModel;
        public OutlineDialogViewModel OutlineDialogViewModel
        {
            get { return _OutlineDialogViewModel; }
            set
            {
                OutlineDialogViewModel oldVM = _OutlineDialogViewModel;
                if (Set(ref _OutlineDialogViewModel, value))
                {
                    if (oldVM != null)
                    {
                        oldVM.RequestClosing -= OutlineDialogViewModel_RequestClosing;
                        oldVM.RequestSwitchSide -= OutlineDialogViewModel_RequestSwitchSide;
                        oldVM.DocumentModified -= OutlineDialogViewModel_DocumentModified;
                        oldVM.UserBookmarksEdited -= OutlineDialogViewModel_UserBookmarksEdited;
                    }

                    if (OutlineDialogViewModel != null)
                    {
                        OutlineDialogViewModel.CloseDialogOnItemClick = _OutlineDialogNavigationCloses;
                        OutlineDialogViewModel.RequestClosing += OutlineDialogViewModel_RequestClosing;
                        OutlineDialogViewModel.RequestSwitchSide += OutlineDialogViewModel_RequestSwitchSide;
                        OutlineDialogViewModel.DocumentModified += OutlineDialogViewModel_DocumentModified;
                        OutlineDialogViewModel.UserBookmarksEdited += OutlineDialogViewModel_UserBookmarksEdited;
                    }
                }
            }
        }

        private FindTextViewModel _FindTextviewModel = null;
        public FindTextViewModel FindTextViewModel
        {
            get { return _FindTextviewModel;}
            set
            {
                Set(ref _FindTextviewModel, value);
            }
        }

        private bool _TabControlLoaded = false;
        private CompleteReaderTabControlViewModel _TabControlViewModel;
        public CompleteReaderTabControlViewModel TabControlViewModel
        {
            get { return _TabControlViewModel; }
            private set
            {
                Set(ref _TabControlViewModel, value);
            }
        } 

        #endregion Non-Visual Properties


        #region Visual Properties

        public PDFViewCtrl PDFViewCtrl
        {
            get
            {
                if (TabControlViewModel == null || TabControlViewModel.SelectedTab == null)
                {
                    return null;
                }
                return TabControlViewModel.SelectedTab.PDFViewCtrl;
            }
        }

        public ReflowView ReflowView
        {
            get
            {
                if (TabControlViewModel == null || TabControlViewModel.SelectedTab == null)
                {
                    return null;
                }
                return TabControlViewModel.SelectedTab.ReflowView;
            }
        }

        private AnnotationCommandBar _AnnotationToolbar;
        public AnnotationCommandBar AnnotationToolbar
        {
            get { return _AnnotationToolbar; }
            set
            {
                Set(ref _AnnotationToolbar, value);
            }
        }

        private ThumbnailSlider _ThumbnailSlider;
        public ThumbnailSlider ThumbnailSlider
        {
            get { return _ThumbnailSlider; }
            set
            {
                Set(ref _ThumbnailSlider, value);
            }
        }

        private MediaElement _MediaElement = null;
        public MediaElement MediaElement
        {
            get { return _MediaElement; }
            set { Set(ref _MediaElement, value); }
        }

        private bool _IsPDFViewCtrlVisible = false;
        public bool IsPDFViewCtrlVisible
        {
            get { return _IsPDFViewCtrlVisible; }
            set { Set(ref _IsPDFViewCtrlVisible, value); }
        }

        private bool _IsModal = false;
        // gets or sets whether the current dialog is modal or not.
        public bool IsModal
        {
            get { return _IsModal; }
            set
            {
                if (_IsModal != value)
                {
                    _IsModal = value;
                    AnnotationToolbar.IsEnabled = !_IsModal;
                    if (TabControlViewModel != null)
                    {
                        TabControlViewModel.IsEnabled = !value;
                    }
                    if (_IsModal)
                    {
                        IsAppBarOpen = false;
                        if (!Settings.Settings.PinCommandBar && !IsAnnotationToolbarOpen)
                        {
                            IsEntireAppBarOpen = false;
                        }
                    }
                    IsAppBarAvailable = !_IsModal;
                    RaisePropertyChanged("IsModal");
                }
            }
        }

        private bool _IsSecretlyModal = false;
        /// <summary>
        /// This is added make the viewer modal without appearing modal. This can be used for async operations that 
        /// are generally fast, but sometimes slow and require modality. Also remember to make the PDFViewCtrl not 
        /// hit test visible.
        /// </summary>
        public bool IsSecretlyModal
        {
            get { return _IsSecretlyModal; }
            set { Set(ref _IsSecretlyModal, value); }
        }

        private bool _IsAppBarAvailable = true;
        public bool IsAppBarAvailable
        {
            get { return _IsAppBarAvailable; }
            set
            {
                Set(ref _IsAppBarAvailable, value);
            }
        }


        private bool _IsModalGrayout = true;
        /// <summary>
        /// When true, the modal dialog grays out the Viewer
        /// </summary>
        public bool IsModalGrayout
        {
            get { return _IsModalGrayout; }
            set { Set(ref _IsModalGrayout, value); }
        }

        private bool _IsDismissableDialogOpen = false;
        // Gets or sets whether or not the current dialog is dismissable
        public bool IsDismissableDialogOpen
        {
            get { return _IsDismissableDialogOpen; }
            set
            {
                if (Set(ref _IsDismissableDialogOpen, value))
                {
                    if (!_IsDismissableDialogOpen)
                    {
                        CloseDismissableDialogs();
                    }
                }
            }
        }

        public bool IsFullScreen
        {
            get { return Utilities.UtilityFunctions.IsFullScreen(); }
        }

        public bool IsReflow
        {
            get
            {
                if (TabControlViewModel?.SelectedTab == null)
                    return false;
                return TabControlViewModel.SelectedTab.IsReflow;
            }
        }

        private bool _IsEntireAppBarOpen = Settings.Settings.PinCommandBar;
        /// <summary>
        /// Controls whether the App Bar is visible on the screen or not
        /// </summary>
        public bool IsEntireAppBarOpen
        {
            get { return _IsEntireAppBarOpen; }
            set
            {
                if (!value && IsFindTextDialogOpen)
                {
                    return;
                }
                if (Set(ref _IsEntireAppBarOpen, value))
                {
                    CloseCurrentTool(true);
                    if (value)
                    { 
                        AppbarInteraction();
                    }

                    UpdateUIVisiblity();
                }
            }
        }

        private bool _IsAppBarOpen = false;
        /// <summary>
        /// Controls whether the App Bar is in a expanded or compact state
        /// </summary>
        public bool IsAppBarOpen
        {
            get { return _IsAppBarOpen; }
            set
            {
                if (Set(ref _IsAppBarOpen, value))
                {
                    System.Diagnostics.Debug.WriteLine("IsAppBarOpen set to " + _IsAppBarOpen);
                    CloseCurrentTool(true);
                    if (_IsAppBarOpen)
                    {
                        CloseDialogsOnAppBarOpen();
                        AppbarInteraction();
                    }
                    else
                    {
                        _SuppressRecentListUpdate = false;
                    }
                    UpdateUIVisiblity();
                }
            }
        }

        private bool _IsQuickSettingsOpen = false;

        public bool IsQuickSettingsOpen
        {
            get { return _IsQuickSettingsOpen; }
            set
            {
                if (Set(ref _IsQuickSettingsOpen, value))
                {
                    CloseCurrentTool();
                    if (_IsQuickSettingsOpen && ViewerPageSettingsViewModel != null)
                    {
                        ViewerPageSettingsViewModel.UpdateSettings();
                    }
                }
            }
        }

        private bool _IsDocumentEditable = true;
        public bool IsDocumentEditable
        {
            get
            {
                return _IsDocumentEditable && !IsReflow;
            }
            set { Set(ref _IsDocumentEditable, value); }
        }

        public bool IsDocumentSearchable
        {
            get
            {
                return !IsReflow && !IsPreparingForConversion;// && TabControlViewModel != null && TabControlViewModel.SelectedTab != null && TabControlViewModel.SelectedTab.DocumentState != OpenedDocumentStates.Universal;
            }
        }

        public bool IsPreparingForConversion
        {
            get { return TabControlViewModel != null && TabControlViewModel.SelectedTab != null && TabControlViewModel.SelectedTab.IsWaitingForConversionToStart; }
        }

        private bool _IsSaveButtonVisible = false;
        public bool IsSaveButtonVisible
        {
            get { return _IsSaveButtonVisible; }
            set { Set(ref _IsSaveButtonVisible, value); }
        }


        public bool IsUndoEnabled
        {
            get
            {
                if (TabControlViewModel != null
                    && TabControlViewModel.SelectedTab != null
                    && TabControlViewModel.SelectedTab.NavigationStack != null)
                {
                    return TabControlViewModel.SelectedTab.NavigationStack.IsUndoEnabled;
                }
                return false;
            }
        }

        public bool IsRedoEnabled
        {
            get
            {
                if (TabControlViewModel != null
                    && TabControlViewModel.SelectedTab != null
                    && TabControlViewModel.SelectedTab.NavigationStack != null)
                {
                    return TabControlViewModel.SelectedTab.NavigationStack.IsRedoEnabled;
                }
                return false;
            }
        }

        private bool _IsUiPinned = Settings.Settings.PinCommandBar;
        public bool IsUiPinned
        {
            get { return _IsUiPinned; }
            set
            {
                if (Set(ref _IsUiPinned, value))
                {
                    Settings.Settings.PinCommandBar = value;
                    UpdateUIVisiblity();
                }
            }
        }

        public bool IsDocumentModifiedSinceLastSave
        {
            get
            {
                if (TabControlViewModel != null && TabControlViewModel.SelectedTab != null)
                {
                    return TabControlViewModel.SelectedTab.IsDocumentModifiedSinceLastSave;
                }
                return false;
            }
            set
            {
                if (value != TabControlViewModel.SelectedTab.IsDocumentModifiedSinceLastSave)
                {
                    TabControlViewModel.SelectedTab.IsDocumentModifiedSinceLastSave = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("IsSaveButtonEnabled");
                }
            }
        }

        public bool IsSaveButtonEnabled
        {
            get
            {
                return IsDocumentModifiedSinceLastSave && (CurrentDocumentState == OpenedDocumentStates.Normal || CurrentDocumentState == OpenedDocumentStates.NeedsFullSave);
            }
        }

        private bool _IsSaveAsButtonVisible = false;
        public bool IsSaveAsButtonVisible
        {
            get { return _IsSaveAsButtonVisible; }
            set { Set(ref _IsSaveAsButtonVisible, value); }

        }

        private bool _IsSaveTimerOpen = false;
        public bool IsSaveTimerOpen
        {
            get { return _IsSaveTimerOpen; }
            set
            {
                if (Set(ref _IsSaveTimerOpen, value))
                {
                    IsModal = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsAnnotationToolbarOpen = false;
        public bool IsAnnotationToolbarOpen
        {
            get { return _IsAnnotationToolbarOpen; }
            set
            {
                if (Set(ref _IsAnnotationToolbarOpen, value))
                {
                    IsAppBarOpen = false;
                }
            }
        }

        private bool _OutlineDialogNavigationCloses = false;
        /// <summary>
        /// Used to Set whether navigating using the outline dialog should close it.
        /// </summary>
        public bool OutlineDialogNavigationCloses
        {
            get { return _OutlineDialogNavigationCloses; }
            set
            {
                if (Set(ref _OutlineDialogNavigationCloses, value))
                {
                    if (OutlineDialogViewModel != null)
                    {
                        OutlineDialogViewModel.CloseDialogOnItemClick = _OutlineDialogNavigationCloses;
                    }
                }
            }
        }

        private bool _OutlineDialogHibernation = false;
        private bool _IsOutlineDialogOpen = false;
        public bool IsOutlineDialogOpen
        {
            get { return _IsOutlineDialogOpen; }
            set
            {
                if (Set(ref _IsOutlineDialogOpen, value))
                {
                    Settings.Settings.OutlineDialogOpen = value;
                    CompleteReaderPDFViewCtrlTabInfo tab = TabControlViewModel.SelectedTab;
                    if (tab != null)
                    {
                        if (_IsOutlineDialogOpen)
                        {
                            OutlineDialogViewModel = tab.OutlineDialogViewModel;
                        }
                        else
                        {
                            tab.CloseOutline();
                        }
                    }
                    if (!_IsOutlineDialogOpen)
                    {
                        OutlineDialogViewModel = null;
                        if (PDFViewCtrl != null)
                        {
                            OutlineDialogViewModel = null;
                            PDFViewCtrl.Focus(FocusState.Programmatic);
                        }
                    }
                }
            }
        }

        private bool _IsFindTextDialogOpen = false;
        public bool IsFindTextDialogOpen
        {
            get { return _IsFindTextDialogOpen; }
            set
            {
                if (Set(ref _IsFindTextDialogOpen, value))
                {
                    CloseCurrentTool();
                    if (_IsFindTextDialogOpen)
                    {
                        IsAppBarOpen = false;
                        _AppBarClosingTimer.Stop();
                        IsEntireAppBarOpen = true;
                        FindTextViewModel = new FindTextViewModel(PDFViewCtrl);
                        FindTextViewModel.FindTextClosed += FindTextViewModel_FindTextClosed;
                        FindTextViewModel.FindTextResultFound += FindTextViewModel_FindTextResultFound;
                    }
                    else if (FindTextViewModel != null)
                    {
                        FindTextViewModel.ClearViewModel();
                        AppbarInteraction();
                    }
                }
                if (FindTextViewModel == null && PDFViewCtrl != null)
                {
                    FindTextViewModel = new FindTextViewModel(PDFViewCtrl);
                }
                if (FindTextViewModel != null)
                {
                    FindTextViewModel.IsInSearchMode = value;
                }
            }
        }

        public enum SaveAsOption
        {
            Save,
            Flatten,
            Optimize,
            Password,
            Crop,
        }

        private SaveAsOption _CurrentSaveAsOption;

        public SaveAsOption CurrentSaveAsOption
        {
            get { return _CurrentSaveAsOption; }
            set { Set(ref _CurrentSaveAsOption, value); }
        }

        private string _SaveAPasswordCopyText;

        public string SaveAPasswordCopyText
        {
            get { return _SaveAPasswordCopyText; }
            set { Set(ref _SaveAPasswordCopyText, value); }
        }

        void FindTextViewModel_FindTextClosed()
        {
            IsFindTextDialogOpen = false;
        }

        private void FindTextViewModel_FindTextResultFound()
        {
            FindTextResultFound?.Invoke();
        }

        private pdftron.PDF.Tools.Controls.ClickablePageNumberIndicator _PageNumberIndicator;
        public pdftron.PDF.Tools.Controls.ClickablePageNumberIndicator PageNumberIndicator
        {
            get { return _PageNumberIndicator; }
        }

        private bool _DoToolsAllowZoom = true;
        private bool DoToolsAllowZoom
        {
            set
            {
                bool before = AreZoombuttonsVisible;
                _DoToolsAllowZoom = value;
                if (before != AreZoombuttonsVisible)
                {
                    RaisePropertyChanged("AreZoombuttonsVisible");
                }
            }
        }
        private bool _IsInputMouse = false;
        private bool IsInputMouse
        {
            get
            {
                return _IsInputMouse;
            }
            set
            {
                bool before = AreZoombuttonsVisible;
                _IsInputMouse = value;
                if (before != AreZoombuttonsVisible)
                {
                    RaisePropertyChanged("AreZoombuttonsVisible");
                }
            }
        }

        private bool _AreAudioButtonsVisible = false;
        public bool AreAudioButtonsVisible
        {
            get
            {
                return _AreAudioButtonsVisible;
            }
            set
            {
                Set(ref _AreAudioButtonsVisible, value);
            }
        }

        private bool _IsAudioPlaying = false;
        public bool IsAudioPlaying
        {
            get { return _IsAudioPlaying; }
            set { Set(ref _IsAudioPlaying, value); }
        }


        void PDFViewCtrl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                IsInputMouse = true;
            }
        }

        void PDFViewCtrl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                IsInputMouse = false;
            }
        }

        void ToolManager_NewToolCreated(ToolType toolType)
        {
            if (toolType == ToolType.e_ink_create || toolType == ToolType.e_text_annot_create)
            {
                DoToolsAllowZoom = false;
            }
            else
            {
                DoToolsAllowZoom = true;
            }

            if (toolType == ToolType.e_ink_create)
            {
                //IsAppBarAvailable = false;        // Don't hide app bar with annotation command bar
            }
            else
            {
                //IsAppBarAvailable = !IsModal;
            }
        }

        private void ToolManager_SingleTap(SingleTapEvents eventType, TappedRoutedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Tapped: " + eventType.ToString());
            if (eventType == SingleTapEvents.BeforeTool && IsEntireAppBarOpen && !IsAnnotationToolbarOpen && !Settings.SharedSettings.PinCommandBar)
            {
                if (!IsAppBarOpen)
                {
                    IsEntireAppBarOpen = false;
                }
                IsAppBarOpen = false;
                args.Handled = true;
            }
            else if (eventType == SingleTapEvents.BeforeTool && IsAnnotationToolbarOpen)
            {
                if (AnnotationToolbar.IsAppBarOpen)
                {
                    AnnotationToolbar.IsAppBarOpen = false;
                    args.Handled = true;
                }
            }
            else if (eventType == SingleTapEvents.SingleTapConfirmed && !Settings.SharedSettings.PinCommandBar && !IsEntireAppBarOpen)
            {
                IsAppBarOpen = false;
                IsEntireAppBarOpen = true;
                args.Handled = true;
            }
        }

        public bool AreZoombuttonsVisible
        {
            get
            {
                if (!_IsInputMouse || !_DoToolsAllowZoom)
                    return false;

                if (!Settings.Settings.PinCommandBar)
                {
                    if (IsAnnotationToolbarOpen)
                    {
                        return AnnotationToolbar.IsAppBarOpen;
                    }
                    else
                    {
                        return IsEntireAppBarOpen;
                    }
                }

                return true;
            }
        }

        public bool IsExtraUIVisible
        {
            get
            {
                if (Settings.Settings.PinCommandBar)
                {
                    return true;
                }
                else
                {
                    if (!IsAnnotationToolbarOpen)
                    {
                        return IsEntireAppBarOpen;
                    }
                    else
                    {
                        return _AnnotationToolbar.IsAppBarOpen;
                    }
                }
            }
        }

        public bool IsThumbnailSliderVisible
        {
            get
            {
                if (ThumbnailSlider == null)
                {
                    return false;
                }
                if (IsUiPinned)
                {
                    return IsAppBarOpen || AnnotationToolbar.IsAppBarOpen;
                }
                else if (IsAnnotationToolbarOpen)
                {
                    return AnnotationToolbar.IsAppBarOpen;
                }

                return IsEntireAppBarOpen;
            }
        }

        public bool IsConverting
        {
            get
            {
                return TabControlViewModel != null && TabControlViewModel.SelectedTab != null &&
                    TabControlViewModel.SelectedTab.PDFViewCtrl != null && TabControlViewModel.SelectedTab.PDFViewCtrl.IsConverting;
            }
        }
        
        private void ResolveThumbnailSlider()
        {
            if (ThumbnailSlider == null && !PDFViewCtrl.IsConverting && PDFViewCtrl.GetPageCount() >= PAGES_NEEDED_FOR_THUMB_SLIDER)
            {
                AddThumbnailSlider(TabControlViewModel.SelectedTab);
            }
            else if (ThumbnailSlider != null && PDFViewCtrl.GetPageCount() < PAGES_NEEDED_FOR_THUMB_SLIDER)
            {
                ThumbnailSlider.PDFViewCtrl = null;
                ThumbnailSlider = null;
            }
            if (ThumbnailSlider != null)
            {
                ThumbnailSlider.UpdateMaximum();
            }
        }

        public void UpdateUIVisiblity()
        {
            RaisePropertyChanged("IsExtraUIVisible");
            RaisePropertyChanged("IsThumbnailSliderVisible");
            RaisePropertyChanged("AreZoombuttonsVisible");
            RaisePropertyChanged("AreAudioButtonsVisible");
        }

        #endregion Visual Properties


        #region Document Properties

        private StorageFile CurrentFile
        {
            get { return TabControlViewModel.SelectedTab.OriginalFile; }
            set { TabControlViewModel.SelectedTab.OriginalFile = value; }
        }
        private StorageFile TemporaryFile
        {
            get { return TabControlViewModel.SelectedTab.PDFFile; }
            set { TabControlViewModel.SelectedTab.PDFFile = value; }
        }
        private IRandomAccessStream _CurrentStream = null;
        private string CurrentPassword
        {
            get { return TabControlViewModel.SelectedTab.MetaData.Password; }
            set { TabControlViewModel.SelectedTab.MetaData.Password = value; }
        }
        private string MRUToken
        {
            get { return TabControlViewModel.SelectedTab.MetaData.MostRecentlyUsedToken; }
            set { TabControlViewModel.SelectedTab.MetaData.MostRecentlyUsedToken = value; }
        }

        /// <summary>
        /// Used for the Phone, where we need to close the app when the user hits the back button in case they opened the app through file association.       
        /// </summary>
        private bool _CloseAppAfterSaving = false;

        public enum OpenedDocumentFileTypes
        {
            pdf,
            xps,
        }

        public static Dictionary<OpenedDocumentFileTypes, string> FileTypeNameDictionary =
            new Dictionary<OpenedDocumentFileTypes, string>() { { OpenedDocumentFileTypes.pdf, "pdf" },
            { OpenedDocumentFileTypes.xps, "xps" } };

        public OpenedDocumentStates CurrentDocumentState
        {
            get
            {
                if (TabControlViewModel.SelectedTab == null)
                {
                    return OpenedDocumentStates.None;
                }
                return TabControlViewModel.SelectedTab.DocumentState;
            }
            set
            {
                if (TabControlViewModel.SelectedTab != null)
                {
                    TabControlViewModel.SelectedTab.DocumentState = value;
                }
            }
        }

        public bool CanSaveToSamefile
        {
            get { return CurrentDocumentState == OpenedDocumentStates.Normal || CurrentDocumentState == OpenedDocumentStates.Created; }
        }

        private OpenedDocumentFileTypes _CurrentDocumentFileType = OpenedDocumentFileTypes.pdf;
        public OpenedDocumentFileTypes CurrentDocumentFileType
        {
            get { return _CurrentDocumentFileType; }
            set { _CurrentDocumentFileType = value; }
        }

        #endregion Document Properties


        #region Commands

        private void InitCommands()
        {
            GoToFilesCommand = new RelayCommand(GoToFilesCommandImpl);
            PinCommand = new RelayCommand(PinCommandImpl);
            UnpinCommand = new RelayCommand(UnpinCommandImpl);
            SaveCommand = new RelayCommand(SaveCommandImpl);
            SaveAsCommand = new RelayCommand(SaveAsCommandImpl);

            SearchCommand = new RelayCommand(SearchCommandImpl);
            OutlineCommand = new RelayCommand(OutlineCommandImpl);
            EditCommand = new RelayCommand(EditCommandImpl);
            SaveAndFlattenCommand = new RelayCommand(SaveAndFlattenCommandImpl);
            SaveReducedFileSizeCopyCommand = new RelayCommand(SaveReducedFileSizeCopyCommandImpl);
            SavePasswordCopyCommand = new RelayCommand(SavePasswordCopyCommandImpl);
            SavePermCroppedCopyCommand = new RelayCommand(SavePermCroppedCopyCommandImpl);

            PageEditingCommand = new RelayCommand(PageEditingCommandImpl);

            PrintCommand = new RelayCommand(PrintCommandImpl);
            ShareCommand = new RelayCommand(ShareCommandImpl);
            ZoomButtonCommand = new RelayCommand(ZoomButtonCommandImpl);

            PlayAudioCommand = new RelayCommand(PlayAudioCommandImpl);
            PauseAudioCommand = new RelayCommand(PauseAudioCommandImpl);
            StopAudioCommand = new RelayCommand(StopAudioCommandImpl);

            UndoCommand = new RelayCommand(UndoCommandImpl);
            RedoCommand = new RelayCommand(RedoCommandImpl);

            CloseSearchCommand = new RelayCommand(CloseSearchCommandImpl);
            FullScreenExitCommand = new RelayCommand(FullScreenExitCommandImpl);

            DismissDialogCommand = new RelayCommand(DismissDialogCommandImpl);
            QuickSettingsOpenedCommand = new RelayCommand(QuickSettingsOpenedCommandImpl);
            QuickSettingsClosedCommand = new RelayCommand(QuickSettingsClosedCommandImpl);
        }

        // App Bar
        public RelayCommand GoToFilesCommand { get; private set; }
        public RelayCommand PinCommand { get; private set; }
        public RelayCommand UnpinCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand SaveAsCommand { get; private set; }
        public RelayCommand SearchCommand { get; private set; }
        public RelayCommand OutlineCommand { get; private set; }
        public RelayCommand EditCommand { get; private set; }
        public RelayCommand SaveAndFlattenCommand { get; private set; }
        public RelayCommand SaveReducedFileSizeCopyCommand { get; private set; }
        public RelayCommand SavePasswordCopyCommand { get; private set; }
        public RelayCommand SavePermCroppedCopyCommand { get; private set; }
        public RelayCommand PageEditingCommand { get; private set; }

        public RelayCommand PrintCommand { get; private set; }
        public RelayCommand ShareCommand { get; private set; }

        public RelayCommand ZoomButtonCommand { get; private set; }
        public RelayCommand PlayAudioCommand { get; private set; }
        public RelayCommand PauseAudioCommand { get; private set; }
        public RelayCommand StopAudioCommand { get; private set; }

        public RelayCommand UndoCommand { get; private set; }
        public RelayCommand RedoCommand { get; private set; }

        public RelayCommand CloseSearchCommand { get; private set; }
        public RelayCommand FullScreenExitCommand { get; private set; }


        // End App Bar

        public RelayCommand DismissDialogCommand { get; private set; }
        public RelayCommand QuickSettingsOpenedCommand { get; private set; }
        public RelayCommand QuickSettingsClosedCommand { get; private set; }

        private async void GoToFilesCommandImpl(object CommandName)
        {
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            CloseCurrentTool();
            SaveNeededStates status = CheckSavingRequirements(TabControlViewModel.SelectedTab);
            if (status == SaveNeededStates.saveApproved)
            {
                SaveResult result = await SaveToOriginalAsync(TabControlViewModel.SelectedTab, true, true);
                if (result != SaveResult.e_normal)
                {
                    try
                    {
                        IsSecretlyModal = true;
                        if (TabControlViewModel.SelectedTab.PDFViewCtrl != null)
                        {
                            TabControlViewModel.SelectedTab.PDFViewCtrl.IsHitTestVisible = false;
                        }
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        string message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_Close_Message_AccessDenied"),
                            TabControlViewModel.SelectedTab.Title, loader.GetString("Generic_Cancel_Text"));
                        if (result == SaveResult.e_unknown_error)
                        {
                            message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_Close_Message_Unknown"), TabControlViewModel.SelectedTab.Title);
                        }
                        MessageDialog md = new MessageDialog(message, loader.GetString("ViewerPage_AutoSaveFailed_Title"));
                        md.Commands.Add(new UICommand(loader.GetString("ViewerPage_AutoSaveFailed_Close_Discard"), (command) =>
                        {
                            NavigateAway();
                        }));
                        md.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Generic_Cancel_Text"), (command) =>
                        {

                        }));
                        await MessageDialogHelper.ShowMessageDialogAsync(md);
                    }
                    finally
                    {
                        IsSecretlyModal = false;
                        if (TabControlViewModel.SelectedTab?.PDFViewCtrl != null)
                        {
                            TabControlViewModel.SelectedTab.PDFViewCtrl.IsHitTestVisible = true;
                        }
                    }
                }
            }
            else
            {
                await SavePageNumberInRecentListAsync(TabControlViewModel.SelectedTab);
                NavigateAway();   
            }
        }

        private void PinCommandImpl(object CommandName)
        {
            IsUiPinned = true;
        }

        private void UnpinCommandImpl(object CommandName)
        {
            IsUiPinned = false;
            IsEntireAppBarOpen = false;
        }

        private async void SaveCommandImpl(object CommandName)
        {
            CloseCurrentTool();
            AppbarInteraction();
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            Save();
            IsAppBarOpen = false;
            await Task.Delay(200); // let the app bar close
            if (_SavingInProgress)
            {
                PDFViewCtrl.IsEnabled = false;
                CurrentSaveAsOption = SaveAsOption.Save;
                IsSaveTimerOpen = true;
            }
        }

        private void SaveAsCommandImpl(object CommandName)
        {
            CloseCurrentTool();
            AppbarInteraction();
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            SaveAs(TabControlViewModel.SelectedTab, false, false, SaveAsOption.Save);
            IsAppBarOpen = false;
        }

        private async void SaveAndFlattenCommandImpl(object CommandName)
        {
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            IsModal = true;
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog messageDialog = new MessageDialog(loader.GetString("ViewerPage_SecondaryOptions_SaveAndFlatten_Message"), loader.GetString("ViewerPage_SecondaryOptions_SaveAndFlatten_Message_Title"));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_SecondaryOptions_SaveAndFlatten_Message_Confirm"), (command) =>
            {
                SaveAs(TabControlViewModel.SelectedTab, false, false, SaveAsOption.Flatten);
                IsAppBarOpen = false;
            }));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Generic_Cancel_Text"), (command) =>
            {
                IsModal = false;
            }));
            await messageDialog.ShowAsync();
        }

        private void SaveReducedFileSizeCopyCommandImpl(object CommandName)
        {
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            IsModal = true;
            OptimizeFileViewModel.IsPopupOpen = true;
        }

        private void SavePasswordCopyCommandImpl(object CommandName)
        {
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            IsModal = true;
            PasswordFileViewModel.IsPopupOpen = true;
        }

        private async void SavePermCroppedCopyCommandImpl(object CommandName)
        {
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            IsModal = true;
            await SavePermCroppedCopyAsync();
        }

        private void PageEditingCommandImpl(object CommandName)
        {
            if (PDFViewCtrl != null)
            {
                IsAppBarOpen = false;
                OpenThumbviewerInEditMode(PDFViewCtrl.GetCurrentPage());
            }
        }

        private void SearchCommandImpl(object CommandName)
        {
            CloseCurrentTool();
            AppbarInteraction();
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            IsFindTextDialogOpen = true;
        }


        private void OutlineCommandImpl(object CommandName)
        {
            CloseCurrentTool();
            AppbarInteraction();
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            IsOutlineDialogOpen = !IsOutlineDialogOpen;
            IsAppBarOpen = false;
            if (!Settings.Settings.PinCommandBar)
            {
                IsEntireAppBarOpen = false;
            }
        }

        private void EditCommandImpl(object CommandName)
        {
            AppbarInteraction();
            if (IsConverting)
            {
                return;
            }
            CloseCurrentTool();
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            _AppBarClosingTimer.Stop();
            IsAnnotationToolbarOpen = true;
            IsAppBarOpen = false;
            UpdateUIVisiblity();
        }

        private async void TryPrint()
        {
            PDFDoc doc = PDFViewCtrl.GetDoc();
            if (doc != null && TabControlViewModel.SelectedTab != null)
            {
                PDFPrintManager.RegisterForPrintingContract(doc, TabControlViewModel.SelectedTab.Title);
                try
                {
                    await Windows.Graphics.Printing.PrintManager.ShowPrintUIAsync();
                }
                catch (Exception)
                {

                }
            }
        }

        private void PrintCommandImpl(object CommandName)
        {
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            IsAppBarOpen = false;
            TryPrint();
        }

        private void ShareCommandImpl(object CommandName)
        {
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        private void ZoomButtonCommandImpl(object action)
        {
            CloseCurrentTool();
            AppbarInteraction();
            TabControlViewModel?.SelectedTab?.ToolManager?.CloseOpenDialog();
            string act = action as string;
            double currentZoom = PDFViewCtrl.GetZoom();
            if (PDFViewCtrl.IsAnimatingZoom())
            {
                currentZoom = PDFViewCtrl.GetZoomAnimationTarget();
            }
            if (act.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                PDFViewCtrl.SetZoom((int)(PDFViewCtrl.ActualWidth / 2), (int)(PDFViewCtrl.ActualHeight / 2), currentZoom * 1.25, true);
            }
            else if (act.Equals("out", StringComparison.OrdinalIgnoreCase))
            {
                PDFViewCtrl.SetZoom((int)(PDFViewCtrl.ActualWidth / 2), (int)(PDFViewCtrl.ActualHeight / 2), currentZoom / 1.25, true);
            }
        }

        private void PlayAudioCommandImpl(object action)
        {
            IsAudioPlaying = true;
            MediaElement.Play();
        }

        private void PauseAudioCommandImpl(object action)
        {
            IsAudioPlaying = false;
            MediaElement.Pause();
        }

        private void StopAudioCommandImpl(object action)
        {
            AreAudioButtonsVisible = false;
            MediaElement.Stop();
        }

        private void UndoCommandImpl(object param)
        {
            CloseCurrentTool();
            AppbarInteraction();
            TabControlViewModel.SelectedTab.NavigationStack.Undo();

        }

        private void RedoCommandImpl(object param)
        {
            CloseCurrentTool();
            AppbarInteraction();
            TabControlViewModel.SelectedTab.NavigationStack.Redo();
        }

        private void CloseSearchCommandImpl(object parameter)
        {
            IsFindTextDialogOpen = false;
        }

        private void FullScreenExitCommandImpl(object parameter)
        {
            IsEntireAppBarOpen = true;
        }

        private void DismissDialogCommandImpl(object commandName)
        {
            IsDismissableDialogOpen = false;
        }

        private void QuickSettingsOpenedCommandImpl(object sender)
        {
            CloseCurrentTool();
            AppbarInteraction();
            IsQuickSettingsOpen = true;
            ViewerPageSettingsViewModel.UpdateSettings();
            _AppBarClosingTimer.Stop();
        }

        private void QuickSettingsClosedCommandImpl(object sender)
        {
            CloseCurrentTool();
            IsQuickSettingsOpen = false;
            AppbarInteraction();
        }

        #endregion Commands


        #region Saving and document Modification

        private Utilities.AutoSaveHelper _AutoSaveHelper;
        private bool _FilePickerOpen = false;

        private bool HasUserBeenWarnedAboutSaving
        {
            get { return TabControlViewModel.SelectedTab.HasUserBeenWarnedAboutSaving; }
            set { TabControlViewModel.SelectedTab.HasUserBeenWarnedAboutSaving = value; }
        }

        public void ResolveAutoSaverAndSaveButton()
        {
            if (TabControlViewModel.SelectedTab == null || TabControlViewModel.SelectedTab.Doc == null)
            {
                TurnOffAutoSave();
                IsSaveButtonVisible = false;
            }
            bool autoSaveOn = Settings.Settings.AutoSaveOn;
            if (autoSaveOn)
            {
                TabControlViewModel.ShowIfDocumentModified = CompleteReaderPDFViewCtrlTabInfo.ShowIfModifedStates.ShowIfModidiedAndCantSave;
            }
            else
            {
                TabControlViewModel.ShowIfDocumentModified = CompleteReaderPDFViewCtrlTabInfo.ShowIfModifedStates.ShowIfModified;
            }
            if (TabControlViewModel.SelectedTab != null)
            {
                TabControlViewModel.SelectedTab.ShowIfDocumentModified = TabControlViewModel.ShowIfDocumentModified;
            }
            IsSaveAsButtonVisible = CurrentDocumentState != OpenedDocumentStates.Uneditable;
            if (autoSaveOn)
            {
                IsSaveButtonVisible = false;
                if (CurrentDocumentState == OpenedDocumentStates.Normal)
                {
                    TurnOnAutoSaving();
                }
                else
                {
                    TurnOffAutoSave();
                }
            }
            else
            {
                if (CurrentDocumentState == OpenedDocumentStates.Normal
                    || CurrentDocumentState == OpenedDocumentStates.NeedsFullSave)
                {
                    IsSaveButtonVisible = true;
                }
                else
                {
                    IsSaveButtonVisible = false;
                }
                TurnOffAutoSave();
            }
            RaisePropertyChanged("IsSaveButtonEnabled");
        }

        private void TurnOnAutoSaving()
        {
            if (_AutoSaveHelper == null)
            {
                _AutoSaveHelper = new Utilities.AutoSaveHelper(30);
                _AutoSaveHelper.DocumentWasSaved += _AutoSaveHelper_DocumentWasSaved;
                _AutoSaveHelper.CommitChangesAsync += AutoSaveHelper_CommitChangesAsync;
            }
            _AutoSaveHelper.Stop();
            _AutoSaveHelper.CurrentFile = CurrentFile;
            _AutoSaveHelper.TemporaryFile = TemporaryFile;
            _AutoSaveHelper.PDFDoc = TabControlViewModel.SelectedTab.Doc;
            _AutoSaveHelper.Start();
        }

        private Task AutoSaveHelper_CommitChangesAsync()
        {
            //if (OutlineDialogViewModel != null && OutlineDialogViewModel.HasUnsavedUserbookmarks)
            //{
            //    OutlineDialogViewModel.UserBookmarks.SaveBookmarks();
            //    return OutlineDialogViewModel.UserBookmarks.WaitForBookmarkSavingAsync();
            //}
            return null;
        }

        void _AutoSaveHelper_DocumentWasSaved(DateTimeOffset newModifiedDate, ulong newFileSize)
        {
            IsDocumentModifiedSinceLastSave = false;
            if (TabControlViewModel.SelectedTab != null)
            {
                TabControlViewModel.SelectedTab.MetaData.LastModifiedDate = newModifiedDate;
                TabControlViewModel.SelectedTab.MetaData.FileSize = newFileSize;
                TabControlViewModel.SelectedTab.SystemLastModifiedDate = newModifiedDate;
                TabControlViewModel.SelectedTab.SystemFileSize = newFileSize;
            }
        }

        private void TurnOffAutoSave()
        {
            if (_AutoSaveHelper != null)
            {
                _AutoSaveHelper.Stop();
                _AutoSaveHelper = null;
            }
        }

        public void PauseAutoSaving()
        {
            if (_AutoSaveHelper != null)
            {
                _AutoSaveHelper.Stop();
            }
        }

        public void ResumeAutoSaving()
        {
            if (_AutoSaveHelper != null)
            {
                _AutoSaveHelper.Start();
            }
        }

        void ToolManager_AnnotationModified(IAnnot annotation, int pageNumber)
        {
            HandleDocumentEditing();
            if (pageNumber == 1)
            {
                TabControlViewModel.SelectedTab.MetaData.IsThumbnailUpToDate = false;
            }
            if (IsOutlineDialogOpen)
            {
                OutlineDialogViewModel.Thumbnails.PageModified(pageNumber);
            }
        }

        void ToolManager_AnnotationGroupModified(Dictionary<IAnnot, int> annotationGroup)
        {
            HandleDocumentEditing();
            if (annotationGroup.ContainsValue(1))
            {
                TabControlViewModel.SelectedTab.MetaData.IsThumbnailUpToDate = false;
            }
            if (IsOutlineDialogOpen)
            {
                foreach (var item in annotationGroup)
                {
                    OutlineDialogViewModel.Thumbnails.PageModified(item.Value);
                }
            }
        }

        void ToolManager_TextToSpeechActivated(MediaElement media)
        {
            AreAudioButtonsVisible = true;
            IsAudioPlaying = true;
            MediaElement = media;
        }

        private void ThumbnailViewerPageMovedHandler(int pageNumber, int newLocation)
        {
            _NavigationStateBeforeThumbnailsviewOpening = null;
            TabControlViewModel.SelectedTab.NavigationStack.Clear();
            _EditedSinceThumbnailViewerOpened = true;
            if (pageNumber == 1 || newLocation == 1)
            {
                _HasPage1ChangeSinceThumbnailViewOpened = true;
            }
        }

        private void ThumbnailViewerPageDeletedHandler(int pageNumber)
        {
            _NavigationStateBeforeThumbnailsviewOpening = null;
            TabControlViewModel.SelectedTab.NavigationStack.Clear();
            _EditedSinceThumbnailViewerOpened = true;
            if (pageNumber == 1)
                _HasPage1ChangeSinceThumbnailViewOpened = true;
            if (PageNumberIndicator != null)
                PageNumberIndicator.UpdatePageNumbers();
        }
        private void ThumbnailViewerPageAddedHandler(int pageNumber, ThumbnailItem item)
        {
            _NavigationStateBeforeThumbnailsviewOpening = null;
            TabControlViewModel.SelectedTab.NavigationStack.Clear();
            _EditedSinceThumbnailViewerOpened = true;
            if (pageNumber == 1)
            {
                _HasPage1ChangeSinceThumbnailViewOpened = true;
            }
            // Need to call this since adding page doesnt cause core to fire PageNumberChanged
            if (PageNumberIndicator != null)
            {
                PageNumberIndicator.UpdatePageNumbers();
            }

        }

        private void ThumbnailViewerPageRotatedDelegate(int pageNumber)
        {
            _NavigationStateBeforeThumbnailsviewOpening = null;
            TabControlViewModel.SelectedTab.NavigationStack.Clear();
            _EditedSinceThumbnailViewerOpened = true;
            if (pageNumber == 1)
            {
                _HasPage1ChangeSinceThumbnailViewOpened = true;
            }
        }

        /// <summary>
        /// Make sure any doc modification has been notified
        /// </summary>
        private void HandleDocumentEditing()
        {
            HasDocumentBeenModifiedSinceOpening = true;
            if (!HasUserBeenWarnedAboutSaving)
            {
                HasUserBeenWarnedAboutSaving = true;
                if (CurrentDocumentState == OpenedDocumentStates.Created || CurrentDocumentState == OpenedDocumentStates.NonePDF)
                {
                    NotifyCreatedDocument();
                }
                else if (CurrentDocumentState == OpenedDocumentStates.ReadOnly)
                {
                    NotifyReadOnlyDocument();
                }
                else if (CurrentDocumentState == OpenedDocumentStates.Corrupted || CurrentDocumentState == OpenedDocumentStates.CorruptedAndModified)
                {
                    NotyfyReparablePDFDoc();
                }
            }
            UpdateTabDependantProperties();

            if (_EditedSinceThumbnailViewerOpened)
            {
                _EditedSinceThumbnailViewerOpened = false;
                PDFViewCtrl.UpdatePageLayout();
                IsDocumentModifiedSinceLastSave = true;
                ResolveThumbnailSlider();
                UpdateUIVisiblity();
            }
            if (_HasPage1ChangeSinceThumbnailViewOpened)
            {
                _HasPage1ChangeSinceThumbnailViewOpened = false;
                TabControlViewModel.SelectedTab.UpdatePreview();
            }

            if (CurrentDocumentState == OpenedDocumentStates.Corrupted)
            {
                CurrentDocumentState = OpenedDocumentStates.CorruptedAndModified;
            }
            IsDocumentModifiedSinceLastSave = true;
        }

        private bool _SavingInProgress = false;
        public async void Save()
        {
            try
            {
                _SavingInProgress = true;
                await SaveHelperAsync(null, true, true);
                ResolveAutoSaverAndSaveButton();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            finally
            {
                _SavingInProgress = false;
                if (PDFViewCtrl != null)
                {
                    PDFViewCtrl.IsEnabled = true;
                }
                IsSaveTimerOpen = false;
            }
        }

        public async System.Threading.Tasks.Task SaveHelperAsync()
        {
            await SaveHelperAsync(null);
        }

        /// <summary>
        /// It starts the saving process on the selected document/tab 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="includeFullSave"></param>
        /// <param name="alwaysSave">Means that we want to save to the original file, not just to the temporary one</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task SaveHelperAsync(CompleteReaderPDFViewCtrlTabInfo tab, bool includeFullSave = false, bool alwaysSave = false)
        {
            if (tab == null)
            {
                tab = TabControlViewModel.SelectedTab;
            }
            if (tab == null)
            {
                return;
            }

            if (OutlineDialogViewModel != null && OutlineDialogViewModel.HasUnsavedUserbookmarks)
            {
                OutlineDialogViewModel.SaveBookmarks();
                await OutlineDialogViewModel.UserBookmarks.WaitForBookmarkSavingAsync();
            }
            SaveResult result = await SaveToOriginalAsync(tab, includeFullSave, alwaysSave).ConfigureAwait(true);
            if (result != SaveResult.e_normal)
            {
                IsSaveTimerOpen = false;
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                string messageString = string.Format(loader.GetString("ViewerPage_TabDialog_SaveError_AccessDenied"), tab.Title);
                if (result == SaveResult.e_unknown_error)
                {
                    messageString = string.Format(loader.GetString("ViewerPage_TabDialog_SaveError"), tab.Title);
                }
                MessageDialog md = new MessageDialog(messageString, loader.GetString("ViewerPage_TabDialog_SaveError_Title"));
                await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);

            }
        }

        private enum SaveResult
        {
            e_normal = 0,
            e_access_denied,
            e_unknown_error,
        }

        private async Task<SaveResult> SaveToOriginalAsync(CompleteReaderPDFViewCtrlTabInfo tab, bool includeFullSave = false, bool alwaysSave = false)
        {
            if (tab == null)
            {
                tab = TabControlViewModel.SelectedTab;
            }
            if (tab == null)
            {
                return SaveResult.e_normal;
            }

            SaveResult result = SaveResult.e_normal;
            try
            {
                if (tab.DocumentState != OpenedDocumentStates.Corrupted && tab.DocumentState != OpenedDocumentStates.CorruptedAndModified)
                {
                    result = SaveResult.e_unknown_error;

                    if (tab.Doc != null)
                    {
                        await tab.SaveDocumentAsync();
                    }

                    if ((tab.DocumentState == OpenedDocumentStates.Normal && (Settings.Settings.AutoSaveOn || alwaysSave))
                        || (tab.DocumentState == OpenedDocumentStates.NeedsFullSave && includeFullSave))
                    {
                        DocumentManager manager = await DocumentManager.GetInstanceAsync();
                        await manager.AddChangesToOriginal(tab.OriginalFile, tab.PDFFile);
                        Tuple<DateTimeOffset, ulong> lastModifiedAndSize = await Utilities.UtilityFunctions.GetDateModifiedAndSizeAsync(tab.OriginalFile);
                        tab.IsDocumentModifiedSinceLastSave = false;
                        tab.MetaData.LastModifiedDate = lastModifiedAndSize.Item1;
                        tab.MetaData.FileSize = lastModifiedAndSize.Item2;
                        tab.SystemLastModifiedDate = lastModifiedAndSize.Item1;
                        tab.SystemFileSize = lastModifiedAndSize.Item2;
                        TabControlViewModel.SaveTabState();
                    }
                    result = SaveResult.e_normal;
                }
            }
            catch (UnauthorizedAccessException)
            {
                result = SaveResult.e_access_denied;
            }
            catch (Exception)
            {
                result = SaveResult.e_unknown_error;
            }
            return result;
        }

        private async Task SaveTempFileOnlyAsync(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            await tab.SaveDocumentAsync();
        }

        private delegate void SaveAsDoneDelegate(bool cancelled, bool failed);
        private event SaveAsDoneDelegate SaveAsDone;


        public async void SaveAs(CompleteReaderPDFViewCtrlTabInfo tab, bool closeIfsuccessful, bool navigateIfSucessful, SaveAsOption option)
        {
            if (tab == null || _FilePickerOpen)
            {
                return;
            }

            StorageFile file = null;

            PDFDoc doc = tab.Doc;
            Windows.Storage.Pickers.FileSavePicker fileSavePicker = new Windows.Storage.Pickers.FileSavePicker();
            fileSavePicker.CommitButtonText = "Save";
            fileSavePicker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
            string suggestedFileName = string.Empty;
            if (tab.OriginalFile != null)
            {
                suggestedFileName = tab.OriginalFile.DisplayName;
            }
            else
            {
                suggestedFileName = tab.PDFFile.DisplayName;
            }
            if (tab.DocumentState == OpenedDocumentStates.Normal || tab.DocumentState == OpenedDocumentStates.NeedsFullSave
            || tab.DocumentState == OpenedDocumentStates.Corrupted || tab.DocumentState == OpenedDocumentStates.CorruptedAndModified)
            {
                string suffix = "";
                switch (option)
                {
                    case SaveAsOption.Flatten:
                        suffix = " - Flattened";
                        break;
                    case SaveAsOption.Optimize:
                        suffix = " - Reduced";
                        break;
                    case SaveAsOption.Password:
                        if (!string.IsNullOrEmpty(PasswordFileViewModel.CurrentPassword))
                        {
                            suffix = " - Protected";
                        }
                        else
                        {
                            suffix = " - Not_Protected";
                        }
                        break;
                    case SaveAsOption.Crop:
                        suffix = " - Cropped";
                        break;
                    default:
                        suffix = " - Copy";
                        break;
                }
                suggestedFileName += suffix;
            }
            fileSavePicker.SuggestedFileName = suggestedFileName;

            try
            {
                _FilePickerOpen = true;
                file = await fileSavePicker.PickSaveFileAsync();
                SaveNeededStates saveState = CheckSavingRequirements(TabControlViewModel.SelectedTab);
                if (saveState == SaveNeededStates.saveApproved)
                {
                    await SaveToOriginalAsync(TabControlViewModel.SelectedTab, false, true);
                }
            }
            catch (Exception ex)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_APP, pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex));
            }
            finally
            {
                _FilePickerOpen = false;
            }
            if (file != null)
            {
                // check if a user selects an already open tab 
                CompleteReaderPDFViewCtrlTabInfo matchedTab = TabControlViewModel.Tabs.Where(x => x.OriginalFile?.Path == file.Path).FirstOrDefault();
                if (matchedTab != null)
                {
                    // If selecting the same open tab, just save
                    if (matchedTab.IsSelected)
                    {
                        var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        MessageDialog md = new MessageDialog("");
                        switch (option)
                        {
                            case SaveAsOption.Flatten:
                                md = new MessageDialog(string.Format(loader.GetString("ViewerPage_TabConflictDialog_SaveAsFlatten"), matchedTab.Title), loader.GetString("ViewerPage_TabConflictDialog_SaveAsFlatten_Title"));
                                break;
                            case SaveAsOption.Optimize:
                                md = new MessageDialog(string.Format(loader.GetString("ViewerPage_TabConflictDialog_SaveAsOptimize"), matchedTab.Title), loader.GetString("ViewerPage_TabConflictDialog_SaveAsOptimize_Title"));
                                break;
                            case SaveAsOption.Password:
                                md = new MessageDialog(string.Format(loader.GetString("ViewerPage_TabConflictDialog_SaveAsPassword"), matchedTab.Title), loader.GetString("ViewerPage_TabConflictDialog_SaveAsPassword_Title"));
                                break;
                            case SaveAsOption.Crop:
                                md = new MessageDialog(string.Format(loader.GetString("ViewerPage_TabConflictDialog_SaveAsCrop"), matchedTab.Title), loader.GetString("ViewerPage_TabConflictDialog_SaveAsCrop_Title"));
                                break;
                            default:
                                Save();
                                break;
                        }

                        if (option != SaveAsOption.Save)
                        {
                            await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                            IsModal = false;
                        }
                    }
                    // If selecting an open, non-selected tab, close that tab, and continue
                    else
                    {
                        CloseTab(matchedTab);
                        ContinueSaving(file, tab, closeIfsuccessful, navigateIfSucessful, option);
                    }
                }
                else
                {
                    ContinueSaving(file, tab, closeIfsuccessful, navigateIfSucessful, option);
                }
            }
            else if (SaveAsDone != null)
            {
                SaveAsDone(true, false);
            }

            if (file == null)
            {
                IsModal = false;
            }
        }

        private IAsyncAction FlattenAsync(PDFDoc doc)
        {
            Task t = new Task(() =>
            {
                doc.FlattenAnnotations();
            });
            t.Start();
            return t.AsAsyncAction();
        }

        private IAsyncAction OptimizeAsync(PDFDoc doc)
        {
            Task t = new Task(() =>
            {
                OptimizeFileViewModel.Optimize(doc);
            });
            t.Start();
            return t.AsAsyncAction();
        }

        private IAsyncOperation<Tuple<bool, string>> ApplyPasswordAsync(PDFDoc doc)
        {
            Task<Tuple<bool, string>> t = new Task<Tuple<bool, string>>(() =>
            {
                return PasswordFileViewModel.ApplyPassword(doc);
            });
            t.Start();
            return t.AsAsyncOperation();
        }

        private IAsyncAction ApplyCropAsync(PDFDoc doc)
        {
            Task t = new Task(() =>
            {
                ApplyCrop(doc);
            });
            t.Start();
            return t.AsAsyncAction();
        }

        private Dictionary<int, pdftron.PDF.Rect> _UserCropRects = new Dictionary<int, pdftron.PDF.Rect>();

        private async Task SavePermCroppedCopyAsync()
        {
            bool isCropped = await Task.Run(() =>
            {
                return GetUserCropBoxes();
            });

            if (!isCropped)
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                string message = loader.GetString("ViewerPage_PermCropSave_Error");
                string title = loader.GetString("ViewerPage_PermCropSave_Error_Title");

                MessageDialog messageDialog = new MessageDialog(message, title);
                messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_PermCropSave_Error_CropNow"), async (command) =>
                {
                    IsModal = true;
                    await Task.Delay(200);
                    CropPopupViewModel.IsPopupOpen = true;
                }));
                messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Generic_Cancel_Text"), (command) =>
                {
                    IsModal = false;
                }));

                await messageDialog.ShowAsync();
            }
            else
            {
                SaveAs(TabControlViewModel.SelectedTab, false, false, SaveAsOption.Crop);
            }
        }

        private bool GetUserCropBoxes()
        {
            _UserCropRects.Clear();
            PDFDoc pdfDoc = PDFViewCtrl.GetDoc();

            try
            {
                pdfDoc.LockRead();
                PageIterator iter = pdfDoc.GetPageIterator();
                int pageNum = 1;

                while (iter.HasNext())
                {
                    pdftron.PDF.Page page = iter.Current();


                    pdftron.PDF.Rect userCropBox = page.GetBox(PageBox.e_user_crop);
                    pdftron.PDF.Rect cropBox = page.GetCropBox();

                    if (!pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.AreRectsSimilar(userCropBox, cropBox, pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.RECT_COMPARE_THRESHOLD))
                    {
                        _UserCropRects[pageNum] = page.GetBox(PageBox.e_user_crop);
                    }

                    pageNum++;
                    iter.Next();
                }
            }
            catch (Exception e)
            {
                PDFNetException pdfe = new PDFNetException(e.HResult);
                if (!pdfe.IsPDFNetException)
                {
                    throw;
                }
            }
            finally
            {
                pdfDoc.UnlockRead();
            }

            return _UserCropRects.Count != 0;
        }

        private void ApplyCrop(PDFDoc doc)
        {
            try
            {
                doc.LockRead();
                PageIterator iter = doc.GetPageIterator();
                int pageNum = 1;

                while (iter.HasNext())
                {
                    pdftron.PDF.Page page = iter.Current();

                    if (_UserCropRects.ContainsKey(pageNum))
                    {
                        page.SetBox(PageBox.e_crop, _UserCropRects[pageNum]);
                    }
                    
                    pageNum++;
                    iter.Next();
                }
            }
            catch (Exception e)
            {
                PDFNetException pdfe = new PDFNetException(e.HResult);
                if (!pdfe.IsPDFNetException)
                {
                    throw;
                }
            }
            finally
            {
                doc.UnlockRead();
            }
        }

        public async void ContinueSaving(StorageFile file, CompleteReaderPDFViewCtrlTabInfo tab, bool closeIfsuccessful, bool navigateIfSucessful, SaveAsOption option, bool destinationIsTempFile = false)
        {
            if (file != null)
            {
                bool success = false;
                bool passwordError = false;
                string errorMessage = null;
                string oldFilePath = file.Path;

                string path = file.Path;
                if (string.IsNullOrEmpty(path))
                {
                    path = file.Name;
                }

                // The old File record is no longer needed, so we decrement it and create a new one here.
                bool isBox = Utilities.UtilityFunctions.IsBox(file.Path);
                bool isDropBox = Utilities.UtilityFunctions.IsDropBox(file.Path);
                bool isOneDrive = Utilities.UtilityFunctions.IsOneDrive(file.Path);
                bool isRemote = isBox || isDropBox || isOneDrive;
                bool useRWStream = Utilities.UtilityFunctions.IsBox(path) || Utilities.UtilityFunctions.IsDropBox(path);

                NewDocumentProperties docProps = null;
                bool createNewTab = option != SaveAsOption.Save;

                try
                {
                    CurrentSaveAsOption = option;

                    IsSaveTimerOpen = true;
                    //this.PDFViewCtrl.IsEnabled = false;
                    IsModal = true;

                    List<pdftron.PDF.Tools.Controls.ViewModels.UserBookmarksViewModel.UserBookmarkItem> bookmarkItems = null;
                    string documentTag = string.Empty;
                    if (tab.OriginalFile != null)
                    {
                        documentTag = Utilities.UtilityFunctions.GetMaximalAvailablePath(tab.OriginalFile);

                        if (!isDropBox)
                        {
                            bookmarkItems = await pdftron.PDF.Tools.Controls.ViewModels.UserBookmarkManager.GetBookmarkListAsync(documentTag);
                        }
                    }

                    DocumentManager manager = await DocumentManager.GetInstanceAsync();
                    bool generateNewFileName = false;
                    if (oldFilePath == file.Path)
                    {
                        generateNewFileName = true;
                    }
                    StorageFile temporaryFile = await manager.OpenTemporaryCopyAsync(file, generateNewFileName);

                    if (tab.Doc != null)
                    {
                        await tab.SaveDocumentAsync();
                        
                        PDFDoc saveDoc = tab.Doc;
                        if (createNewTab)
                        {

                            // Replace the temporary file created with a copy of the original one: tab.PDFFile
                            await tab.PDFFile.CopyAndReplaceAsync(temporaryFile);

                            docProps = new NewDocumentProperties();
                            docProps.Password = tab.MetaData.Password;
                            docProps.StartPage = tab.PDFViewCtrl.GetCurrentPage();
                            docProps.Zoom = tab.PDFViewCtrl.GetZoom();
                            docProps.HorizontalScrollPosition = tab.PDFViewCtrl.GetHScrollPos();
                            docProps.VerticalScrollPosition = tab.PDFViewCtrl.GetVScrollPos();
                            docProps.PageRotation = tab.PDFViewCtrl.GetRotation();
                            docProps.PresentationMode = tab.PDFViewCtrl.GetPagePresentationMode();
                            docProps.File = file;
                            docProps.TemporaryFile = temporaryFile;
                            docProps.OpenedDocumentState = OpenedDocumentStates.Normal;
                            if (Utilities.UtilityFunctions.IsBox(file.Path) || Utilities.UtilityFunctions.IsDropBox(file.Path) || destinationIsTempFile)
                            {
                                docProps.OpenedDocumentState = OpenedDocumentStates.ReadOnly;
                            }

                            docProps.Doc = new PDFDoc(docProps.TemporaryFile);
                            if (!docProps.Doc.InitSecurityHandler())
                            {
                                docProps.Doc.InitStdSecurityHandler(docProps.Password);
                            }
                            saveDoc = docProps.Doc;
                        }
                        else
                        {
                            await tab.Doc.SaveAsync(temporaryFile, pdftron.SDF.SDFDocSaveOptions.e_incremental);
                        }

                        switch (option)
                        {
                            case SaveAsOption.Flatten:
                                await FlattenAsync(saveDoc);
                                break;
                            case SaveAsOption.Optimize:
                                await OptimizeAsync(saveDoc);
                                OptimizeFileViewModel.IsPopupOpen = false;
                                break;
                            case SaveAsOption.Password:
                                Tuple<bool, string> passwordResult = await ApplyPasswordAsync(saveDoc);
                                passwordError = !passwordResult.Item1;
                                if (!passwordError)
                                {
                                    docProps.Password = passwordResult.Item2;
                                }
                                PasswordFileViewModel.IsPopupOpen = false;
                                break;
                            case SaveAsOption.Crop:
                                await ApplyCropAsync(saveDoc);
                                break;
                        }

                        if (createNewTab)
                        {
                            await docProps.Doc.SaveAsync(pdftron.SDF.SDFDocSaveOptions.e_remove_unused);

                            /* NOTE: at this point we re-open the document due a series of crashes caused after a PDFDoc.SaveAsync(e_remove_unused)
                             * setting free some of the internal objects, probably not used. 
                             * The saved file is actually in good state and it does not cause any issues when opening outside,
                             * but down the road when using PDFViewCtrl.SetDoc(doc) it will cause a crash due the free object when checking 
                             * `obj.IsFree() == false` at DocImpl::LoadObj(Indirect& obj)
                             * 
                             * Also, this issue does not happen on every document.
                             * 
                             * The work around is simply re-opening the same file after saving it.
                             */
                            docProps.Doc = new PDFDoc(docProps.TemporaryFile);
            
                            // Copy all changes from the temporary file to the output original one (flattened, optimized)
                            await manager.AddChangesToOriginal(docProps.File, docProps.TemporaryFile, Utilities.UtilityFunctions.IsDropBox(file.Path));

                            Tuple<DateTimeOffset, ulong> lastModifiedAndSize = await Utilities.UtilityFunctions.GetDateModifiedAndSizeAsync(file);
                            docProps.LastModifiedDate = lastModifiedAndSize.Item1;
                            docProps.Filesize = lastModifiedAndSize.Item2;
                        }
                    }
                    else
                    {
                        await tab.PDFFile.CopyAndReplaceAsync(temporaryFile);
                    }

                    success = !passwordError;

                    if (!createNewTab && success)
                    {
                        await manager.AddChangesToOriginal(file, temporaryFile, Utilities.UtilityFunctions.IsDropBox(file.Path));

                        Tuple<DateTimeOffset, ulong> lastModifiedAndSize = await Utilities.UtilityFunctions.GetDateModifiedAndSizeAsync(file);
                        tab.MetaData.LastModifiedDate = lastModifiedAndSize.Item1;
                        tab.MetaData.FileSize = lastModifiedAndSize.Item2;
                        tab.SystemLastModifiedDate = lastModifiedAndSize.Item1;
                        tab.SystemFileSize = lastModifiedAndSize.Item2;

                        if (bookmarkItems != null && bookmarkItems.Count > 0)
                        {
                            await pdftron.PDF.Tools.Controls.ViewModels.UserBookmarkManager.SaveBookmarkListAsync(documentTag, bookmarkItems);
                        }

                        if (tab.OriginalFile != null)
                        {
                            manager.CloseFile(tab.OriginalFile);
                        }
                        tab.OriginalFile = file;
                        tab.PDFFile = temporaryFile;
                        tab.Title = file.DisplayName;
                        tab.MetaData.OriginalFileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(tab.OriginalFile);
                        tab.MetaData.FutureAccessListToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(tab.PDFFile);

                        tab.DocumentState = OpenedDocumentStates.Normal;
                        if (Utilities.UtilityFunctions.IsBox(file.Path) || Utilities.UtilityFunctions.IsDropBox(file.Path) || destinationIsTempFile)
                        {
                            tab.DocumentState = OpenedDocumentStates.ReadOnly;
                        }

                        bool isEncrypted = !string.IsNullOrEmpty(tab.MetaData.Password);
                        if (Utilities.UtilityFunctions.DoesFileBelongInRecentList(file) && tab.DocumentState != OpenedDocumentStates.ReadOnly)
                        {
                            RecentItemsData recentItems = RecentItemsData.Instance;
                            if (recentItems != null)
                            {
                                recentItems.UpdateWithNewFile(tab.OriginalFile, isEncrypted);
                                if (!isEncrypted)
                                {
                                    pdftron.Common.RecentlyUsedCache.AccessDocument(Utilities.UtilityFunctions.GetMaximalAvailablePath(temporaryFile));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex);
                    AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_APP, errorMessage);
                    success = false;
                }
                finally
                {
                    IsSaveTimerOpen = false;
                    IsModal = false;
                }

                if (success)
                {
                    if (SaveAsDone != null)
                    {
                        SaveAsDone(false, false);
                    }

                    if (closeIfsuccessful)
                    {
                        CloseTab(tab);
                    }
                    else
                    {
                        if (createNewTab)
                        {
                            ActivateWithFile(file, docProps);
                        }
                        else
                        {
                            tab.IsDocumentModifiedSinceLastSave = false;
                            if (_AutoSaveHelper != null)
                            {
                                _AutoSaveHelper.CurrentFile = tab.OriginalFile;
                                _AutoSaveHelper.TemporaryFile = tab.PDFFile;
                            }
                        }
                    }

                    if (navigateIfSucessful)
                    {
                        NavigateAway();
                        return;
                    }

                    if (!closeIfsuccessful && tab.Doc == null)
                    {
                        tab.Activate();
                        ActivateTab(tab);
                    }

                    ResolveAutoSaverAndSaveButton();

                    _SharingHelper.DocumentSaver = SaveHelperAsync;

                    // TODO Phone
                    //CloseAppIfApplicable();

                }
                else
                {
                    Windows.ApplicationModel.Resources.ResourceLoader stringLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    if (passwordError)
                    {
                        MessageDialog md = new MessageDialog(stringLoader.GetString("ViewerPage_SaveWithPasswordFailed_Content"),
                            stringLoader.GetString("ViewerPage_SaveAsFailed_Title"));
                        await MessageDialogHelper.ShowMessageDialogAsync(md);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            errorMessage = "";
                        }
                        
                        MessageDialog md = new MessageDialog(
                            string.Format(stringLoader.GetString("ViewerPage_SaveAsFailed_Content"), Settings.Settings.SupportName, Environment.NewLine + errorMessage),
                            stringLoader.GetString("ViewerPage_SaveAsFailed_Title"));
                        await MessageDialogHelper.ShowMessageDialogAsync(md);
                    }
                    if (SaveAsDone != null)
                    {
                        SaveAsDone(false, true);
                    }
                }
            }
            //WhatsNew.IsReadyToShowWhatsNew = true;
        }

        public async Task SuspendAsync()
        {
            try
            {
                _AutoSaveResultFromSuspend = SaveResult.e_normal;
                if (PDFViewCtrl != null && PDFViewCtrl.GetDoc() != null)
                {
                    SaveNeededStates saveState = CheckSavingRequirements(TabControlViewModel.SelectedTab);
                    if (saveState == SaveNeededStates.saveApproved)
                    {
                        _AutoSaveResultFromSuspend = await SaveToOriginalAsync(TabControlViewModel.SelectedTab, false, true);
                    }
                }
                TabControlViewModel.SaveTabState();
                if (CropPopupViewModel.IsAutomaticCropping)
                {
                    CropPopupViewModel.CancelAutomaticCommand.Execute(null);
                }
                await DeactivateTabAsync(TabControlViewModel.SelectedTab);
            }
            catch (Exception ex)
            {
                string error = string.Empty;

                pdftron.Common.PDFNetException pdfEx = new pdftron.Common.PDFNetException(ex.HResult);
                if (pdfEx.IsPDFNetException)
                {
                    error = pdfEx.ToString();
                }
                else
                {
                    error = ex.ToString();
                }
                System.Diagnostics.Debug.WriteLine(error);
            }
        }

        private System.Threading.SemaphoreSlim _ResumeSemaphore;
        private System.Threading.SemaphoreSlim ResumeSemaphore
        {
            get
            {
                if (_ResumeSemaphore == null)
                {
                    _ResumeSemaphore = new System.Threading.SemaphoreSlim(1);
                }
                return _ResumeSemaphore;
            }
        }

        private SaveResult _AutoSaveResultFromSuspend = SaveResult.e_normal;
        private bool _ShowingAutoSaveResumeMessage = false;

        public async void Resume()
        {
            await ResumeSemaphore.WaitAsync();
            try
            {
                if (TabControlViewModel != null)
                {
                    IsModal = true;
                    await TabControlViewModel.UpdateFilePropertiesAsync();
                    IsModal = false;
                }
            }
            catch (Exception){ }
            finally
            {
                ResumeSemaphore.Release();
            }

            if (_FileToActivate == null)
            {
                if (TabControlViewModel != null && TabControlViewModel.SelectedTab != null)
                {
                    if (!TabControlViewModel.SelectedTab.IsUpToDate && TabControlViewModel.SelectedTab.IsDocumentModifiedSinceLastSave)
                    {
                        ShowConflictingTabOptions(TabControlViewModel.SelectedTab, null);
                    }
                    else
                    {
                        if (!TabControlViewModel.SelectedTab.IsUpToDate)
                        {
                            TabControlViewModel.SelectedTab.UpdateFile();
                        }
                        _PreviousTab = TabControlViewModel.SelectedTab;
                        ActivateTab(TabControlViewModel.SelectedTab);
                        if (_AutoSaveResultFromSuspend != SaveResult.e_normal)
                        {
                            try
                            {
                                _ShowingAutoSaveResumeMessage = true;
                                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                                string message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_Suspend_Message_AccessDenied"),
                                    TabControlViewModel.SelectedTab.Title);
                                if (_AutoSaveResultFromSuspend == SaveResult.e_unknown_error)
                                {
                                    message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_Suspend_Message_unknown"), TabControlViewModel.SelectedTab.Title);
                                }
                                MessageDialog md = new MessageDialog(message, loader.GetString("ViewerPage_AutoSaveFailed_Title"));
                                await MessageDialogHelper.ShowMessageDialogAsync(md);
                            }
                            finally
                            {
                                _ShowingAutoSaveResumeMessage = false;
                            }
                        }
                    }
                }
            }
        }


        #endregion Saving and document Modification


        #region Sub View Models

        private OptimizeFileViewModel _OptimizeFileViewModel;
        public OptimizeFileViewModel OptimizeFileViewModel
        {
            get { return _OptimizeFileViewModel; }
            set { Set(ref _OptimizeFileViewModel, value); }

        }

        private PasswordFileViewModel _PasswordFileViewModel;
        public PasswordFileViewModel PasswordFileViewModel
        {
            get { return _PasswordFileViewModel; }
            set { Set(ref _PasswordFileViewModel, value); }

        }

        private CropPopupViewModel _CropPopupViewModel;
        public CropPopupViewModel CropPopupViewModel
        {
            get { return _CropPopupViewModel; }
            set { Set(ref _CropPopupViewModel, value); }
        }

        private ViewerPageSettingsViewModel _ViewerPageSettingsViewModel;
        public ViewerPageSettingsViewModel ViewerPageSettingsViewModel
        {
            get { return _ViewerPageSettingsViewModel; }
            set
            {
                ViewerPageSettingsViewModel old = _ViewerPageSettingsViewModel;
                if (Set(ref _ViewerPageSettingsViewModel, value) && old != null)
                {
                    old.CleanUp();
                }
            }

        }

        private CustomColorViewModel _CustomColorViewModel;
        public CustomColorViewModel CustomColorViewModel
        {
            get { return _CustomColorViewModel; }
            set { Set(ref _CustomColorViewModel, value); }

        }

        public string CurrentPagePresentationModeSymbol
        {
            get
            {
                if (PDFViewCtrl != null)
                {
                    switch (PDFViewCtrl.GetPagePresentationMode())
                    {
                        case PDFViewCtrlPagePresentationMode.e_single_continuous:
                            return new string((char)0x0052, 1);
                        case PDFViewCtrlPagePresentationMode.e_single_page:
                            return new string((char)0x0061, 1);
                        case PDFViewCtrlPagePresentationMode.e_facing:
                            return new string((char)0x0060, 1);
                        case PDFViewCtrlPagePresentationMode.e_facing_cover:
                            return new string((char)0x0055, 1);
                        case PDFViewCtrlPagePresentationMode.e_facing_continuous_cover:
                            return new string((char)0x0056, 1);
                        case PDFViewCtrlPagePresentationMode.e_facing_continuous:
                            return new string((char)0x0054, 1);
                    }
                }
                return new string((char)0x0061, 1);
            }
        }

        public Windows.UI.Xaml.Thickness CurrentPagePresentationModeSymbolMargin
        {
            get
            {
                if (PDFViewCtrl != null)
                {
                    switch (PDFViewCtrl.GetPagePresentationMode())
                    {
                        case PDFViewCtrlPagePresentationMode.e_facing_cover:
                            return new Windows.UI.Xaml.Thickness(7, 0, 0, 4);
                        case PDFViewCtrlPagePresentationMode.e_facing:
                            return new Windows.UI.Xaml.Thickness(0, 0, 0, 4);
                    }
                }
                return new Windows.UI.Xaml.Thickness(0, 0, 0, 2);
            }
        }

        public double CurrentPagePresentationModeFontSize
        {
            get
            {
                if (PDFViewCtrl != null)
                {
                    switch (PDFViewCtrl.GetPagePresentationMode())
                    {
                        case PDFViewCtrlPagePresentationMode.e_single_continuous:
                        case PDFViewCtrlPagePresentationMode.e_single_page:
                            return 25;
                        case PDFViewCtrlPagePresentationMode.e_facing:
                        case PDFViewCtrlPagePresentationMode.e_facing_continuous:
                        case PDFViewCtrlPagePresentationMode.e_facing_cover:
                        case PDFViewCtrlPagePresentationMode.e_facing_continuous_cover:
                            return 22;
                    }
                }
                return 22;
            }
        }

        private void ViewerPageSettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("PagePresentationMode", StringComparison.OrdinalIgnoreCase))
            {
                IsAppBarOpen = false;
                IsQuickSettingsOpen = false;
                RaisePropertyChanged("IsQuickSettingsOpen");
                RaisePropertyChanged("CurrentPagePresentationModeSymbol");
                RaisePropertyChanged("CurrentPagePresentationModeSymbolMargin");
                RaisePropertyChanged("CurrentPagePresentationModeFontSize");
            }
            if (e.PropertyName.Equals("ColorMode", StringComparison.OrdinalIgnoreCase))
            {
                IsAppBarOpen = false;
                IsQuickSettingsOpen = false;
                RaisePropertyChanged("IsQuickSettingsOpen");
                if (ViewerPageSettingsViewModel.ColorMode != ViewerPageSettingsViewModel.CustomColorModes.Custom)
                {
                    ResolveColorMode(ViewerPageSettingsViewModel.ColorMode);
                }
            }
            if (e.PropertyName.Equals("CustomColor", StringComparison.OrdinalIgnoreCase))
            {
                IsAppBarOpen = false;
                IsQuickSettingsOpen = false;
                RaisePropertyChanged("IsQuickSettingsOpen");
                IsModal = true;
                CustomColorViewModel.IsPopupOpen = true;
            }
            if (e.PropertyName.Equals("FullScreen", StringComparison.OrdinalIgnoreCase))
            {
                RaisePropertyChanged("IsFullScreen");
            }
            if (e.PropertyName.Equals("IsReflow", StringComparison.OrdinalIgnoreCase))
            {
                IsAppBarOpen = false;
                IsQuickSettingsOpen = false;
                RaisePropertyChanged("IsDocumentEditable");
                RaisePropertyChanged("IsDocumentSearchable");
                ResolveReflow();
            }

            // TODO Phone
            //IsViewModeDialogOpen = false;
        }

        void ViewerPageSettingsViewModel_ViewRequestedHandler(ViewerPageSettingsViewModel.RequestableViews view)
        {
            if (view == Helpers.ViewerPageSettingsViewModel.RequestableViews.Thumbnails)
            {
                IsThumbnailsViewOpen = true;
                RaisePropertyChanged("IsThumbnailsViewOpen");
            }

            if (view == ViewerPageSettingsViewModel.RequestableViews.Crop)
            {
                IsAppBarOpen = false;
                IsQuickSettingsOpen = false;
                IsModal = true;
                CropPopupViewModel.IsPopupOpen = true;
            }

            // TODO Phone
            //IsViewModeDialogOpen = false;
        }

        private void CustomColorViewModel_CustomColorRequested(CustomColorIcon icon)
        {
            ViewerPageSettingsViewModel.ColorMode = ViewerPageSettingsViewModel.CustomColorModes.Custom;
            ResolveColorMode(ViewerPageSettingsViewModel.CustomColorModes.Custom);
        }

        private void PopupClosed()
        {
            IsModal = false;
        }

        private void OptimizeFileViewModel_OptimizePopupOptimize()
        {
            SaveAs(TabControlViewModel.SelectedTab, false, false, SaveAsOption.Optimize);
            IsAppBarOpen = false;
            IsModal = true;
        }

        private void PasswordFileViewModel_PasswordConfirmed()
        {
            SaveAs(TabControlViewModel.SelectedTab, false, false, SaveAsOption.Password);
            IsAppBarOpen = false;
            IsModal = true;
        }

        private void CropPopupViewModel_ManualCropRequested()
        {
            IsCropViewOpen = true;
            RaisePropertyChanged("IsCropViewOpen");
        }
        private void CropPopupViewModel_DocumentEdited()
        {
            TabControlViewModel.SelectedTab.NavigationStack.Clear();
            HandleDocumentEditing();
        }

        private ThumbnailViewer _ThumbnailViewer;
        public ThumbnailViewer ThumbnailViewer
        {
            get { return _ThumbnailViewer; }
            set
            {
                if (value != _ThumbnailViewer)
                {
                    if (_ThumbnailViewer != null)
                    {
                        _ThumbnailViewer.ViewModel.CleanUp();
                        if (_ThumbnailViewerPageMovedDelegate != null)
                        {
                            _ThumbnailViewer.ViewModel.PageMoved -= _ThumbnailViewerPageMovedDelegate;
                            _ThumbnailViewer.ViewModel.PageDeleted -= _ThumbnailViewerPageDeletedDelegate;
                            _ThumbnailViewer.ViewModel.PageAdded -= _ThumbnailViewerPageAddedDelegate;
                            _ThumbnailViewer.ViewModel.PageRotated -= _ThumbnailViewerPageRotatedDelegate;
                            _ThumbnailViewerPageMovedDelegate = null;
                            _ThumbnailViewerPageDeletedDelegate = null;
                        }
                        _ThumbnailViewer.PDFViewCtrl = null;
                    }
                    _ThumbnailViewer = value;
                    if (_ThumbnailViewer != null)
                    {
                        _ThumbnailViewer.ViewModel.ReadOnly = CurrentDocumentState == OpenedDocumentStates.Uneditable;
                        _ThumbnailViewerPageMovedDelegate =
                            new pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageMovedDelegate(ThumbnailViewerPageMovedHandler);
                        _ThumbnailViewerPageDeletedDelegate =
                            new pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageDeletedDelegate(ThumbnailViewerPageDeletedHandler);
                        _ThumbnailViewerPageAddedDelegate =
                            new pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageAddedDelegate(ThumbnailViewerPageAddedHandler);
                        _ThumbnailViewerPageRotatedDelegate = new pdftron.PDF.Tools.Controls.ViewModels.ThumbnailsViewViewModel.PageRotatedDelegate(ThumbnailViewerPageRotatedDelegate);
                        _ThumbnailViewer.ViewModel.PageMoved += _ThumbnailViewerPageMovedDelegate;
                        _ThumbnailViewer.ViewModel.PageDeleted += _ThumbnailViewerPageDeletedDelegate;
                        _ThumbnailViewer.ViewModel.PageAdded += _ThumbnailViewerPageAddedDelegate;
                        _ThumbnailViewer.ViewModel.PageRotated += _ThumbnailViewerPageRotatedDelegate;

                    }
                    RaisePropertyChanged();
                }
            }
        }

        private NavigationState _NavigationStateBeforeThumbnailsviewOpening = null;
        private bool _IsThumbnailsViewOpen = false;
        public bool IsThumbnailsViewOpen
        {
            get { return _IsThumbnailsViewOpen; }
            set
            {
                if (value != _IsThumbnailsViewOpen)
                {
                    _IsThumbnailsViewOpen = value;
                    IsEntireAppBarOpen = !_IsThumbnailsViewOpen;

                    if (!_IsThumbnailsViewOpen)
                    {
                        IsOutlineDialogOpen = _OutlineDialogHibernation;

                        if (_ThumbnailViewer != null)
                        {
                            _ThumbnailViewer.CloseControl();
                        }
                        ThumbnailViewer = null;
                        if (_EditedSinceThumbnailViewerOpened)
                        {
                            HandleDocumentEditing();
                        }
                    }
                    else
                    {
                        _OutlineDialogHibernation = IsOutlineDialogOpen;
                        IsOutlineDialogOpen = false;

                        _EditedSinceThumbnailViewerOpened = false;
                        PDFViewCtrl.CancelRendering();
                        if (ToolManager != null)
                        {
                            ToolManager.CreateTool(ToolType.e_pan, ToolManager.CurrentTool);
                        }

                        _NavigationStateBeforeThumbnailsviewOpening = NavigationState.TakeSnapshot(PDFViewCtrl);
                        ThumbnailViewer = new ThumbnailViewer(PDFViewCtrl, Utilities.UtilityFunctions.GetMaximalAvailablePath(CurrentFile));
                        ThumbnailViewer.ToolManager = TabControlViewModel.SelectedTab.ToolManager;

                        if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Night)
                        {
                            ThumbnailViewer.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 44, 44, 44));
                            ThumbnailViewer.BlankPageDefaultColor = Windows.UI.Colors.Black;
                        }
                        else if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Sepia)
                        {
                            ThumbnailViewer.BlankPageDefaultColor = Windows.UI.Color.FromArgb(255, 255, 232, 206);
                        }
                        ThumbnailViewer.ControlClosed += ThumbnailViewer_ControlClosed;
                        IsAppBarOpen = false;
                    }
                    IsModal = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CropView _CropView;

        public CropView CropView
        {
            get { return _CropView; }
            set { Set(ref _CropView, value); }
        }

        private bool _IsCropViewOpen;

        public bool IsCropViewOpen
        {
            get { return _IsCropViewOpen; }
            set
            {
                if (value != _IsCropViewOpen)
                {
                    _IsCropViewOpen = value;
                    IsEntireAppBarOpen = !_IsCropViewOpen;

                    if (!_IsCropViewOpen)
                    {
                        if (_CropView != null)
                        {
                            _CropView.CloseControl();
                        }
                        _CropView = null;
                        PDFViewCtrl.UpdatePageLayout();
                    }
                    else
                    {
                        PDFViewCtrl.CancelRendering();
                        if (ToolManager != null)
                        {
                            ToolManager.CreateTool(ToolType.e_pan, ToolManager.CurrentTool);
                        }
                        CropView = new CropView(PDFViewCtrl);
                        if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Sepia)
                        {
                            CropView.SetColorPostProcessing(PDFRasterizerColorPostProcessMode.e_postprocess_gradient_map, Windows.UI.Color.FromArgb(255, 255, 232, 206), Windows.UI.Colors.Black);
                        }
                        else
                        {
                            CropView.SetColorPostProcessing(PDFViewCtrl.GetColorPostProcessMode(), null, null);
                        }
                        CropView.ControlClosed += CropView_ControlClosed;
                        CropView.ViewModel.DocumentEdited += CropViewModel_DocumentEdited;
                        IsAppBarOpen = false;
                    }
                    IsModal = value;
                    RaisePropertyChanged();
                }
            }
        }

        void ThumbnailViewer_ControlClosed()
        {
            if (_NavigationStateBeforeThumbnailsviewOpening != null)
            {
                NavigationState afterState = NavigationState.TakeSnapshot(PDFViewCtrl);
                TabControlViewModel.SelectedTab.NavigationStack.RegisterJump(_NavigationStateBeforeThumbnailsviewOpening, afterState);
                _NavigationStateBeforeThumbnailsviewOpening = null;
            }
            IsThumbnailsViewOpen = false;
        }

        void CropView_ControlClosed()
        {
            int cropViewPage = CropView.ViewModel.CurrCropItem.Page;
            TabControlViewModel.SelectedTab.NavigationStack.SetCurrentPage(cropViewPage);
            IsCropViewOpen = false;
        }

        private void CropViewModel_DocumentEdited()
        {
            TabControlViewModel.SelectedTab.NavigationStack.Clear();
            HandleDocumentEditing();
        }

        #endregion Sub View Models


        #region Tab Management

        void TabControlViewModel_FixedButtonClicked(CompleteReaderTabControlViewModel tabControlViewModel)
        {
            IsAppBarOpen = false;
            Browse();
        }

        private void TabControlViewModel_CloseButtonClicked(CompleteReaderTabControlViewModel tabControlViewModel,
            CompleteReaderPDFViewCtrlTabInfo clickedTab)
        {
            CloseCurrentTool();
            AppbarInteraction();

            bool conflict = !clickedTab.IsUpToDate && clickedTab.IsDocumentModifiedSinceLastSave;
            if (conflict)
            {
                tabControlViewModel.SelectTab(clickedTab);
            }
            else
            {
                SaveNeededStates status = CheckSavingRequirements(clickedTab);
                _RemovingTab = true;
                CloseTabRequested(status, clickedTab, TabControlViewModel.OpenTabs == 1);
                _RemovingTab = false;
            }
        }

        private RoutedEventHandler _PdfUpdatedHandler = null;
        private RoutedEventHandler _PDFIsReadyHandler = null;
        private CompleteReaderPDFViewCtrlTabInfo _PreviousTab = null;
        private async void TabControlViewModel_VisibleTabChanged(CompleteReaderTabControlViewModel tabControlViewModel)
        {
            if (_PreviousTab == TabControlViewModel.SelectedTab)
            {
                return;
            }
            CloseCurrentTool();
            AppbarInteraction();

            if (_PreviousTab != null)
            {
                DeactivateTab(_PreviousTab);
                SaveNeededStates saveState = CheckSavingRequirements(_PreviousTab);
                bool continueAsNormal = true;
                if (saveState == SaveNeededStates.saveApproved)
                {
                    try
                    {
                        IsSecretlyModal = true;
                        TabControlViewModel.SelectedTab.PDFViewCtrl.IsHitTestVisible = false;
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        SaveResult result = await SaveToOriginalAsync(_PreviousTab, true, true);
                        if (result != SaveResult.e_normal && !_ShowingAutoSaveResumeMessage)
                        {
                            string message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_TabChanged_Message_AccessDenied"), _PreviousTab.Title);
                            if (result == SaveResult.e_unknown_error)
                            {
                                message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_TabChanged_Message_Unknown"), _PreviousTab.Title);
                            }
                            MessageDialog md = new MessageDialog(message, loader.GetString("ViewerPage_AutoSaveFailed_Title"));
                            md.Commands.Add(new UICommand(loader.GetString("ViewerPage_AutoSaveFailed_TabChanged_IgnoreAndDiscard"), (s) => { }));

                            md.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_AutoSaveFailed_TabChanged_ReturnToTab"), (s) =>
                            {
                                continueAsNormal = false;
                            }));

                            IUICommand command = await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                            if (command == null)
                            {
                                continueAsNormal = false;
                            }
                        }
                    }
                    finally
                    {
                        IsSecretlyModal = false;
                        TabControlViewModel.SelectedTab.PDFViewCtrl.IsHitTestVisible = true;
                    }
                }
                if (!continueAsNormal)
                {
                    CompleteReaderPDFViewCtrlTabInfo reactivateTab = _PreviousTab;
                    _PreviousTab = null;
                    TabControlViewModel.SelectTab(reactivateTab);
                    return;
                }
            }

            if (TabControlViewModel.SelectedTab != null)
            {
                CompleteReaderPDFViewCtrlTabInfo previousTab = _PreviousTab;
                _PreviousTab = TabControlViewModel.SelectedTab;
                if (!TabControlViewModel.SelectedTab.IsUpToDate && TabControlViewModel.SelectedTab.IsDocumentModifiedSinceLastSave)
                { 
                    ShowConflictingTabOptions(tabControlViewModel.SelectedTab, previousTab);
                }
                else
                {
                    (this.AnnotationToolbar as pdftron.PDF.Tools.Controls.ControlBase.ICloseableControl).CloseControl();
                    ActivateTab(TabControlViewModel.SelectedTab);
                }
            }
            else
            {
                NavigateAway();
            }

            if (!_RemovingTab)
            {
                if (IsInputMouse)
                {
                    DelayThenCloseAppBar(100);
                }
                else
                {
                    DelayThenCloseAppBar();
                }
            }

            RaisePropertyChanged("PageNumberIndicator");
        }

        private bool _TabIsSubscribed = false;

        private Dictionary<CompleteReaderPDFViewCtrlTabInfo, TappedEventHandler> _TabRightTapHandlers = new Dictionary<CompleteReaderPDFViewCtrlTabInfo, TappedEventHandler>();
        private void TabRightTapHandler(object sender, TappedRoutedEventArgs args)
        {
            if (!IsUiPinned)
            {
                IsAppBarOpen = false;
                IsEntireAppBarOpen = !IsEntireAppBarOpen;
            }
        }

        private async void ActivateTab(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            if (tab == null || TabControlViewModel.SelectedTab != tab || _ActiveTabs.Contains(tab))
            {
                return;
            }

            if (tab.PDFViewCtrl == null)
            {
                _PDFIsReadyHandler = new RoutedEventHandler(PDFIsReadyHander);
                tab.PdfIsReady += _PDFIsReadyHandler;
                return;
            }

            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (tab.FileLoadingError)
            {
                MessageDialog messageDialog = new MessageDialog(string.Format(loader.GetString("ViewerPage_TabControl_TabLoadError_FileMissing_Info"), tab.Title), loader.GetString("ViewerPage_TabControl_TabLoadError_FileMissing_Title"));
                await MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
                CloseTab(tab);
                return;
            }

            if (tab.DocumentState == OpenedDocumentStates.Universal)
            {
                _PdfUpdatedHandler = new RoutedEventHandler(PDFUpdatedHandler);
                tab.PdfUpdated += _PdfUpdatedHandler;
            }

            _ActiveTabs.Add(tab);
            _TabIsSubscribed = true;

            tab.Resume();
            // This is how the tools communicate that they have a modal dialog open
            tab.PDFViewCtrl.IsEnabledChanged += PDFViewCtrl_IsEnabledChanged;

            tab.PDFViewCtrl.OnPageNumberChanged += PDFViewCtrl_OnPageNumberChanged;
            if (tab.DocumentState == OpenedDocumentStates.Universal)
            {
                _OnConversionChangedHandler = new OnConversionEventHandler(PDFViewCtrl_OnConversionChanged);
                tab.PDFViewCtrl.OnConversionChanged += _OnConversionChangedHandler;
            }

            PageNumberIndicator.PDFViewCtrl = tab.PDFViewCtrl;
            PageNumberIndicator.NavigationStack = tab.NavigationStack;

            PDFDoc doc = tab.PDFViewCtrl.GetDoc();
            if (doc != null)
            {
                PDFPrintManager.RegisterForPrintingContract(doc, tab.Title);
            }

            tab.PDFViewCtrl.PointerPressed += PDFViewCtrl_PointerPressed;
            tab.PDFViewCtrl.PointerMoved += PDFViewCtrl_PointerMoved;

            tab.NavigationStack.NavigationStackChanged += NavigationStack_NavigationStackChanged;

            tab.ToolManager.InkSmoothingBehaviour = Settings.Settings.InkSmoothingOption;
            tab.ToolManager.StylusAsPen = Settings.Settings.StylusAsPen;
            tab.PDFViewCtrl.Focus(FocusState.Programmatic);

            AnnotationToolbar.ToolManager = tab.ToolManager;
            if (!IsOutlineDialogOpen && Settings.Settings.OutlineDialogOpen && Utilities.UtilityFunctions.GetDeviceFormFactorType() != Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                IsOutlineDialogOpen = true;
            }
            if (IsOutlineDialogOpen)
            {
                OutlineDialogViewModel = tab.OutlineDialogViewModel;
            }
            ResolveReflow();

            ViewerPageSettingsViewModel = new ViewerPageSettingsViewModel(tab);
            ViewerPageSettingsViewModel.PropertyChanged += _Current.ViewerPageSettingsViewModel_PropertyChanged;
            ViewerPageSettingsViewModel.ViewRequestedHandler += ViewerPageSettingsViewModel_ViewRequestedHandler;

            CustomColorViewModel = new CustomColorViewModel();
            CustomColorViewModel.CustomColorRequested += CustomColorViewModel_CustomColorRequested;
            CustomColorViewModel.PopupClosed += PopupClosed;

            OptimizeFileViewModel = new OptimizeFileViewModel();
            OptimizeFileViewModel.PopupClosed += PopupClosed;
            OptimizeFileViewModel.OptimizeConfirmed += OptimizeFileViewModel_OptimizePopupOptimize;

            bool isPasswordProtected = !string.IsNullOrEmpty(tab.MetaData.Password);
            PasswordFileViewModel = new PasswordFileViewModel(isPasswordProtected);
            PasswordFileViewModel.PopupClosed += PopupClosed;
            PasswordFileViewModel.PasswordConfirmed += PasswordFileViewModel_PasswordConfirmed;

            if (isPasswordProtected)
            {
                SaveAPasswordCopyText = loader.GetString("ViewerPage_SecondaryOptions_ChangePassword");
            }
            else
            {
                SaveAPasswordCopyText = loader.GetString("ViewerPage_SecondaryOptions_Password");
            }

            CropPopupViewModel = new CropPopupViewModel(tab.PDFViewCtrl);
            CropPopupViewModel.PopupClosed += PopupClosed;
            CropPopupViewModel.ManualCropRequested += CropPopupViewModel_ManualCropRequested;
            CropPopupViewModel.DocumentEdited += CropPopupViewModel_DocumentEdited;


            _ToolManager_NewToolCreated = new NewToolCreatedDelegate(ToolManager_NewToolCreated);
            tab.ToolManager.NewToolCreated += _ToolManager_NewToolCreated;
            _ToolManager_SingleTap = new SingleTapDelegate(ToolManager_SingleTap);
            tab.ToolManager.SingleTap += _ToolManager_SingleTap;

            //_AnnotRemovedListener = new pdftron.PDF.Tools.ToolManager.AnnotationModificationHandler(ToolManager_AnnotationModified);
            //tab.ToolManager.AnnotationAdded += _AnnotAddedListener;

            if (!_TabRightTapHandlers.ContainsKey(tab))
            {
                TappedEventHandler handler = new TappedEventHandler(TabRightTapHandler);
                tab.ReflowTapped += handler;
                _TabRightTapHandlers[tab] = handler;
            }

            //GoToPageControl.DataContext = PageNumberIndicator.DataContext;
            _TextToSpeechListener = new pdftron.PDF.Tools.ToolManager.TextToSpeechDelegate(ToolManager_TextToSpeechActivated);
            tab.ToolManager.TextToSpeechActivated += _TextToSpeechListener;

            _AnnotAddedListener = new pdftron.PDF.Tools.ToolManager.AnnotationModificationHandler(ToolManager_AnnotationModified);
            _AnnotEditedListener = new pdftron.PDF.Tools.ToolManager.AnnotationModificationHandler(ToolManager_AnnotationModified);
            _AnnotRemovedListener = new pdftron.PDF.Tools.ToolManager.AnnotationModificationHandler(ToolManager_AnnotationModified);
            tab.ToolManager.AnnotationAdded += _AnnotAddedListener;
            tab.ToolManager.AnnotationEdited += _AnnotEditedListener;
            tab.ToolManager.AnnotationRemoved += _AnnotRemovedListener;

            _AnnotationGroupAddedListener = new pdftron.PDF.Tools.ToolManager.AnnotationGroupModificationHandler(ToolManager_AnnotationGroupModified);
            _AnnotationGroupEditedListener = new pdftron.PDF.Tools.ToolManager.AnnotationGroupModificationHandler(ToolManager_AnnotationGroupModified);
            _AnnotationGroupRemovedListener = new pdftron.PDF.Tools.ToolManager.AnnotationGroupModificationHandler(ToolManager_AnnotationGroupModified);
            tab.ToolManager.AnnotationGroupAdded += _AnnotationGroupAddedListener;
            tab.ToolManager.AnnotationGroupEdited += _AnnotationGroupEditedListener;
            tab.ToolManager.AnnotationGroupRemoved += _AnnotationGroupRemovedListener;

            IsDocumentEditable = tab.DocumentState != OpenedDocumentStates.Uneditable;

            if (tab.OriginalFile == null)
            {
                OpenedDocumentStates oldstate = tab.DocumentState;

                if (oldstate == OpenedDocumentStates.Normal || oldstate == OpenedDocumentStates.NeedsFullSave)
                {
                    tab.DocumentState = OpenedDocumentStates.ReadOnly;
                    MessageDialog messageDialog = new MessageDialog(string.Format(loader.GetString("ViewerPage_TabControl_TabLoadErro_OriginalMissing_Info"), tab.Title), loader.GetString("ViewerPage_TabControl_TabLoadErro_OriginalMissing_Title"));
                    messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("SaveBeforeClosingDialog_SaveAs_Option"), (command) =>
                    {
                        SaveAs(tab, false, false, SaveAsOption.Save);
                    }));
                    messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("SaveBeforeClosingDialog_Discard_Option"), (command) =>
                    {
                        CloseTab(tab);
                    }));
                    messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Generic_Cancel_Text"), (command) =>
                    {

                    }));

                    //WhatsNew.IsReadyToShowWhatsNew = false;
                    await MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
                }
            }

            UpdateTabDependantProperties();

            if (!tab.FileLoadingError)
            {
                //Settings.Settings.PagePresentationMode = tab.PDFViewCtrl.GetPagePresentationMode();
                if (tab.PDFViewCtrl.GetPageCount() >= PAGES_NEEDED_FOR_THUMB_SLIDER)
                {
                    AddThumbnailSlider(tab);

                    PageNumberIndicator.Show();
                }
                else
                {
                    ThumbnailSlider = null;
                }
            }

            UpdateRecentListIfNecessary();

            IsFindTextDialogOpen = false;
        }


        private void AddThumbnailSlider(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            ThumbnailSlider = new ThumbnailSlider();
            ThumbnailSlider.PDFViewCtrl = tab.PDFViewCtrl;
            ThumbnailSlider.NavigationStack = tab.NavigationStack;
            this.ThumbnailSlider.ManipulationStarted += ThumbnailSlider_ManipulationStarted;
            this.ThumbnailSlider.ManipulationCompleted += ThumbnailSlider_ManipulationCompleted;
            this.ThumbnailSlider.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ThumbnailSlider_PointerPressed), true);
            this.ThumbnailSlider.PointerReleased += ThumbnailSlider_PointerReleased;
        }


        void UpdateTabDependantProperties()
        {
            RaisePropertyChanged("");
            RaisePropertyChanged("IsDocumentModifiedSinceLastSave");
            RaisePropertyChanged("IsSaveButtonEnabled");
            RaisePropertyChanged("HasUserBeenWarnedAboutSaving");
            RaisePropertyChanged("HasDocumentBeenModifiedSinceOpening");
            RaisePropertyChanged("CurrentPagePresentationModeSymbol");
            RaisePropertyChanged("CurrentPagePresentationModeSymbolMargin");
            RaisePropertyChanged("CurrentPagePresentationModeFontSize");
            RaisePropertyChanged("IsPreparingForConversion");
            RaisePropertyChanged("IsDocumentSearchable");
            RaisePropertyChanged("IsConverting");
            ResolveAutoSaverAndSaveButton();
            if (ViewerPageSettingsViewModel != null)
            {
                ResolveColorMode(ViewerPageSettingsViewModel.ColorMode);
            }
        }

        private async void DeactivateTab(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            try
            {
                await DeactivateTabAsync(tab);
            }
            catch (Exception ex)
            {
                string error = string.Empty;

                pdftron.Common.PDFNetException pdfEx = new pdftron.Common.PDFNetException(ex.HResult);
                if (pdfEx.IsPDFNetException)
                {
                    error = pdfEx.ToString();
                }
                else
                {
                    error = ex.ToString();
                }
                System.Diagnostics.Debug.WriteLine(error);
            }
        }

        private async Task DeactivateTabAsync(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            if (tab == null || !_ActiveTabs.Contains(tab))
            {
                return;
            }
            if (_PDFIsReadyHandler != null)
            {
                tab.PdfIsReady -= _PDFIsReadyHandler;
                _PDFIsReadyHandler = null;
            }

            if (_PdfUpdatedHandler != null)
            {
                tab.PdfUpdated -= _PdfUpdatedHandler;
                _PdfUpdatedHandler = null;
            }

            if (tab.PDFViewCtrl == null)
            {
                return;
            }

            if (_OnConversionChangedHandler != null)
            {
                tab.PDFViewCtrl.OnConversionChanged -= _OnConversionChangedHandler;
                _OnConversionChangedHandler = null;
            }

            if (!_TabIsSubscribed)
            {
                return;
            }

            if (_TabRightTapHandlers.ContainsKey(tab))
            {
                tab.ReflowTapped -= _TabRightTapHandlers[tab];
                _TabRightTapHandlers.Remove(tab);
            }

            _ActiveTabs.Remove(tab);

            _TabIsSubscribed = false;

            // unsubscribe to events
            tab.Pause();
            tab.PDFViewCtrl.IsEnabledChanged -= PDFViewCtrl_IsEnabledChanged;
            tab.PDFViewCtrl.OnPageNumberChanged -= PDFViewCtrl_OnPageNumberChanged;
            SavePageNumberInRecentList(tab);

            PageNumberIndicator.PDFViewCtrl = null;
            PageNumberIndicator.NavigationStack = null;
            PageNumberIndicator.ReflowViewModel = null;
            tab.PDFViewCtrl.PointerPressed -= PDFViewCtrl_PointerPressed;
            tab.PDFViewCtrl.PointerMoved -= PDFViewCtrl_PointerMoved;

            PDFPrintManager.UnRegisterForPrintingContract();

            IsFindTextDialogOpen = false;

            AnnotationToolbar.ToolManager = null;

            tab.NavigationStack.NavigationStackChanged -= NavigationStack_NavigationStackChanged;

            if (ViewerPageSettingsViewModel != null)
            {
                ViewerPageSettingsViewModel.PropertyChanged -= _Current.ViewerPageSettingsViewModel_PropertyChanged;
                ViewerPageSettingsViewModel.ViewRequestedHandler -= ViewerPageSettingsViewModel_ViewRequestedHandler;
            }

            if (CustomColorViewModel != null)
            {
                CustomColorViewModel.PopupClosed -= PopupClosed;
            }

            if (OptimizeFileViewModel != null)
            {
                OptimizeFileViewModel.PopupClosed -= PopupClosed;
                OptimizeFileViewModel.OptimizeConfirmed -= OptimizeFileViewModel_OptimizePopupOptimize;
            }

            if (PasswordFileViewModel != null)
            {
                PasswordFileViewModel.PopupClosed -= PopupClosed;
                PasswordFileViewModel.PasswordConfirmed -= PasswordFileViewModel_PasswordConfirmed;
            }

            if (CropPopupViewModel != null)
            {
                CropPopupViewModel.PopupClosed -= PopupClosed;
                CropPopupViewModel.ManualCropRequested -= CropPopupViewModel_ManualCropRequested;
                CropPopupViewModel.DocumentEdited -= CropPopupViewModel_DocumentEdited;
                CropPopupViewModel = null;
            }

            tab.ToolManager.NewToolCreated -= _ToolManager_NewToolCreated;
            tab.ToolManager.SingleTap -= _ToolManager_SingleTap;
            _ToolManager_NewToolCreated = null;
            _ToolManager_SingleTap = null;

            tab.ToolManager.TextToSpeechActivated -= _TextToSpeechListener;

            if (MediaElement != null)
            {
                MediaElement.Stop();
                AreAudioButtonsVisible = false;
            }

            tab.ToolManager.AnnotationAdded -= _AnnotAddedListener;
            tab.ToolManager.AnnotationEdited -= _AnnotEditedListener;
            tab.ToolManager.AnnotationRemoved -= _AnnotRemovedListener;
            _AnnotAddedListener = null;
            _AnnotEditedListener = null;
            _AnnotRemovedListener = null;

            tab.ToolManager.AnnotationGroupAdded -= _AnnotationGroupAddedListener;
            tab.ToolManager.AnnotationGroupEdited -= _AnnotationGroupEditedListener;
            tab.ToolManager.AnnotationGroupRemoved -= _AnnotationGroupRemovedListener;
            _AnnotationGroupAddedListener = null;
            _AnnotationGroupEditedListener = null;
            _AnnotationGroupRemovedListener = null;

            if (ThumbnailSlider != null)
            {
                ThumbnailSlider.PDFViewCtrl = null;
            }

            SaveNeededStates saveState = CheckSavingRequirements(tab);
            try
            {
                if (saveState == SaveNeededStates.saveNeeded)
                {
                    await tab.SaveDocumentAsync();
                    System.Diagnostics.Debug.WriteLine("Saving on deactivate done.");
                }
                await tab.CloseOutlineAsync();
                if (tab.DisposeWhenSaved)
                {
                    System.Diagnostics.Debug.WriteLine("Disposing tab " + tab.Title);
                    tab.Doc.Dispose();
                }
            }
            catch (Exception) { }
            await tab.CloseOutlineAsync();
        }

        private class TabConflictChoiceCanceller
        {
            public bool Cancel { get; set; }
            public TabConflictChoiceCanceller()
            {
                Cancel = false;
            }
        }
        private TabConflictChoiceCanceller _CurrentConflictCanceller;
        private async void ShowConflictingTabOptions(CompleteReaderPDFViewCtrlTabInfo conflictedTab, CompleteReaderPDFViewCtrlTabInfo previousTab)
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog messageDialog = new MessageDialog(string.Format(loader.GetString("ViewerPage_TabConflictDialog_Info"), Settings.Settings.DisplayName, conflictedTab.Title),
                loader.GetString("ViewerPage_TabConflictDialog_Title"));

            if (_FileToActivate == null)
            {
                messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_TabConflictDialog_UseFile"), (command) =>
                {
                    ReopenTabWithNewOriginalFile(conflictedTab);
                }));
            }
            else
            {
                messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("SaveBeforeClosingDialog_Discard_Option"), (command) =>
                {
                    DiscardTab(conflictedTab);
                }));
            }
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_TabConflictDialog_SaveAs"), (command) =>
            {
                SaveTabToNewFile(conflictedTab, previousTab);
            }));
            TabConflictChoiceCanceller canceller = new TabConflictChoiceCanceller();
            if (_CurrentConflictCanceller != null)
            {
                _CurrentConflictCanceller.Cancel = true;
            }
            _CurrentConflictCanceller = canceller;
            await Task.Delay(100);
            if (!canceller.Cancel)
            {
                IUICommand selectedChoice = await MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
                if (selectedChoice == null)
                {
                    if (!canceller.Cancel)
                    {
                        SwitchAwayFromCurrentTab(conflictedTab, previousTab);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Choice cancelled");
                    }
                }
            }
        }

        private async void ReopenTabWithNewOriginalFile(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            try
            {
                await DeactivateTabAsync(tab);
                if (TabControlViewModel.SelectedTab == tab)
                {
                    tab.UpdateFile();
                    ActivateTab(tab);
                }
                else
                {
                    tab.IsDocumentModifiedSinceLastSave = false;
                    TabControlViewModel.SelectTab(tab);
                }
            }
            catch (Exception) { }
        }

        private void DiscardTab(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            CloseTabRequested(SaveNeededStates.none, tab, true);
        }

        private async void SaveTabToNewFile(CompleteReaderPDFViewCtrlTabInfo conflictedTab, CompleteReaderPDFViewCtrlTabInfo previousTab)
        {
            await System.Threading.Tasks.Task.Delay(100);
            SaveAsDone += SaveTabToNewFile_SaveDone;
            _TabThatIsMovingFile = conflictedTab;
            _TabToFallBackTo = previousTab;
            SaveAs(conflictedTab, false, false, SaveAsOption.Save);
        }

        private CompleteReaderPDFViewCtrlTabInfo _TabThatIsMovingFile = null;
        private CompleteReaderPDFViewCtrlTabInfo _TabToFallBackTo = null;
        private void SaveTabToNewFile_SaveDone(bool cancelled, bool failed)
        {
            if (cancelled && _TabThatIsMovingFile != null)
            {
                if (_TabToFallBackTo == null)
                {
                    ShowConflictingTabOptions(_TabThatIsMovingFile, null);
                }
                else
                {
                    _FileToActivate = null; // cancel it all
                    SwitchAwayFromCurrentTab(_TabThatIsMovingFile, _TabToFallBackTo);
                }
            }

            if (!cancelled && !failed && _FileToActivate != null)
            {
                SaveNeededStates status = SaveNeededStates.none;
                CloseTabRequested(status, _TabThatIsMovingFile, true);
            }

            if (_TabToLoadAfterResolvingConflict != null)
            {
                CompleteReaderPDFViewCtrlTabInfo tab = _TabToLoadAfterResolvingConflict;
                _TabToLoadAfterResolvingConflict = null;
                TabControlViewModel.SelectTab(tab);
            }

            _TabThatIsMovingFile = null;
            SaveAsDone -= SaveTabToNewFile_SaveDone;
        }

        private void SwitchAwayFromCurrentTab(CompleteReaderPDFViewCtrlTabInfo conflictedTab, CompleteReaderPDFViewCtrlTabInfo previousTab)
        {
            if (previousTab != null)
            {
                TabControlViewModel.SelectTab(previousTab);
            }
            else
            {
                Windows.UI.Xaml.Controls.Frame frame = Window.Current.Content as Windows.UI.Xaml.Controls.Frame;
                if (frame.CanGoBack)
                {
                    NavigateAway();
                }
                else
                {
                    ExitApp();
                }
            }
        }

        private void PDFIsReadyHander(object sender, RoutedEventArgs e)
        {
            CompleteReaderPDFViewCtrlTabInfo tab = sender as CompleteReaderPDFViewCtrlTabInfo;
            if (tab != null)
            {
                ActivateTab(tab);
            }
        }

        private void PDFUpdatedHandler(object sender, RoutedEventArgs e)
        {
            CompleteReaderPDFViewCtrlTabInfo tab = TabControlViewModel.SelectedTab;
            if (tab != null)
            {
                IsDocumentEditable = tab.DocumentState != OpenedDocumentStates.Uneditable && tab.DocumentState != OpenedDocumentStates.Universal;
                RaisePropertyChanged("IsDocumentSearchable");
                ViewerPageSettingsViewModel?.UpdateSettings();
            }

        }

        #endregion Tab Management


        #region Universal Conversion

        private OnConversionEventHandler _OnConversionChangedHandler;

        private void PDFViewCtrl_OnConversionChanged(PDFViewCtrlConversionType type, int totalPagesConverted)
        {
            RaisePropertyChanged("IsPreparingForConversion");
            RaisePropertyChanged("IsDocumentSearchable");
            RaisePropertyChanged("IsConverting");
            if (type == PDFViewCtrlConversionType.e_conversion_progress)
            {
                PageNumberIndicator.UpdatePageNumbers();
                ResolveReflow();
            }
            else
            {
                ResolveThumbnailSlider();
            }
        }

        #endregion Universal Conversion


        #region HotKeys

        private CompleteReader.Utilities.HotKeyHandler _HotKeyHandler;

        /// <summary>
        /// The users pressed a button without any modifier keys down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void HotKeyHandler_KeyPressedEvent(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (IsSecretlyModal)
            {
                return;
            }

            // Handle escape for closing dialogs
            if (args.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                OutlineDialogViewModel?.InteractionOutsideDialog();
                // If something was closed, don't process escape for fullscreen
                if (ToolManager.CloseOpenDialog(true))
                    return;
                if (IsFindTextDialogOpen)
                {
                    IsFindTextDialogOpen = false;
                    return;
                }
            }

            // Exit full screen mode via Escape/F11 if in full screen mode
            if ((args.VirtualKey == Windows.System.VirtualKey.Escape || args.VirtualKey == Windows.System.VirtualKey.F11) && IsFullScreen)
            {
                CloseDismissableDialogs();
                CloseDialogsOnAppBarOpen();
                Utilities.UtilityFunctions.SetFullScreenModeDontWait(false);
            }

            // Enter full screen mode
            else if (args.VirtualKey == Windows.System.VirtualKey.F11 && !IsFullScreen)
            {
                Utilities.UtilityFunctions.SetFullScreenModeDontWait(true);
            }

            if (IsFindTextDialogOpen && FindTextViewModel != null)
            {
                FindTextViewModel.HandleKeyboardEvent(args.VirtualKey, false, false, _HotKeyHandler.IsShiftDown);
            }
        }

        public event EventHandler<object> OpenSearch;

        public void OpenTextSearch()
        {
            OpenSearch?.Invoke(this, this);
        }

        /// <summary>
        /// Ctrl + something was pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HotKeyHandler_HotKeyPressedEvent(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (IsModal)
            {
                return;
            }
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.S:
                    if (IsSaveButtonEnabled && IsSaveButtonVisible)
                    {
                        Save();
                    }
                    break;

                case Windows.System.VirtualKey.F:
                    OpenTextSearch();
                    if (IsFindTextDialogOpen)
                    {
                        FindTextViewModel.FocusTextSearch();
                    }
                    IsFindTextDialogOpen = true;
                    break;

                case Windows.System.VirtualKey.P:
                    try
                    {
                        if (Windows.UI.Xaml.Window.Current.Bounds.Width >= 500)
                        {
                            TryPrint();
                        }
                    }
                    catch (Exception) { }
                    break;

                case Windows.System.VirtualKey.W:
                    SaveNeededStates status = CheckSavingRequirements(TabControlViewModel.SelectedTab);
                    CloseTabRequested(status, TabControlViewModel.SelectedTab, false);
                    if (TabControlViewModel.Tabs.Count == 1)
                    {
                        NavigateAway();
                    }
                    break;

                case Windows.System.VirtualKey.Z:
                    
                    if (ToolManager.CurrentTool.ToolMode == ToolType.e_ink_create)
                    {
                        var annotToolBar = AnnotationToolbar.DataContext as AnnotationToolbarViewModel;
                        annotToolBar.PerformInkUndo();
                        return;
                    }
                    else if (ToolManager.UndoRedoAction.CanUndo)
                    {
                        CloseCurrentTool();
                        ToolManager.UndoRedoAction.DoUndo();
                    }
                    break;

                case Windows.System.VirtualKey.Y:

                    if (ToolManager.CurrentTool.ToolMode == ToolType.e_ink_create)
                    {
                        var annotToolBar = AnnotationToolbar.DataContext as AnnotationToolbarViewModel;
                        annotToolBar.PerformInkRedo();
                        return;
                    }
                    else if (ToolManager.UndoRedoAction.CanRedo)
                    {
                        CloseCurrentTool();
                        ToolManager.UndoRedoAction.DoRedo();
                    }
                    break;

                case Windows.System.VirtualKey.Number1:
                    HotKeyTabHandler(0);
                    break;
                case Windows.System.VirtualKey.Number2:
                    HotKeyTabHandler(1);
                    break;
                case Windows.System.VirtualKey.Number3:
                    HotKeyTabHandler(2);
                    break;
                case Windows.System.VirtualKey.Number4:
                    HotKeyTabHandler(3);
                    break;
                case Windows.System.VirtualKey.Number5:
                    HotKeyTabHandler(4);
                    break;
                case Windows.System.VirtualKey.Number6:
                    HotKeyTabHandler(5);
                    break;
                case Windows.System.VirtualKey.Number9:
                    HotKeyTabHandler(9);
                    break;
                case Windows.System.VirtualKey.Number0:
                    if (PDFViewCtrl != null)
                    {
                        PDFViewCtrl.SetPageViewMode(PDFViewCtrlPageViewMode.e_fit_width, PDFViewCtrl.ActualWidth / 2, PDFViewCtrl.ActualHeight / 2, true);
                    }
                    break;

                case Windows.System.VirtualKey.Tab:
                    if (_HotKeyHandler.IsShiftDown)
                    {
                        HotKeyNextTabHandler(true);
                    }
                    else
                    {
                        HotKeyNextTabHandler(false);
                    }
                    args.Handled = true;
                    break;
            }
        }

        private void HotKeyTabHandler(int index)
        {
            if (index == 9)
            {
                index = TabControlViewModel.OpenTabs - 1;
            }
            if (index >= 0 && TabControlViewModel.OpenTabs > index)
            {
                TabControlViewModel.SelectTab(index);
            }
        }

        private void HotKeyNextTabHandler(bool goBackwards)
        {
            int index = TabControlViewModel.VisibleTabIndex;
            if (goBackwards)
            {
                index--;
                if (index < 0)
                {
                    index = TabControlViewModel.OpenTabs - 1;
                }
            }
            else
            {
                index++;
                if (index >= TabControlViewModel.OpenTabs)
                {
                    index = 0;
                }
            }
            HotKeyTabHandler(index);
        }

        /// <summary>
        /// ctrl + alt + something was pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void HotKeyHandler_AltHotKeyPressedEvent(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (IsModal)
            {
                return;
            }
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.S:
                    if (IsSaveAsButtonVisible)
                    {
                        SaveAs(TabControlViewModel.SelectedTab, false, false, SaveAsOption.Save);
                    }
                    break;
            }
        }

        #endregion HotKeys


        #region Back Key
        void BackButtonHandler_BackPressed(object sender, BackRequestedEventArgs e)
        {
            Windows.UI.Xaml.Controls.Frame frame = Window.Current.Content as Windows.UI.Xaml.Controls.Frame;
            if (frame == null)
            {
                return;
            }

            if (IsAppBarOpen)
            {
                IsAppBarOpen = false;
                e.Handled = true;
                return;
            }

            if (ToolManager != null && ToolManager.CloseOpenDialog())
            {
                e.Handled = true;
                return;
            }
            if (IsAnnotationToolbarOpen)
            {
                if (AnnotationToolbar == null || !AnnotationToolbar.GoBack())
                {
                    IsAnnotationToolbarOpen = false;
                }
                ToolManager.CreateDefaultTool();
                e.Handled = true;
                return;
            }
            if (IsThumbnailsViewOpen && ThumbnailViewer != null)
            {
                if (!ThumbnailViewer.GoBack())
                {
                    IsThumbnailsViewOpen = false;
                }
                e.Handled = true;
                return;
            }
            if (IsOutlineDialogOpen && OutlineDialogViewModel != null)
            {
                if (!OutlineDialogViewModel.GoBack())
                {
                    IsOutlineDialogOpen = false; // we only hide it if the outline dialog didn't take care of itself.
                }
                e.Handled = true;
                return;
            }
            if (IsFindTextDialogOpen && FindTextViewModel != null)
            {
                if (!FindTextViewModel.GoBack())
                {
                    IsFindTextDialogOpen = false;
                }
                e.Handled = true;
                return;
            }
            if (IsCropViewOpen)
            {
                if (!CropView.GoBack())
                {
                    IsFindTextDialogOpen = false;
                }
                e.Handled = true;
                return;
            }

            if (PageNumberIndicator.GoBack())
            {
                e.Handled = true;
                return;
            }

            if (IsFullScreen && Utilities.UtilityFunctions.GetDeviceFormFactorType() != Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                Utilities.UtilityFunctions.SetFullScreenModeDontWait(false);
                e.Handled = true;
                return;
            }

            SaveNeededStates status = SaveNeededStates.none;
            if (TabControlViewModel.SelectedTab != null)
            {
                status = CheckSavingRequirements(TabControlViewModel.SelectedTab);
            }
            if (status == SaveNeededStates.none)
            {
                if (_LaunchedThroughFileActivation)
                {
                    e.Handled = true;
                    if (PDFViewCtrl != null)
                    {
                        PDFViewCtrl.CloseDoc();
                    }
                    ExitApp();
                }
                else
                {
                    e.Handled = true;
                    NavigateAway();
                }
            }
            else
            {
                e.Handled = true;
                if (!frame.CanGoBack)
                {
                    _CloseAppAfterSaving = true;
                }
                if (status == SaveNeededStates.saveApproved)
                {
                    CloseTabRequested(status, TabControlViewModel.SelectedTab, true);
                }
                else
                {
                    NavigateAway();
                }
            }
        }

        #endregion Back Key


        #region AppBar Management

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // These listeners lets us know when not to close the app bar automatically.

        private DispatcherTimer _AppBarClosingTimer;
        private bool _UseTimedAppBar = false;

        private void InitAppBarManagement()
        {
            _AppBarClosingTimer = new DispatcherTimer();
            _AppBarClosingTimer.Interval = TimeSpan.FromSeconds(5);
            _AppBarClosingTimer.Tick += AppBarClosingTimer_Tick;
        }

        // Auto closing the app bar
        private void AppbarInteraction()
        {
            OutlineDialogViewModel?.InteractionOutsideDialog();
            if (_UseTimedAppBar)
            {
                this._AppBarClosingTimer.Stop();
                this._AppBarClosingTimer.Start();
            }
        }

        private void CloseCurrentTool(bool creationToolsOnly = false)
        {
            if (ToolManager != null)
            {
                if (!creationToolsOnly)
                {
                    ToolManager.CreateDefaultTool();
                }
                else
                {
                    ToolType currentToolType = ToolManager.CurrentTool.ToolMode;
                    if (currentToolType != ToolType.e_text_select &&
                        currentToolType != ToolType.e_pan)
                    {
                        ToolManager.CreateDefaultTool();
                    }
                }
            }
            
        }

        private void AppBarClosingTimer_Tick(object sender, object e)
        {
            DispatcherTimer timer = sender as DispatcherTimer;

            if (IsFindTextDialogOpen || IsAnnotationToolbarOpen)
            {
                timer.Stop();
                return;
            }

            IsAppBarOpen = false;
            if (!Settings.Settings.PinCommandBar)
            { 
                IsEntireAppBarOpen = false;
            }

            timer.Stop();
        }

        void ThumbnailSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_UseTimedAppBar)
            {
                _AppBarClosingTimer.Start();
            }
        }

        private void ThumbnailSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (_UseTimedAppBar)
            {
                _AppBarClosingTimer.Stop();
            }
        }

        void ThumbnailSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_UseTimedAppBar)
            {
                _AppBarClosingTimer.Stop();
            }
        }

        void ThumbnailSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_UseTimedAppBar)
            {
                _AppBarClosingTimer.Start();
            }
        }

        #endregion AppBar Management


        #region File Association

        private StorageFile _FileToActivate;
        private NewDocumentProperties _FileToActivateDocumentProperties;
        private bool _LaunchedThroughFileActivation = false;
        private CompleteReaderPDFViewCtrlTabInfo _TabToLoadAfterResolvingConflict = null;

        private FileOpening.DocumentOpener _DocumentOpener;
        public DocumentOpener DocumentOpener
        {
            get { return _DocumentOpener; }
            set { Set(ref _DocumentOpener, value); }
        }

        /// <summary>
        /// Use this to activate the current view with a file.
        /// </summary>
        /// <param name="file">The file to open</param>
        public async void ActivateWithFile(StorageFile file, NewDocumentProperties properties)
        {
            await ResumeSemaphore.WaitAsync();
            try
            {
                bool wereTabsReady = true;
                if (!CompleteReaderTabControlViewModel.IsReady)
                {
                    IsModal = true;
                    wereTabsReady = false;
                    await CompleteReaderTabControlViewModel.GetInstanceAsync();
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

                if (wereTabsReady)
                {
                    await CompleteReaderTabControlViewModel.Instance.UpdateFilePropertiesAsync();
                }
            }
            catch (Exception) { }
            finally
            {
                ResumeSemaphore.Release();
            }

            _FileToActivate = file;
            _LaunchedThroughFileActivation = true;
            _FileToActivateDocumentProperties = properties;

            TabControlViewModel = GetTabControlViewModel();
            TabControlViewModel.MaximumItems = MAX_TABS;

            if (_CurrentConflictCanceller != null)
            {
                _CurrentConflictCanceller.Cancel = true;
            }

            if (TabControlViewModel.ContainsFile(file))
            {
                _FileToActivate = null;
                foreach (CompleteReaderPDFViewCtrlTabInfo tab in TabControlViewModel.Tabs)
                {
                    if (Utilities.UtilityFunctions.AreFilesEqual(tab.OriginalFile, file))
                    {
                        if (!tab.IsUpToDate && tab.IsDocumentModifiedSinceLastSave)
                        {
                            _TabToLoadAfterResolvingConflict = tab;
                            ShowConflictingTabOptions(tab, null);
                        }
                        else
                        {
                            if (tab.MetaData.IsOpenedThroughDrop
                                && (file.Attributes & FileAttributes.Temporary) != FileAttributes.Temporary
                                && (file.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                            {
                                tab.OriginalFile = file;
                                tab.DocumentState = OpenedDocumentStates.Normal;
                                tab.MetaData.IsOpenedThroughDrop = false;
                                UpdateTabDependantProperties();
                            }
                            TabControlViewModel.SelectTab(tab);
                            UpdateRecentListIfNecessary();
                        }
                        return;
                    }
                }
            }
            else
            {
                if (TabControlViewModel.OpenTabs >= MAX_TABS)
                {
                    bool conflict = !TabControlViewModel.OldestViewedTab.IsUpToDate && TabControlViewModel.OldestViewedTab.IsDocumentModifiedSinceLastSave;
                    if (conflict)
                    {
                        ShowConflictingTabOptions(TabControlViewModel.OldestViewedTab, null);
                        //ShowConflictingTaskOptions(TabControlViewModel.OldestViewedTab, null);
                        //TabControlViewModel.SelectTab(TabControlViewModel.OldestViewedTab);
                    }
                    else
                    {
                        SaveNeededStates status = CheckSavingRequirements(TabControlViewModel.OldestViewedTab);
                        CloseTabRequested(status, TabControlViewModel.OldestViewedTab, true);
                    }
                }
                else
                {
                    ContinueFileActivation();
                }
            }
        }

        private void ContinueFileActivation()
        {
            StorageFile file = _FileToActivate;
            _FileToActivate = null;

            _SuppressRecentListUpdate = false;

            if (_FileToActivateDocumentProperties != null && !_FileToActivateDocumentProperties.OpenedThroughDrop)
            {
                DocumentOpener_DocumentReady(_FileToActivateDocumentProperties);

                _FileToActivateDocumentProperties = null;
            }
            else
            {
                IsModal = true;
                DocumentOpener = new DocumentOpener();
                DocumentOpener.DocumentReady += DocumentOpener_DocumentReady;
                DocumentOpener.ModalityChanged += DocumentOpener_ModalityChanged;
                DocumentOpener.DocumentLoadingFailed += DocumentOpener_DocumentLoadingFailed;
                NewDocumentProperties props = _FileToActivateDocumentProperties;
                _FileToActivateDocumentProperties = null;
                DocumentOpener.Open(file, props);
            }
        }

        void DocumentOpener_ModalityChanged(bool isModal)
        {
            IsModal = isModal;
        }

        void DocumentOpener_DocumentReady(NewDocumentProperties selectedFileProperties)
        {
            DocumentOpener = null;
            InitPDFView(selectedFileProperties);
            UpdateRecentListIfNecessary();
            IsModal = false;
        }

        void DocumentOpener_DocumentLoadingFailed(object sender, RoutedEventArgs e)
        {
            IsModal = false;
            DocumentOpener = null;
            if (TabControlViewModel.OpenTabs > 0)
            {
                TabControlViewModel.ShowLastViewedTab();
            }
            else
            {
                NavigateAway();
            }
        }

        private async void ExitApp()
        {
            try
            {
                if (TabControlViewModel.SelectedTab != null && !TabControlViewModel.SelectedTab.IsFixedItem)
                {
                    await TabControlViewModel.SelectedTab.SaveDocumentAsync();
                }
                TabControlViewModel.SaveTabState();
            }
            catch (Exception) { }
            Application.Current.Exit();
        }

        #endregion File Association


        #region Utility functions

        private void CloseDismissableDialogs()
        {
            IsFindTextDialogOpen = false;
            IsDismissableDialogOpen = false;
        }

        private void CloseDialogsOnAppBarOpen()
        {   // Don't close Find Text dialog when expanding app bar
            //IsOutlineDialogOpen = false;
            //IsDismissableDialogOpen = false;
        }

        private async void DelayThenCloseAppBar(int milleseconds = 300)
        {
            await Task.Delay(milleseconds);
            IsAppBarOpen = false;
        }

        /// <summary>
        ///  This is only used internally to avoid duplicating checkds for what saving to do.
        /// </summary>
        private enum SaveNeededStates
        {
            none,
            saveApproved,
            saveNeeded,
        }

        private SaveNeededStates CheckSavingRequirements(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            if (tab == null)
            {
                return SaveNeededStates.none;
            }
            if (Settings.Settings.AutoSaveOn && tab.IsDocumentModifiedSinceLastSave &&
                (tab.DocumentState == OpenedDocumentStates.Normal
                || tab.DocumentState == OpenedDocumentStates.NeedsFullSave))
            {
                return SaveNeededStates.saveApproved;
            }
            else if (tab.DocumentState == OpenedDocumentStates.CorruptedAndModified
                || tab.DocumentState == OpenedDocumentStates.Created
                || (tab.DocumentState == OpenedDocumentStates.ReadOnly && tab.HasDocumentBeenModifiedSinceOpening)
                || (tab.DocumentState == OpenedDocumentStates.Normal && tab.IsDocumentModifiedSinceLastSave)
                || (tab.DocumentState == OpenedDocumentStates.NeedsFullSave && tab.IsDocumentModifiedSinceLastSave)
                || (tab.DocumentState == OpenedDocumentStates.NonePDF && tab.IsDocumentModifiedSinceLastSave)
                )
            {
                return SaveNeededStates.saveNeeded;
            }
            return SaveNeededStates.none;
        }

        private async void CloseTabRequested(SaveNeededStates saveNeededStatus, CompleteReaderPDFViewCtrlTabInfo tabToClose, bool navigateWhenDone)
        {
            CloseDismissableDialogs();
            CloseDialogsOnAppBarOpen();

            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            if (saveNeededStatus == SaveNeededStates.saveApproved)
            {
                if (tabToClose.PDFViewCtrl != null)
                {
                    tabToClose.PDFViewCtrl.CancelRendering();
                    tabToClose.PDFViewCtrl.CancelAllThumbRequests();
                }
                try
                {
                    IsSecretlyModal = true;
                    if (tabToClose.PDFViewCtrl != null)
                    {
                        tabToClose.PDFViewCtrl.IsHitTestVisible = false;
                    }
                    SaveResult result = await SaveToOriginalAsync(tabToClose, true, true);
                    if (result != SaveResult.e_normal)
                    {
                        string message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_Close_Message_AccessDenied"),
                            tabToClose.Title, loader.GetString("Generic_Cancel_Text"));
                        if (result == SaveResult.e_unknown_error)
                        {
                            message = string.Format(loader.GetString("ViewerPage_AutoSaveFailed_Close_Message_Unknown"), tabToClose.Title);
                        }
                        MessageDialog md = new MessageDialog(message, loader.GetString("ViewerPage_AutoSaveFailed_Title"));
                        md.Commands.Add(new UICommand(loader.GetString("ViewerPage_AutoSaveFailed_Close_Discard"), (s) =>
                        {
                            tabToClose.DocumentState = OpenedDocumentStates.None;
                            CloseTab(tabToClose);
                        // TODO : Phone devices may want to leave the app at this point if the app was activated through app picker
                        if (false && _LaunchedThroughFileActivation && _FileToActivate == null)
                            {
                                ExitApp();
                            }
                            else if (navigateWhenDone)
                            {
                                NavigateAway();
                            }
                        }));
                        md.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Generic_Cancel_Text"), (s) =>
                        {
                            _FileToActivate = null;
                        }));

                        IUICommand command = await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        if (command == null)
                        {
                            _FileToActivate = null;
                        }
                    }
                    else
                    {
                        CloseTab(tabToClose);
                        if (navigateWhenDone)
                        {
                            NavigateAway();
                        }
                    }
                }
                finally
                {
                    IsSecretlyModal = false;
                    if (tabToClose.PDFViewCtrl != null)
                    {
                        tabToClose.PDFViewCtrl.IsHitTestVisible = true;
                    }
                }
            }
            else if (saveNeededStatus == SaveNeededStates.saveNeeded)
            {
                string message = string.Format(loader.GetString("SaveBeforeClosingDialog_Info"), tabToClose.Title);

                Windows.UI.Popups.MessageDialog messageDialog = new Windows.UI.Popups.MessageDialog(message, loader.GetString("SaveBeforeClosingDialog_Title"));
                if (tabToClose.DocumentState == OpenedDocumentStates.Normal || tabToClose.DocumentState == OpenedDocumentStates.NeedsFullSave)
                {
                    messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("SaveBeforeClosingDialog_Save_Option"), (s) =>
                    {
                        SaveAndCloseTab(tabToClose, navigateWhenDone);
                    }));
                }
                else
                {
                    messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("SaveBeforeClosingDialog_SaveAs_Option"), (s) =>
                    {
                        // We can't call SaveAs(true) here, or we'll get an UnauthorizedAccessException when trying to open the file picker.
                        DelaySaveAs(tabToClose, true, navigateWhenDone);
                    }));
                }

                messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("SaveBeforeClosingDialog_Discard_Option"), (s) =>
                {
                    tabToClose.DocumentState = OpenedDocumentStates.None;
                    CloseTab(tabToClose);
                    // TODO : Phone devices may want to leave the app at this point if the app was activated through app picker
                    if (false && _LaunchedThroughFileActivation && _FileToActivate == null)
                    {
                        ExitApp();
                    }
                    else if (navigateWhenDone)
                    {
                        NavigateAway();
                    }
                }));

                if (Utilities.UtilityFunctions.GetDeviceFormFactorType() != Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
                {
                    messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Generic_Cancel_Text"), (s) =>
                    {
                        _FileToActivate = null;
                    }));
                }

                IUICommand command = await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
                if (command == null)
                {
                    _FileToActivate = null;
                }
            }
            else
            {
                if (_TabToLoadAfterResolvingConflict != null)
                {
                    CompleteReaderPDFViewCtrlTabInfo tab = _TabToLoadAfterResolvingConflict;
                    _TabToLoadAfterResolvingConflict = null;
                    TabControlViewModel.SelectTab(tab);
                }
                else
                {
                    CloseTab(tabToClose);
                    if (navigateWhenDone)
                    {
                        NavigateAway();
                    }
                }
            }
        }

        private async void SaveAndCloseTab(CompleteReaderPDFViewCtrlTabInfo tab, bool navigateWhenDone)
        {
            await SaveHelperAsync(tab, true, true);
            CloseTab(tab);
            if (navigateWhenDone)
            {
                NavigateAway();
            }
        }

        private async void DelaySaveAs(CompleteReaderPDFViewCtrlTabInfo tab, bool closeWhenDone, bool quitWhenDone)
        {
            // We wait a little, and the save picker can safely be open.
            await System.Threading.Tasks.Task.Delay(100);
            if (_LaunchedThroughFileActivation)
            {
                quitWhenDone = false;
            }
            SaveAs(tab, closeWhenDone, quitWhenDone, SaveAsOption.Save);
        }

        private void CloseTab(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            //WhatsNew.IsReadyToShowWhatsNew = true;
            DeactivateReflow();
            tab?.NavigationStack?.Unsubscribe();
            DeactivateTab(tab);
            if (TabControlViewModel != null)
            {
                if (TabControlViewModel.SelectedTab == tab)
                {
                    _PreviousTab = null;
                }
                TabControlViewModel.CloseTab(tab, _FileToActivate == null);
            }
        }

        /// <summary>
        /// Closes the document and turns off all auto-saving, etc. The document will be disposed, so ensure it is saved before calling this function.
        /// </summary>
        private void NavigateAway()
        {
            _AppBarClosingTimer.Stop();
            TurnOffAutoSave();
            if (_FileToActivate != null)
            {
                ContinueFileActivation();
            }
            else if (NewINavigableAvailable != null)
            {
                NewINavigableAvailable(this, ViewModels.Document.DocumentViewModel.Current);
            }
        }

        protected async void Browse()
        {
            if (_FilePickerOpen)
            {
                return;
            }

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
                // apparently, this sometimes throws a System.Exception "Element not found" for no apparent reason. We want to catch that.
                file = await fileOpenPicker.PickSingleFileAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                _FilePickerOpen = false;
            }

            if (file != null)
            {
                if (DocumentOpener == null)
                {
                    DocumentOpener = new DocumentOpener();
                    DocumentOpener.DocumentReady += DocumentOpener_DocumentReady;
                    DocumentOpener.ModalityChanged += DocumentOpener_ModalityChanged;
                    DocumentOpener.DocumentLoadingFailed += DocumentOpener_DocumentLoadingFailed;
                }
                else
                {
                    DocumentOpener.CancelDocumentOpening();
                }

                DocumentOpener.Open(file);
            }
        }

        private void ResolveColorMode(ViewerPageSettingsViewModel.CustomColorModes mode)
        {
            if (PDFViewCtrl != null)
            {
                PDFRasterizerColorPostProcessMode oldMode = PDFViewCtrl.GetColorPostProcessMode();
                if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Night && oldMode != PDFRasterizerColorPostProcessMode.e_postprocess_night_mode)
                {
                    // reflow inject css
                    PDFViewCtrl.SetColorPostProcessMode(PDFRasterizerColorPostProcessMode.e_postprocess_night_mode);
                    Windows.UI.Color viewerBackground = (Windows.UI.Color)App.Current.Resources["MainViewerNightModeBackgroundColor"];
                    PDFViewCtrl.SetBackgroundColor(viewerBackground);
                    PDFViewCtrl.Update(true);
                }
                else if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Sepia)
                {
                    Windows.UI.Color paperColor = (Windows.UI.Color)App.Current.Resources["ViewerSepiaPaperColor"];
                    Windows.UI.Color textColor = (Windows.UI.Color)App.Current.Resources["ViewerSepiaTextColor"];
                    if (!Utilities.UtilityFunctions.MatchesColorPostProcessMode(PDFViewCtrl, PDFRasterizerColorPostProcessMode.e_postprocess_gradient_map, paperColor, textColor))
                    {
                        PDFViewCtrl.SetColorPostProcessMode(PDFRasterizerColorPostProcessMode.e_postprocess_gradient_map);
                        PDFViewCtrl.SetBackgroundColor(Utilities.UtilityFunctions.GetViewerBackgroundColor(paperColor));
                        PDFViewCtrl.SetColorPostProcessColors(paperColor, textColor);
                        PDFViewCtrl.Update(true);
                    }
                }
                else if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.none && oldMode != PDFRasterizerColorPostProcessMode.e_postprocess_none)
                {
                    PDFViewCtrl.SetColorPostProcessMode(PDFRasterizerColorPostProcessMode.e_postprocess_none);
                    Windows.UI.Color viewerBackground = (Windows.UI.Color)App.Current.Resources["MainViewerBackgroundColor"];
                    PDFViewCtrl.SetBackgroundColor(viewerBackground);
                    PDFViewCtrl.Update(true);
                }
                else if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Custom)
                {
                    int iconIndex = Settings.Settings.CurrentCustomColorIcon;
                    Tuple<Windows.UI.Color, Windows.UI.Color> colors = Settings.Settings.GetCustomColors()[iconIndex];
                    if (!Utilities.UtilityFunctions.MatchesColorPostProcessMode(PDFViewCtrl, PDFRasterizerColorPostProcessMode.e_postprocess_gradient_map, colors.Item1, colors.Item2))
                    {
                        PDFViewCtrl.SetColorPostProcessMode(PDFRasterizerColorPostProcessMode.e_postprocess_gradient_map);
                        PDFViewCtrl.SetColorPostProcessColors(colors.Item1, colors.Item2);
                        PDFViewCtrl.SetBackgroundColor(Utilities.UtilityFunctions.GetViewerBackgroundColor(colors.Item1));
                        PDFViewCtrl.Update(true);
                    }
                }

                ResolveReflowAndOutlineColors();
            }
        }

        private void ResolveReflowAndOutlineColors()
        {
            bool changeReflow = TabControlViewModel.SelectedTab.IsReflow &&
                TabControlViewModel.SelectedTab.ReflowView != null
                && TabControlViewModel.SelectedTab.ReflowView.ReflowViewModel != null;
            bool changeOutline = IsOutlineDialogOpen && OutlineDialogViewModel != null;
            if (PDFViewCtrl != null && (changeReflow || changeOutline))
            {
                PDFRasterizerColorPostProcessMode mode = PDFViewCtrl.GetColorPostProcessMode();
                Windows.UI.Color newWhite = Windows.UI.Colors.White;
                Windows.UI.Color newBlack = Windows.UI.Colors.Black;
                if (mode != PDFRasterizerColorPostProcessMode.e_postprocess_none)
                {
                    newWhite = pdftron.PDF.Tools.UtilityFunctions.GetPostProcessedColor(newWhite, PDFViewCtrl);
                    newBlack = pdftron.PDF.Tools.UtilityFunctions.GetPostProcessedColor(newBlack, PDFViewCtrl);
                }
                if (changeReflow)
                {
                    TabControlViewModel.SelectedTab.ReflowView.ReflowViewModel.SetColorPostProcessingMode(mode, newWhite, newBlack);
                }
                if (changeOutline)
                {
                    OutlineDialogViewModel.Thumbnails.BlankPageDefaultColor = newWhite;
                }
            }
        }

        private Tuple<CompleteReaderPDFViewCtrlTabInfo, ReflowViewModel> _CurrentReflow = null;
        private async void ResolveReflow()
        {
            CompleteReaderPDFViewCtrlTabInfo tab = TabControlViewModel.SelectedTab;
            if (tab != null)
            {
                if ((_CurrentReflow != null && tab != _CurrentReflow.Item1 && _CurrentReflow.Item2 != null)
                    || _CurrentReflow != null && _CurrentReflow.Item2 != null && !_CurrentReflow.Item1.IsReflow)
                {
                    DeactivateReflow();
                }

                if (tab.IsReflow && tab.ReflowView != null)
                {
                    _CurrentReflow = new Tuple<CompleteReaderPDFViewCtrlTabInfo, ReflowViewModel>(tab, tab.ReflowView.ReflowViewModel);
                    _CurrentReflow.Item2.PageChanged += CurrentreflowViewModel_PageChanged;
                    PageNumberIndicator.ReflowViewModel = tab.ReflowView.ReflowViewModel;

                    tab.NavigationStack.ReflowViewModel = tab.ReflowView.ReflowViewModel;
                    tab.NavigationStack.CurrentNavigator = NavigationStack.Navigators.ReflowView;
                }
                RaisePropertyChanged("IsReflow");
                if (tab.IsReflow && tab.ReflowView != null)
                {
                    await Task.Delay(200);
                    if (tab.IsReflow && tab.ReflowView != null)
                    {
                        tab.ReflowView.SetFocus(FocusState.Programmatic);
                    }
                }

                ResolveReflowAndOutlineColors();
            }
        }

        private void DeactivateReflow()
        {
            if (_CurrentReflow != null)
            {
                _CurrentReflow.Item2.PageChanged -= CurrentreflowViewModel_PageChanged;
                PageNumberIndicator.ReflowViewModel = null;

                _CurrentReflow.Item1.NavigationStack.ReflowViewModel = null;
                _CurrentReflow.Item1.NavigationStack.CurrentNavigator = NavigationStack.Navigators.PDFViewCtrl;
            }
            _CurrentReflow = null;
        }

        private void CurrentreflowViewModel_PageChanged(int pageNumber, int totalPages)
        {
            if (PDFViewCtrl != null)
            {
                PDFViewCtrl.SetCurrentPage(pageNumber);
                PDFViewCtrl.CancelRendering();
            }
        }

        private void UpdateRecentListIfNecessary(CompleteReaderPDFViewCtrlTabInfo tab = null)
        {
            if (tab == null)
            {
                tab = TabControlViewModel.SelectedTab;
            }

            if (tab == null)
            {
                return;
            }

            if (tab.OriginalFile != null && !_SuppressRecentListUpdate &&
                Utilities.UtilityFunctions.DoesFileBelongInRecentList(tab.OriginalFile) && 
                (tab.DocumentState == OpenedDocumentStates.Normal
                || tab.DocumentState == OpenedDocumentStates.Corrupted
                || tab.DocumentState == OpenedDocumentStates.CorruptedAndModified
                || tab.DocumentState == OpenedDocumentStates.Uneditable))
            {
                bool isEnctypted = !string.IsNullOrEmpty(tab.MetaData.Password);
                if (!isEnctypted)
                {
                    // This needs to happen before any await is called. Otherwise, it will happen after the document is opened in the viewer.
                    // If that is the case, thumbnails won't be added to the recently used list.
                    pdftron.Common.RecentlyUsedCache.AccessDocument(tab.PDFFile.Path);
                }
                RecentItemsData recentItems = RecentItemsData.Instance;
                if (recentItems != null)
                {
                    MRUToken = recentItems.UpdateWithRecentFile(tab.OriginalFile, isEnctypted);
                }
            }
        }

        private async void ShowCorruptedMessage()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog messageDialog = new MessageDialog(string.Format(loader.GetString("DocumentsPage_FileOpeningError_CorruptFile_Info"), Settings.Settings.DisplayName), loader.GetString("DocumentsPage_FileOpeningError_Title"));
            await MessageDialogHelper.ShowMessageDialogAsync(messageDialog);

            NavigateAway();
        }

        private void SavePageNumberInRecentList()
        {
            SavePageNumberInRecentList(TabControlViewModel.SelectedTab);
        }

        private async void SavePageNumberInRecentList(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            await SavePageNumberInRecentListAsync(tab);
        }

        private async Task SavePageNumberInRecentListAsync(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            if (tab == null)
            {
                return;
            }
            string mruToken = tab.MetaData.MostRecentlyUsedToken;
            if (Settings.Settings.RememberLastPage && mruToken != null)
            {
                try
                {

                    CompleteReader.Collections.RecentItemsData recentItems = CompleteReader.Collections.RecentItemsData.Instance;
                    await recentItems.UpdatePropertiesAsync(mruToken, tab.PDFViewCtrl, tab.IsReflow);
                }
                catch (Exception)
                {

                }
            }
        }

        public async void NotifyReadOnlyDocument()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog messageDialog = new MessageDialog(string.Format(loader.GetString("Viewerpage_DocumentModifiedDialog_ReadOnly_Info"),
                loader.GetString("ViewerPage_FileOpeningError_SaveACopy_Option")), loader.GetString("Viewerpage_DocumentModifiedDialog_ReadOnly_Title"));

            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_FileOpeningError_SaveACopy_Option"), (command) =>
            {
                // We can't call SaveAs(false) here, or we'll get an UnauthorizedAccessException when trying to open the file picker.
                DelaySaveAs(TabControlViewModel.SelectedTab, false, false);
            }));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Viewerpage_DocumentModifiedDialog_ReadOnly_Dismiss"), (command) =>
            {
            }));
            await MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
        }

        public async void NotifyCreatedDocument()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog messageDialog = new MessageDialog(string.Format(loader.GetString("Viewerpage_DocumentModifiedDialog_Created_Info"),
                loader.GetString("ViewerPage_FileOpeningError_SaveACopy_Option")), loader.GetString("Viewerpage_DocumentModifiedDialog_Created_Title"));

            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_FileOpeningError_SaveACopy_Option"), (command) =>
            {
                // We can't call SaveAs(false) here, or we'll get an UnauthorizedAccessException when trying to open the file picker.
                DelaySaveAs(TabControlViewModel.SelectedTab, false, false);
            }));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Viewerpage_DocumentModifiedDialog_Created_Dismiss"), (command) =>
            {
            }));
            await MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
        }

        public async void NotyfyReparablePDFDoc()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog messageDialog = new MessageDialog(string.Format(loader.GetString("Viewerpage_DocumentModifiedDialog_Corrupted_Info"), Settings.Settings.DisplayName
                , loader.GetString("ViewerPage_FileOpeningError_SaveACopy_Option")), loader.GetString("Viewerpage_DocumentModifiedDialog_Corrupted_Title"));


            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("ViewerPage_FileOpeningError_SaveACopy_Option"), (command) =>
            {
                // We can't call SaveAs(false) here, or we'll get an UnauthorizedAccessException when trying to open the file picker.
                DelaySaveAs(TabControlViewModel.SelectedTab, false, false);
            }));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Viewerpage_DocumentModifiedDialog_Corrupted_Dismiss"), (command) =>
            {
            }));
            await MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
        }

        StorageFile SharingHelper_RetrieveSharingStorageFile(ref string errorMessage)
        {
            // If we could run into a state where _CurrentFile was null, we would return null and set the error
            return TemporaryFile;
        }

        private string SharingHelper_RetrieveSharingString()
        {
            if (ToolManager.HasSelectedText)
            {
                return ToolManager.GetSelectedText();
            }
            return null;
        }

        private IAsyncAction CloseDocAsync(PDFDoc doc)
        {
            System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(() =>
            {
                doc.Dispose();
            });
            t.Start();
            return t.AsAsyncAction();
        }

        private void OpenThumbviewerInEditMode(int defaultSelectedPage)
        {
            IsThumbnailsViewOpen = true;
            if (ThumbnailViewer != null)
            {
                ThumbnailViewer.SelectPages(new List<int>() { defaultSelectedPage });
            }
        }

        #endregion Utility Functions
    }
}
