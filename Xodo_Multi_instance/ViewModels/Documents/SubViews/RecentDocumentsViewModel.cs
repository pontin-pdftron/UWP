using CompleteReader.Collections;
using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.FileOpening;
using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CompleteReader.ViewModels.Document.SubViews
{
    public class RecentDocumentsViewModel : ViewModelBase, INavigable
    {
        private static RecentDocumentsViewModel _Current;
        public RecentDocumentsViewModel()
        {
            _Current = this;
            Init();
        }

        /// <summary>
        /// The current instance of the RecentDocumentsViewModel
        /// </summary>
        public static RecentDocumentsViewModel Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new RecentDocumentsViewModel();
                }
                return _Current;
            }
        }

        // Need this to satisfy INavigable, but don't need to use it.
        public event NewINavigableAvailableDelegate NewINavigableAvailable;

        public void Activate(object parameter)
        {
            _SharingHelper = Utilities.SharingHelper.GetSharingHelper();
            _SharingHelper.RetrieveSharingRecentItems += SharingHelper_RetrieveSharingRecentItems;

            _IsSelectedView = true;
            IsDeleteButtonVisible = false;
            IsSelectButtonVisible = true;

            App.Current.ActiveRecentDocumentsViewModel = this;
        }

        public void Deactivate(object parameter)
        {
            if (_SharingHelper != null)
            {
                _SharingHelper.RetrieveSharingRecentItems -= SharingHelper_RetrieveSharingRecentItems;
            }

            SystemNavigationManager.GetForCurrentView().BackRequested -= BackButtonHandler_BackPressed;

            _IsSelectedView = false;
            ExitSelectionMode();
            IsDeleteButtonVisible = false;
            IsSelectButtonVisible = false;

            IsModal = false;

            StopRestoringThumbnails();

            App.Current.ActiveRecentDocumentsViewModel = null;
        }

        private bool _IsActive = true;
        public void StopAllActivity()
        {
            _IsActive = false;
            DocumentPreviewCache.CancelAllRequests();
        }

        public void StartActivity()
        {
            _IsActive = true;
            _RestoringIndex--;
            if (_RestoringIndex < 0)
            {
                _RestoringIndex = 0;
            }
            if (_IsRestoringThumbnails)
            {
                RestoreNextThumbnail();
            }
        }

        private async void Init()
        {
            try
            {
                InitCommands();
                UpdateUI();

                if (!RecentItemsData.IsReady)
                {
                    CompleteReader.Utilities.AnalyticsHandler.CURRENT.SendEvent(CompleteReader.Utilities.AnalyticsHandler.Category.FILEBROWSER,
                        "RecentDocumentsViewewModel initialized without RecentItemsData ready");
                }
                else
                {
                    await RecentItemsData.Instance.UpdateThumbnailLocations();
                }
                if (RecentItemsData.IsReady)
                {
                    _RecentItemsData = RecentItemsData.Instance;
                }
                else
                {
                    _RecentItemsData = await RecentItemsData.GetItemSourceAsync();
                }

                foreach (var item in RecentItems)
                {
                    item.IsSelected = false;
                }

                RaisePropertyChanged("RecentItems");
                RaisePropertyChanged("HasItems");

                RestoreMissingThumbnails();

                SystemNavigationManager.GetForCurrentView().BackRequested += BackButtonHandler_BackPressed;

                // Hack because on Surface Pro 3, the first item's thumbnail doesn't show up for some reason.
                if (_RecentItemsData != null && _RecentItemsData.RecentFiles != null && _RecentItemsData.RecentFiles.Count > 0)
                {
                    RecentItem item = _RecentItemsData.RecentFiles[0];
                    item.IsSelected = false;
                    string thumbLoc = item.ThumbnailLocation;
                    item.ThumbnailLocation = "";
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        item.ThumbnailLocation = thumbLoc;
                    });
                }
            }
            catch (Exception)
            { }
        }

        public RecentItemsData _RecentItemsData;

        public ObservableCollection<RecentItem> RecentItems
        {
            get 
            {
                if (_RecentItemsData != null)
                {
                    return _RecentItemsData.RecentFiles;
                }
                return null;
            }
        }

        // Stores a reference to the current streak of characters that the user has typed for quick navigation purposes 
        private StringBuilder _CurrentTypedName = new StringBuilder();

        private RecentItem _MatchedRecentItem;

        private Stopwatch _Stopwatch = new Stopwatch();

        private long _LastTypedTime = 0;

        private const int RESET_STREAK_TIME = 1000;

        #region Events

        public delegate void RecentFileSelectedDelegate(RecentDocumentProperties fileProperties);

        public event RecentFileSelectedDelegate RecentFileSelected;

        public event EventHandler SelectedFileInfoFetched;

        public delegate void UserKeyboardTypingHandler(RecentItem item);

        public event UserKeyboardTypingHandler UserKeyboardTyping;

        public delegate void IconViewChangedHandler(IconView status);

        public event IconViewChangedHandler IconViewChanged;

        #endregion Events


        #region Sharing

        Utilities.SharingHelper _SharingHelper;

        public IList<RecentItem> SharingHelper_RetrieveSharingRecentItems(ref string errorMessage)
        {
            if (RightClickedItem != null)
            {
                return new List<RecentItem> { RightClickedItem };
            }

            if (_SelectedItems.Count == 0)
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                errorMessage = loader.GetString("DocumentsPage_SharingFailed_DocumentsNeeded");
                return null;
            }
            List<RecentItem> selectedList = new List<RecentItem>();
            foreach (RecentItem selectedItem in _SelectedItems)
            {
                selectedList.Add(selectedItem);
            }
            return selectedList;
        }

        #endregion Sharing


        #region Commands

        private void InitCommands()
        {
            GridTappedCommand = new RelayCommand(GridTappedCommandImpl);
            RecentItemClickCommand = new RelayCommand(RecentItemClickCommandImpl);
            RecentItemsSelectionChangedCommand = new RelayCommand(RecentItemsSelectionChangedCommandImpl);
            DeleteSelectedItemsCommand = new RelayCommand(DeleteSelectedItemsCommandImpl);
            DeleteItemCommand = new RelayCommand(DeleteItemCommandImpl);
            DeleteAllCommand = new RelayCommand(DeleteAllCommandImpl);
            BrowseFilesCommand = new RelayCommand(BrowseFilesCommandImpl);

            IconViewButtonCommand = new RelayCommand(IconViewButtonCommandImpl);

            SelectionButtonCommand = new RelayCommand(SelectionButtonCommandImpl);
            CancelSelectionButtonCommand = new RelayCommand(CancelSelectionButtonCommandImpl);
            ShareCommand = new RelayCommand(ShareCommandImpl);
            FileInfoCommand = new RelayCommand(FileInfoCommandImpl);
        }
        public RelayCommand GridTappedCommand { get; private set; }
        public RelayCommand RecentItemClickCommand { get; private set; }
        public RelayCommand RecentItemsSelectionChangedCommand { get; private set; }
        public RelayCommand DeleteSelectedItemsCommand { get; private set; }
        public RelayCommand DeleteItemCommand { get; private set; }
        public RelayCommand DeleteAllCommand { get; private set; }
        public RelayCommand BrowseFilesCommand { get; private set; }
        public RelayCommand FileInfoCommand { get; private set; }
        public RelayCommand IconViewButtonCommand { get; private set; }

        private void GridTappedCommandImpl(object param)
        {
            if (_MatchedRecentItem != null)
            {
                _MatchedRecentItem.IsSelected = false;
            }
        }

        private void RecentItemClickCommandImpl(object clickedItem)
        {
            RecentItem item = clickedItem as RecentItem;
            if (item != null)
            {
                RecentItemClicked(item);
            }
        }

        private void RecentItemsSelectionChangedCommandImpl(object changeArgs)
        {
            SelectionChangedEventArgs args = changeArgs as SelectionChangedEventArgs;
            if (args != null)
            {
                ResolveSelection(args);
            }
        }

        private void DeleteSelectedItemsCommandImpl(object sender)
        {
            DeleteSelectedItems();
        }

        private void DeleteItemCommandImpl(object sender)
        {
            DeleteItem(sender as RecentItem);
        }

        private void DeleteAllCommandImpl(object sender)
        {
            DeleteAllItems();
            ExitSelectionMode();
        }

        private void BrowseFilesCommandImpl(object sender) {
            DocumentViewModel.Current.BrowseFilesCommand.Execute(null);
        }

        private void IconViewButtonCommandImpl(object parameter)
        {
            ResolveIconView(parameter.ToString());  
        }

        private void ResolveIconView(string param)
        {
            if (param.Equals("d", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.Default)
            {
                CurrentIconView = IconView.Default;
            }
            else if (param.Equals("s", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.Small)
            {
                CurrentIconView = IconView.Small;
            }
            else if (param.Equals("l", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.List)
            {
                CurrentIconView = IconView.List;
            }
            else if (param.Equals("c", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.Cover)
            {
                CurrentIconView = IconView.Cover;
                // Need to fetch higher resolution thumbnails
                RefreshThumbnails();
            }
        }

        #endregion Commands


        #region Visual Properties

        public enum ItemsStatus {
            NotLoaded,
            NoItems,
            HasItems
        }
        public enum IconView
        {
            Default,
            Small,
            List,
            Cover,
        }

        private bool _IsAppBarOpen = false;
        public bool IsAppBarOpen
        {
            get { return _IsAppBarOpen; }
            set
            {
                Set(ref _IsAppBarOpen, value);
            }
        }

        public bool HasSelection
        {
            get { return _SelectedItems.Count > 0; }
        }

        public bool IsOneItemSelected
        {
            get { return _SelectedItems.Count == 1;}
        }

        public ItemsStatus HasItems
        {
            get 
            {
                if (!RecentItemsData.IsReady || RecentItems == null)
                    return ItemsStatus.NotLoaded;
                else 
                    return RecentItems.Count > 0 ? ItemsStatus.HasItems : ItemsStatus.NoItems;
            }
        }

        private SelectedFileInfo _SelectedFileInfo;
        public SelectedFileInfo SelectedFileInfo
        {
            get { return _SelectedFileInfo; }
            set { Set(ref _SelectedFileInfo, value); }
        }

        private bool _IsModal = false;
        public bool IsModal
        {
            get { return _IsModal; }
            set { Set(ref _IsModal, value); }
        }

        private IconView _CurrentIconView = IconView.Default;

        public IconView CurrentIconView
        {
            get { return _CurrentIconView; }
            set
            {
                if (Set(ref _CurrentIconView, value))
                {
                    Settings.Settings.RecentIconView = (int)_CurrentIconView;
                    IconViewChanged?.Invoke(_CurrentIconView);
                    RaisePropertyChanged("CurrVisibleIconView");
                }
            }
        }

        public bool IsIconViewVisible
        {
            get
            {
                return SelectionMode == ListViewSelectionMode.None && !Constants.IsPhoneWidth();
            }
        }

        // Used incase user right clicks an item to share
        private RecentItem _RightClickedItem;

        public RecentItem RightClickedItem
        {
            get { return _RightClickedItem; }
            set { Set(ref _RightClickedItem, value); }
        }

        public void UpdateUI()
        {
            CurrentIconView = Constants.IsPhoneWidth() ? IconView.List : (IconView)Settings.Settings.RecentIconView;
            RaisePropertyChanged("IsIconViewVisible");
            RaisePropertyChanged("CurrentIconView");
        }

        #endregion Visual Properties


        #region Impl

        private bool _RecentItemClicked = false;
        private async void RecentItemClicked(RecentItem recent)
        {
            if (_RecentItemClicked)
            {
                return;
            }

            _RecentItemClicked = true;
            
            if (recent != null)
            {
                RecentDocumentProperties recentProperties = null;
                try
                {
                    recentProperties = await _RecentItemsData.GetRecentFileAsync(recent);

                }
                catch (Exception)
                {
                    // message
                }

                if (recentProperties != null)
                {
                    if (RecentFileSelected != null)
                    {
                        RecentFileSelected(recentProperties);
                    }
                }
                else
                {
                    Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    Windows.UI.Popups.MessageDialog md = new Windows.UI.Popups.MessageDialog(loader.GetString("DocumentsPage_RecentItems_FileNoLongerThere_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                    _RecentItemsData.RemoveItems(new List<RecentItem>() { recent });
                }
            }
            _RecentItemClicked = false;
        }

        private List<RecentItem> _SelectedItems = new List<RecentItem>();

        private void ResolveSelection(SelectionChangedEventArgs args)
        {
            int oldSelectionCount = _SelectedItems.Count;
            foreach  (RecentItem item in args.RemovedItems)
            {
                System.Diagnostics.Debug.Assert(_SelectedItems.Contains(item));
                if (_SelectedItems.Contains(item))
                {
                    _SelectedItems.Remove(item);
                }
            }
            foreach (RecentItem item in args.AddedItems)
            {
                System.Diagnostics.Debug.Assert(!_SelectedItems.Contains(item));
                if (!_SelectedItems.Contains(item))
                {
                    _SelectedItems.Add(item);
                }
            }

            if (oldSelectionCount == 0 && _SelectedItems.Count > 0)
            {
                //IsAppBarOpen = true;
            }
            if (_SelectedItems.Count == 0 && oldSelectionCount > 0)
            {
                IsAppBarOpen = false;
            }
            UpdateListAndSelectionValues();
            if (!HasSelection)
            {
                ExitSelectionMode();
            }
            UpdateAppBarButtonStatus();
        }

        public void UpdateListAndSelectionValues()
        {
            RaisePropertyChanged("RecentItems");
            RaisePropertyChanged("IsOneItemSelected");
            RaisePropertyChanged("HasSelection");
            RaisePropertyChanged("HasItems");
            RaisePropertyChanged("SelectedRecentItem");
            RaisePropertyChanged("CurrVisibleIconView");
        }

        private void DeleteSelectedItems()
        {
            IList<RecentItem> recentItems = new List<RecentItem>();
            foreach (RecentItem selectedItem in _SelectedItems)
            {
                recentItems.Add(selectedItem);
            }
            _RecentItemsData.RemoveItems(recentItems);
            UpdateListAndSelectionValues();
        }

        private void DeleteItem(RecentItem item)
        {
            _RecentItemsData.RemoveItems(new List<RecentItem> { item });

            UpdateListAndSelectionValues();
        }

        private async void DeleteAllItems()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            Windows.UI.Popups.MessageDialog messageDialog = new Windows.UI.Popups.MessageDialog(loader.GetString("DocumentsPage_RecentItems_ClearDialog_Info"), loader.GetString("DocumentsPage_RecentItems_ClearDialog_Title"));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("DocumentsPage_RecentItems_ClearDialog_Clear_Option"), (command) =>
            {
                _RecentItemsData.ClearFilesList();
                UpdateListAndSelectionValues();
            }));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("DocumentsPage_RecentItems_ClearDialog_Cancel_Option"), (command) =>
            {

            }));
            await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(messageDialog);
        }

        #endregion Impl

        #region Selection

        public RelayCommand SelectionButtonCommand { get; private set; }
        public RelayCommand CancelSelectionButtonCommand { get; private set; }
        public RelayCommand ShareCommand { get; private set; }

        private void SelectionButtonCommandImpl(object param)
        {
            EnterSelectionMode();
        }

        private void CancelSelectionButtonCommandImpl(object param)
        {
            ExitSelectionMode();
        }

        private void ShareCommandImpl(object param)
        {
            if (param as RecentItem != null)
            {
                RightClickedItem = param as RecentItem;
            }
            else
            {
                RightClickedItem = null;
            }
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        /// <summary>
        ///  Fetches the file info for a RecentItem and sends an event to notify the View.
        ///  Files contain 6 parameters: Title, Author, Page Count, Path, Size, and Date Modified.
        /// </summary>
        /// <param name="param"></param>
        public async void FileInfoCommandImpl(object param)
        {
            RecentItem item = null;
            if (IsOneItemSelected)
            {
                item = _SelectedItems[0];
            }
            if (param != null)
            {
                item = param as RecentItem;
            }

            if (item == null)
                return;

            string title = item.DocumentName;
            string path = item.DocumentPath;

            RecentDocumentProperties recentProperties = await _RecentItemsData.GetRecentFileAsync(item);
            if (recentProperties != null)
            {

                PDFDoc doc = new PDFDoc(recentProperties.File as StorageFile);
                PDFDocInfo docInfo = doc.GetDocInfo();

                string author = docInfo.GetAuthor();
                string pageCount = doc.GetPageCount().ToString();

                StorageFile file = recentProperties.File as StorageFile;
                BasicProperties basicProperties = await file.GetBasicPropertiesAsync();

                // Compute file size 
                double fileSize = basicProperties.Size;
                string fileSizeStr = "";

                // Format to MB if greater than 0.1 MB
                if (fileSize > 1000000)
                {
                    fileSize /= 1024f * 1024f;
                    fileSize = Math.Round(fileSize, 2);
                    fileSizeStr = fileSize.ToString() + " MB";
                }
                // Otherwise format to KB
                else
                {
                    fileSize /= 1024f;
                    fileSize = Math.Round(fileSize, 2);
                    fileSizeStr = fileSize.ToString() + " KB";
                }

                string lastModified = basicProperties.DateModified.LocalDateTime.ToString();

                SelectedFileInfo = new SelectedFileInfo(title, author, pageCount, path, fileSizeStr, lastModified);
                SelectedFileInfoFetched(item, null);
            }
            else
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                Windows.UI.Popups.MessageDialog md = new Windows.UI.Popups.MessageDialog(loader.GetString("DocumentsPage_RecentItems_FileNoLongerThere_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                _RecentItemsData.RemoveItems(new List<RecentItem>() { item });
            }
        }

        private ListViewSelectionMode _SelectionMode = ListViewSelectionMode.None;
        public ListViewSelectionMode SelectionMode
        {
            get { return _SelectionMode; }
            set 
            { 
                if (Set(ref _SelectionMode, value))
                {
                    RaisePropertyChanged("IsInSelectionMode");
                    RaisePropertyChanged("IsIconViewVisible");
                }
            }
        }

        public bool IsInSelectionMode
        {
            get { return SelectionMode != ListViewSelectionMode.None; }
        }

        private async void EnterSelectionMode()
        {
            if (_MatchedRecentItem != null)
            {
                _MatchedRecentItem.IsSelected = false;
            }

            SelectionMode = ListViewSelectionMode.Multiple;
            RaisePropertyChanged("CurrVisibleIconView");
            IsSelectButtonVisible = false;
            IsInDerivedAppBarState = true;
            await System.Threading.Tasks.Task.Delay(100); // makes the AppBar buttons appear and disappear nicer
            IsDeleteButtonVisible = true;
        }

        private async void ExitSelectionMode()
        {
            IsDeleteButtonVisible = false;
            SelectionMode = ListViewSelectionMode.None;
            RaisePropertyChanged("CurrVisibleIconView");
            await System.Threading.Tasks.Task.Delay(100); // makes the AppBar buttons appear and disappear nicer
            IsInDerivedAppBarState = false;
            IsSelectButtonVisible = _IsSelectedView;
        }

        #endregion Selection


        #region AppBar

        public event EventHandler AppBarButtonStatusUpdated;
        private bool _IsSelectedView = false;

        private void UpdateAppBarButtonStatus()
        {
            if (AppBarButtonStatusUpdated != null)
            {
                AppBarButtonStatusUpdated(this, new EventArgs());
            }
        }

        private bool _IsInDerivedAppBarState = false;
        public bool IsInDerivedAppBarState
        {
            get { return _IsInDerivedAppBarState; }
            set 
            { 
                if (Set(ref _IsInDerivedAppBarState, value))
                {
                    UpdateAppBarButtonStatus();
                }
            }
        }

        private bool _IsSelectButtonVisible = false;
        public bool IsSelectButtonVisible
        {
            get { return _IsSelectButtonVisible; }
            set {
                if (Set(ref _IsSelectButtonVisible, value))
                {
                    
                }
            }
        }

        private bool _IsDeleteButtonVisible = false;
        public bool IsDeleteButtonVisible
        {
            get { return _IsDeleteButtonVisible; }
            set 
            {
                Set(ref _IsDeleteButtonVisible, value);
            }
        }

        public string CurrVisibleIconView
        {
            get
            {
                switch (CurrentIconView)
                {
                    case IconView.Default:
                        return "";
                    case IconView.Small:
                        return "";
                    case IconView.List:
                        return "";
                    default:
                        return "";
                }
            }
        }

        #endregion AppBar


        #region BackButton
        private void BackButtonHandler_BackPressed(object sender, BackRequestedEventArgs e)
        {
            if (GoBack())
                e.Handled = true;
        }

        public override bool GoBack()
        {
            if (IsInSelectionMode)
            {
                ExitSelectionMode();
                return true;
            }
            return false;
        }

        #endregion BackButton


        #region Restoring Thumbnails

        private bool _IsRestoringThumbnails = false;
        private bool _GenerateUnavailableThumbs = false;
        private int _RestoringIndex = 0;
        private void RestoreMissingThumbnails()
        {
            if (RecentItems == null)
            {
                return;
            }
            _IsRestoringThumbnails = true;
            DocumentPreviewCache.DocumentPreviewCacheResponse += DocumentPreviewCache_DocumentPreviewCacheResponse;
            RestoreNextThumbnail();
        }

        private async void RestoreNextThumbnail()
        {
            while (_RestoringIndex < RecentItems.Count)
            {
                RecentItem item = RecentItems[_RestoringIndex];
                _RestoringIndex++;
                if (item.ThumbnailLocation != null && item.ThumbnailLocation.Equals(RecentDocumentProperties.DEFAULT_THUMB_PATH))
                {
                    try
                    {
                        RecentDocumentProperties props = await _RecentItemsData.GetRecentFileAsync(item);
                        if (props != null && props.File != null)
                        {
                            if (_IsActive)
                            {
                                int length = CurrentIconView == IconView.Cover ? 400 : 200;
                                double scale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                                length = (int)(length * scale);
                                DocumentPreviewCache.GetBitmapWithID(props.FilePath, length, length, item);
                            }
                            return;
                        }
                    }
                    catch (Exception) { }
                }
            }
            
            if (!_GenerateUnavailableThumbs)
            {
                _GenerateUnavailableThumbs = true;
                _RestoringIndex = 0;
                RestoreNextThumbnail();
            }
            else
            {
                StopRestoringThumbnails();
            }
            
        }

        private void RefreshThumbnails()
        {
            _RestoringIndex = 0;
            foreach(RecentItem item in RecentItems)
            {
                item.ThumbnailLocation = null;
            }
            RestoreMissingThumbnails();
        }

        private async void DocumentPreviewCache_DocumentPreviewCacheResponse(DocumentPreviewCachePreviewResult result, string previewPath, object customData)
        {
            RecentItem item = customData as RecentItem;
            if (item == null)
            {
                return;
            }
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    if (result == DocumentPreviewCachePreviewResult.e_not_found && _GenerateUnavailableThumbs)
                    {
                        RecentDocumentProperties props = await _RecentItemsData.GetRecentFileAsync(item);
                        if (props != null && props.File != null)
                        {
                            if (_IsActive)
                            {
                                int length = CurrentIconView == IconView.Cover ? 400 : 200;
                                double scale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                                length = (int)(length * scale);
                                DocumentPreviewCache.CreateBitmapWithID(props.FilePath, props.File as StorageFile, length, length, item);
                            }
                            return;
                        }
                    }
                    else if (result == DocumentPreviewCachePreviewResult.e_success)
                    {
                        item.ThumbnailLocation = previewPath;
                    }
                    else if (result == DocumentPreviewCachePreviewResult.e_security_error)
                    {
                        item.ThumbnailLocation = RecentDocumentProperties.DEFAULT_PROTECTED_THUMB_PATH;
                    }

                    if (RecentItems.Contains(item))
                    {
                        _RestoringIndex = RecentItems.IndexOf(item) + 1;
                    }
                    else
                    {
                        _RestoringIndex = 0;
                    }

                    RestoreNextThumbnail();
                });
            }
            catch (Exception e)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_EVENT_CATEGORY, pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(e));
            }
        }

        private void StopRestoringThumbnails()
        {
            if (_IsRestoringThumbnails)
            {
                DocumentPreviewCache.CancelAllRequests();
                DocumentPreviewCache.DocumentPreviewCacheResponse -= DocumentPreviewCache_DocumentPreviewCacheResponse;
            }
            _IsRestoringThumbnails = false;
        }

        #endregion Restoring Thumbnails


        #region Utilities
        /// <summary>
        /// Will parse a hotkey and attempt to perform an action associated with the hotkey.
        /// Will handle left/right/up/down/enter keys. 
        /// <param name="args"></param>
        /// <returns> Returns true if it handled the hotkey, false otherwise.</returns>
        /// </summary>
        public void ParseHotKeyPress(KeyEventArgs args)
        {
            if (_SelectionMode != ListViewSelectionMode.None || _IsModal)
                return;

            // Handle interaction with a selected item 
            if (_MatchedRecentItem != null)
            {
                if (args.VirtualKey == Windows.System.VirtualKey.Enter)
                {
                    RecentItemClickCommandImpl(_MatchedRecentItem);
                    return;
                }

                if (args.VirtualKey == Windows.System.VirtualKey.Left)
                {
                    int index = RecentItems.IndexOf(_MatchedRecentItem) - 1;
                    if (index < 0)
                        return;

                    _MatchedRecentItem.IsSelected = false;
                    _MatchedRecentItem = RecentItems[index];
                    _MatchedRecentItem.IsSelected = true;
                    UserKeyboardTyping(_MatchedRecentItem);
                    return;
                }

                if (args.VirtualKey == Windows.System.VirtualKey.Right)
                {
                    int index = RecentItems.IndexOf(_MatchedRecentItem) + 1;
                    if (index > RecentItems.Count - 1)
                        return;

                    _MatchedRecentItem.IsSelected = false;
                    _MatchedRecentItem = RecentItems[index];
                    _MatchedRecentItem.IsSelected = true;
                    UserKeyboardTyping(_MatchedRecentItem);
                    return;
                }
            }
            else if (RecentItems != null && RecentItems.Count > 0
                && (args.VirtualKey == Windows.System.VirtualKey.Left
                || args.VirtualKey == Windows.System.VirtualKey.Right
                || args.VirtualKey == Windows.System.VirtualKey.Up
                || args.VirtualKey == Windows.System.VirtualKey.Down))
            {
                _MatchedRecentItem = RecentItems[0];
                _MatchedRecentItem.IsSelected = true;
                UserKeyboardTyping(_MatchedRecentItem);
                return;
            }
        }

        /// <summary>
        /// Find the matched item from a given key press.
        /// This will notify the view via an event. 
        /// </summary>
        /// <param name="key"></param>
        public void ParseKeyPress(string key)
        {
            // Don't allow quickfinding in selection mode
            if (_SelectionMode != ListViewSelectionMode.None || _IsModal)
                return;

            // Update stopwatch calculations
            if (!_Stopwatch.IsRunning)
            {
                _Stopwatch.Start();
            }

            if (_Stopwatch.ElapsedMilliseconds - _LastTypedTime >= RESET_STREAK_TIME)
            {
                _CurrentTypedName.Clear();
            }

            _LastTypedTime = _Stopwatch.ElapsedMilliseconds;

            // Add to current string streak and find next match
            _CurrentTypedName.Append(key);

            string currTypedName = _CurrentTypedName.ToString().ToLower();

            RecentItem matchedItem = RecentItems.Where(x => x.DocumentName.ToLower().StartsWith(currTypedName)).FirstOrDefault();

            if (matchedItem != null )
            {
                if (_MatchedRecentItem != null)
                {
                    _MatchedRecentItem.IsSelected = false;
                }

                _MatchedRecentItem = matchedItem;

                // Notify view to scroll into view of item
                UserKeyboardTyping(matchedItem);

                matchedItem.IsSelected = true;
            }
            // Reset string if no matches (optimization over how Windows File Explorer works, since if no matches, there will never any matches till timer reset)
            else if (matchedItem == null)
            {
                _CurrentTypedName.Clear();
            }
        }

        /// <summary>
        ///  Calculates where the matched item will be after pressing the up/down arrow. 
        ///  The View needs to provide the itemsPerRow param to calculate such. 
        /// </summary>
        /// <param name="itemsPerRow"></param>
        /// <param name="isDown"></param>
        public void MoveMatchedItemByRow(int itemsPerRow, bool isDown)
        {
            // Don't allow quick finding in selection mode
            if (_SelectionMode != ListViewSelectionMode.None || _IsModal || _MatchedRecentItem == null || itemsPerRow <= 0)
                return;

            // check if last element is on the same row as current matched item. Don't allow down arrow to move down if there is no bottom row.
            if (((RecentItems.Count - 1) / itemsPerRow == RecentItems.IndexOf(_MatchedRecentItem) / itemsPerRow) && isDown)
                return;

            int index = isDown ? RecentItems.IndexOf(_MatchedRecentItem) + itemsPerRow : RecentItems.IndexOf(_MatchedRecentItem) - itemsPerRow;

            if (index < 0)
                return;

            if (index > RecentItems.Count - 1)
            {
                index = RecentItems.Count - 1;
            }

            _MatchedRecentItem.IsSelected = false;
            _MatchedRecentItem = RecentItems[index];
            _MatchedRecentItem.IsSelected = true;
            UserKeyboardTyping(_MatchedRecentItem);
        }
        #endregion Utilities
    }
}
