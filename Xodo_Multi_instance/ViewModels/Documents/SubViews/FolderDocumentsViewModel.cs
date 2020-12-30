using CompleteReader.Collections;
using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.FileOpening;
using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace CompleteReader.ViewModels.Document.SubViews
{
    public class FolderDocumentsViewModel : ViewModelBase, INavigable
    {
        private static FolderDocumentsViewModel _Current;
        public FolderDocumentsViewModel()
        {
            _Current = this;
            Init();
        }

        /// <summary>
        /// The current instance of the RecentDocumentsViewModel
        /// </summary>
        public static FolderDocumentsViewModel Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new FolderDocumentsViewModel();
                }
                return _Current;
            }
        }

        private PinnedRootFolders _PinnedRootFolders;

        // Need this to satisfy INavigable, but don't need to use it.
        public event NewINavigableAvailableDelegate NewINavigableAvailable;


        private bool _IsActive = false;
        public void StopAllActivity()
        {
            _IsActive = false;
            DocumentPreviewCache.CancelAllRequests();
        }

        public void StartActivity()
        {
            _IsActive = true;
            if (!_IsLoading)
            {
                if (FilteredPinnedItems != null)
                {
                    FilteredPinnedItems.Pause = false;
                    FilteredPinnedItems.RequestResources();
                }
            }
        }

        public void Activate(object parameter)
        {
            _SharingHelper = Utilities.SharingHelper.GetSharingHelper();
            _SharingHelper.RetrieveSharingRecentItems += SharingHelper_RetrieveSharingRecentItems;

            _IsSelectedView = true;
            IsDeleteButtonVisible = false;
            IsSelectButtonVisible = true;

            App.Current.ActiveFolderDocumentsViewModel = this;

            StartActivity();
        }

        public void Deactivate(object parameter)
        {
            if (_SharingHelper != null)
            {
                _SharingHelper.RetrieveSharingRecentItems -= SharingHelper_RetrieveSharingRecentItems;
            }
            _IsSelectedView = false;
            ExitSelectionMode();
            IsDeleteButtonVisible = false;
            IsSelectButtonVisible = false;

            DocumentPreviewCache.CancelAllRequests();
            DocumentPreviewCache.DocumentPreviewCacheResponse -= DocumentPreviewCache_DocumentPreviewCacheResponse;
            SystemNavigationManager.GetForCurrentView().BackRequested -= BackButtonHandler_BackPressed;

            App.Current.ActiveFolderDocumentsViewModel = null;

            IsModal = false;

            // retain the folder path that the user is in 
            SaveFolderPathToSettings();

            StopAllActivity();
        }

        private async void Init()
        {
            try
            {
                InitCommands();
                UpdateUI();
                LoadFiltersFromSettings();

                _PinnedRootFolders = await PinnedRootFolders.GetItemSourceAsync();
                FilteredPinnedItems = new pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>();
                FilteredPinnedItems.ResetRequestsWhenViewChanges = true;
                FilteredPinnedItems.LimitResourcesToItemsNearScreen = true;

                _PinItem = PinnedItem.CreatePin();

                List<PinnedItem> pinnedItems = _PinnedRootFolders.PinnedItems;
                pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> pinnedRoots =
                        new pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>();
                foreach (PinnedItem item in pinnedItems)
                {
                    pinnedRoots.Add(item);
                }

                _RootFolder = pinnedRoots;
                _FoldersList.Add(pinnedRoots);
                IsPinItemVisible = true;

                FolderPathList = new ObservableCollection<KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>>();
                FolderPathList.Add(new KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>
                    (pinnedRoots, ResourceLoader.GetForCurrentView().GetString("DocumentsPage_FoldersPage_DefaultPathName")));
                RaisePropertyChanged("FolderPathList");

                try
                {
                    CurrentlyPinnedItems = await RestoreFolderLocation(pinnedRoots);
                    // restore last folder path navigation directory 
                }
                catch (Exception)
                {
                    CurrentlyPinnedItems = pinnedRoots;
                }


                _IsLoading = false;
                UpdateItemAvailabilityStatus();

                DocumentPreviewCache.DocumentPreviewCacheResponse += DocumentPreviewCache_DocumentPreviewCacheResponse;

                SystemNavigationManager.GetForCurrentView().BackRequested += BackButtonHandler_BackPressed;
            }
            catch (Exception)
            { }
        }

        #region View Items


        // This will hold onto the collections of folders as user navigates forward 
        private List<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>> _FoldersList = new List<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>>();
        private pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> _RootFolder;

        private PinnedItem _PinItem;
        private bool IsPinItemVisible
        {
            get { return _RootFolder != null && _RootFolder.Contains(_PinItem); }
            set
            {
                if (_RootFolder != null)
                {
                    if (_RootFolder.Contains(_PinItem) && !value)
                    {
                        _RootFolder.Remove(_PinItem);
                    }
                    else if (!_RootFolder.Contains(_PinItem) && value)
                    {
                        _RootFolder.Add(_PinItem);
                    }
                }
            }
        }

        // Collection that maps a ObservableCollection<PinnedItem> to a user friendly path name in UI
        public ObservableCollection<KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>> FolderPathList
        {
            get; private set;
        }



        private pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> _CurrentlyPinnedItems;
        private pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> CurrentlyPinnedItems
        {
            get { return _CurrentlyPinnedItems; }
            set
            {
                pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> oldPinned = _CurrentlyPinnedItems;
                if (Set(ref _CurrentlyPinnedItems, value))
                {
                    if (oldPinned != null)
                    {
                        oldPinned.CollectionChanged -= CurrentlyPinnedItems_CollectionChanged;
                    }
                    UpdateFilteredItems();
                    if (_CurrentlyPinnedItems != null)
                    {
                        _CurrentlyPinnedItems.CollectionChanged += CurrentlyPinnedItems_CollectionChanged;
                    }
                }
            }
        }

        private void CurrentlyPinnedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (object removedItem in e.OldItems)
                {
                    PinnedItem item = removedItem as PinnedItem;
                    if (item != null)
                    {
                        if (FilteredPinnedItems.Contains(item))
                        {
                            FilteredPinnedItems.Remove(item);
                        }
                    }
                }
            }
            else
            {
                FilterItems();
            }
        }

        private pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> _FilteredPinnedItems;
        public pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> FilteredPinnedItems
        {
            get { return _FilteredPinnedItems; }
            set
            {
                if (Set(ref _FilteredPinnedItems, value))
                {
                    RaisePropertyChanged("LoadingItems");
                    FilteredPinnedItems.AllRequestsCancelled += FilteredPinnedItems_AllRequestsCancelled;
                    FilteredPinnedItems.ResourceNeededForIndex += FilteredPinnedItems_ResourceNeededForIndex;
                    FilteredPinnedItems.DiscardResourceForIndex += FilteredPinnedItems_DiscardResourceForIndex;
                }
            }
        }

        private void UpdateFilteredItems()
        {
            FilteredPinnedItems.Clear();
            FilteredPinnedItems.ItemsWithResource.Clear();
            FilterItems();
        }

        private void FilterItems()
        {
            if (CurrentlyPinnedItems != null)
            {
                int filteredIndex = 0;
                foreach (PinnedItem item in CurrentlyPinnedItems)
                {
                    if (ItemBelongsInFilteredList(item))
                    {
                        if (FilteredPinnedItems.Count <= filteredIndex || FilteredPinnedItems[filteredIndex] != item)
                        {
                            FilteredPinnedItems.Insert(filteredIndex, item);
                        }
                        filteredIndex++;
                    }
                    else
                    {
                        if (FilteredPinnedItems.Count > filteredIndex && FilteredPinnedItems[filteredIndex] == item)
                        {
                            FilteredPinnedItems.RemoveAt(filteredIndex);
                        }
                    }
                }
            }
            UpdateItemAvailabilityStatus();
        }

        private bool ItemBelongsInFilteredList(PinnedItem item)
        {
            if (item.PinnedItemType != PinnedItem.PinnedType.File)
            {
                return true;
            }
            string ext = item.DocumentExtension;
            if (Settings.Settings.FILTER_PDFFileTypes.Contains(ext) && IncludePDFs)
            {
                return true;
            }
            if (Settings.Settings.FILTER_OffcieFileTypes.Contains(ext) && IncludeOfficeDocumentss)
            {
                return true;
            }
            if (Settings.Settings.FILTER_ImageFileTypes.Contains(ext) && IncludeImages)
            {
                return true;
            }
            return false;
        }

        public bool LoadingItems
        {
            get
            {
                return _FilteredPinnedItems == null;
            }
        }

        private PinnedItem _CurrentPinItem;

        public PinnedItem CurrentPinItem
        {
            get { return _CurrentPinItem; }
        }

        public PinnedItem PinItem
        {
            get { return _PinItem; }
        }

        #endregion View Items


        #region ScrollViewer Thumbnails

        private bool _IsNavigating = false;
        public bool IsNavigating
        {
            get { return _IsNavigating; }
            set
            {
                if (Set(ref _IsNavigating, value))
                {
                    FilteredPinnedItems.Pause = IsNavigating;
                    if (IsNavigating)
                    {
                        foreach (PinnedInfo info in FilteredPinnedItems.ItemsWithResource.Values)
                        {
                            info.Item.ThumbnailLocation = null;
                            info.Item.ShowNameInCoverMode = true;
                            info.Item.ThumbLoaded = false;
                        }

                        DocumentPreviewCache.CancelAllRequests();
                        _BadThumbnails.Clear();
                    }
                    else
                    {
                        FilteredPinnedItems.Refresh();
                    }
                }
            }
        }

        private int _FolderPathSequenceNumber = 0; // updates each time you navigate to a new folder. Updates every time we go to new fonder to ensure that thumb belongs in current list.
        private class ThumbnailRequestBundle // used for thumbnail requests
        {
            public int Index;
            public int SequenceNumber;
            public ThumbnailRequestBundle(int index, int sequence)
            {
                Index = index;
                SequenceNumber = sequence;
            }
        }

        // Maps the number of failures of a PinnedItem when retrieving its thumbnails 
        private Dictionary<PinnedItem, int> _BadThumbnails = new Dictionary<PinnedItem, int>();
        private const int MAX_RETRIES = 3;

        public ScrollViewer _CurrentScrollViewer;
        private ListViewBase _CurrentListViewBase;
        public ListViewBase CurrentListViewBase
        {
            get { return _CurrentListViewBase; }
            set
            {
                if (_CurrentListViewBase != value)
                {
                    _CurrentListViewBase = value;
                }
            }
        }

        #endregion ScrollViewer Thumbnails


        #region Typing Navigation
        // Stores a reference to the current streak of characters that the user has typed for quick navigation purposes 
        private StringBuilder _CurrentTypedName = new StringBuilder();

        private PinnedItem _MatchedPinnedItem;

        private System.Diagnostics.Stopwatch _Stopwatch = new System.Diagnostics.Stopwatch();

        private long _LastTypedTime = 0;

        private const int RESET_STREAK_TIME = 1000;

        #endregion Typing Navigation


        #region Events

        public delegate void FolderDocumentSelectedDelegate(StorageFile file);

        public event FolderDocumentSelectedDelegate FolderDocumentSelected;

        public event EventHandler SelectedFileInfoFetched;

        public delegate void UserKeyboardTypingHandler(PinnedItem item);

        public event UserKeyboardTypingHandler UserKeyboardTyping;

        public delegate void IconViewChangedHandler(IconView status);

        public event IconViewChangedHandler IconViewChanged;

        public delegate void ThumbnailReceivedHandler(PinnedItem item, int index);
        public event ThumbnailReceivedHandler ThumbnailReceived;

#endregion Events


#region Sharing

        Utilities.SharingHelper _SharingHelper;

        public IList<RecentItem> SharingHelper_RetrieveSharingRecentItems(ref string errorMessage)
        {
            if (RightClickedItem != null)
            {
                RecentItem item = new RecentItem(RightClickedItem.DocumentName, "", RightClickedItem.Token, RightClickedItem.DocumentPath);
                item.Properties.File = RightClickedItem.File;
                return new List<RecentItem> { item };
            }

            if (_SelectedItems.Count == 0)
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                errorMessage = loader.GetString("DocumentsPage_SharingFailed_DocumentsNeeded");
                return null;
            }
            List<RecentItem> selectedList = new List<RecentItem>();
            foreach (PinnedItem selectedItem in _SelectedItems)
            {
                RecentItem item = new RecentItem(selectedItem.DocumentName, "", selectedItem.Token, selectedItem.DocumentPath);
                item.Properties.File = selectedItem.File;
                selectedList.Add(item);
            }
            return selectedList;
        }

#endregion Sharing


#region Commands

        private void InitCommands()
        {
            GridTappedCommand = new RelayCommand(GridTappedCommandImpl);
            FolderItemClickCommand = new RelayCommand(FolderItemClickCommandImpl);
            FolderPathClickCommand = new RelayCommand(FolderPathClickCommandImpl);
            FolderItemsSelectionChangedCommand = new RelayCommand(FolderItemsSelectionChangedCommandImpl);
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
        public RelayCommand FolderItemClickCommand { get; private set; }
        public RelayCommand FolderPathClickCommand { get; private set; }
        public RelayCommand FolderItemsSelectionChangedCommand { get; private set; }
        public RelayCommand DeleteSelectedItemsCommand { get; private set; }
        public RelayCommand DeleteItemCommand { get; private set; }
        public RelayCommand DeleteAllCommand { get; private set; }
        public RelayCommand BrowseFilesCommand { get; private set; }
        public RelayCommand FileInfoCommand { get; private set; }
        public RelayCommand IconViewButtonCommand { get; private set; }


        // Tapping on the main grid will deselect any previously matched item
        private void GridTappedCommandImpl(object param)
        {
            if (_MatchedPinnedItem != null)
            {
                _MatchedPinnedItem.IsSelected = false;
                _MatchedPinnedItem = null;
            }
        }

        private void FolderItemClickCommandImpl(object clickedItem)
        {
            PinnedItem item = clickedItem as PinnedItem;
            if (item != null)
            {
                if (_MatchedPinnedItem != null)
                {
                    _MatchedPinnedItem.IsSelected = false;
                    _MatchedPinnedItem = null;
                }
                // Handle pin folder 
                if ((item.PinnedItemType == PinnedItem.PinnedType.Pin))
                {
                    PinClicked();
                }
                // Handle folder
                else if (item.PinnedItemType == PinnedItem.PinnedType.Folder)
                {
                    FolderClicked(item);
                }
                // Handle file
                else
                {
                    PinnedItemClicked(item);
                }
            }
        }

        private void FolderPathClickCommandImpl(object clickedItem)
        {
            if (FilteredPinnedItems == null)
            {
                return;
            }
            var item = (KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>)clickedItem;
            pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> pinnedItems = item.Key;
            int index = _FoldersList.IndexOf(pinnedItems);
            if (index == FolderPathList.Count - 1)
                return;

            int times = _FoldersList.Count - 1 - index;
            _FolderPathSequenceNumber++;
            GoBackInFolder(times);
        }

        private void FolderItemsSelectionChangedCommandImpl(object changeArgs)
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
            DeleteItem(sender as PinnedItem);
        }

        private void DeleteAllCommandImpl(object sender)
        {
            DeleteAllItems();
            ExitSelectionMode();
        }

        private void BrowseFilesCommandImpl(object sender)
        {
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
            }
        }

#endregion Commands


#region Visual Properties

        public enum ItemsStatus
        {
            NotLoaded,
            NoItems,
            HasItems,
            HasNoSubItems,
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
            get { return _SelectedItems.Count == 1; }
        }

        public bool IsDeleteEnabled
        {
            get { return _FoldersList.Count == 1 && HasSelection; }
        }

        public bool IsRootPath
        {
            get { return _FoldersList.Count == 1; }
        }

        public bool IsDeleteAllEnabled
        {
            get { return _FoldersList.Count == 1 && HasItems == ItemsStatus.HasItems; }
        }

        // Only allow share to be enabled if there are selected items and none of them are folders
        public bool IsShareEnabled
        {
            get { return _SelectedItems.Count > 0 && _SelectedItems.Where(x => (x.PinnedItemType == PinnedItem.PinnedType.Folder)).Count() == 0; }
        }

        private void UpdateItemAvailabilityStatus()
        {
            RaisePropertyChanged("HasItems");
            RaisePropertyChanged("HasFilteredItems");
            RaisePropertyChanged("HasOnlyUnfilteredItems");
        }

        private bool _IsLoading = true;
        public ItemsStatus HasItems
        {
            get
            {
                if (_IsLoading || CurrentlyPinnedItems == null)
                {
                    return ItemsStatus.NotLoaded;
                }
                else if (_FoldersList.Count == 1)
                {
                    return CurrentlyPinnedItems.Count > 0 ? ItemsStatus.HasItems : ItemsStatus.NoItems;
                }
                else
                {
                    return CurrentlyPinnedItems.Count > 0 ? ItemsStatus.HasItems : ItemsStatus.HasNoSubItems;
                }
            }
        }

        public bool HasFilteredItems
        {
            get
            {
                if (_IsLoading || FilteredPinnedItems == null)
                {
                    return false;
                }

                return FilteredPinnedItems.Count > 0;
            }
        }

        public bool HasOnlyUnfilteredItems
        {
            get { return HasItems == ItemsStatus.HasItems && !HasFilteredItems; }
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
                    Settings.Settings.FolderIconView = (int)_CurrentIconView;
                    IconViewChanged?.Invoke(_CurrentIconView);
                    RaisePropertyChanged("CurrVisibleIconView");
                }
            }
        }

        // Used in case user right clicks an item to share
        private PinnedItem _RightClickedItem;

        public PinnedItem RightClickedItem
        {
            get { return _RightClickedItem; }
            set { Set(ref _RightClickedItem, value); }
        }

        public int ThumbnailSideLength
        {
            get
            {
                int length = CurrentIconView == IconView.Cover ? 400 : 200;
                // There's some problem when the resulting jpeg is generated that makes it look blurry when blown up.
                // Adding an arbitrary 1.5 gives us a nice result (determined through experiment)
                double scale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel * 1.5;
                length = (int)(length * scale);
                return length;
            }
        }

#endregion Visual Properties


#region Impl

        private bool _PinnedItemClicked = false;

        private bool _FilePickerOpen = false;
        private async void PinClicked()
        {
            if (_FilePickerOpen)
            {
                return;
            }
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.ViewMode = PickerViewMode.List;
            StorageFolder folder = null;
            _FilePickerOpen = true;
            foreach (string fileType in Settings.Settings.AssociatedFileTypes)
            {
                folderPicker.FileTypeFilter.Add(fileType);
            }
            try
            {
                // apparently, this sometimes throws a System.Exception "Element not found" for no apparent reason. We want to catch that.
                folder = await folderPicker.PickSingleFolderAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                _FilePickerOpen = false;
            }

            if (folder != null)
            {
                PinnedItem item = _PinnedRootFolders.AddPinnedFolder(folder);
                if (item == null)
                {
                    // it's already pinned
                    MessageDialog md = new MessageDialog(ResourceLoader.GetForCurrentView().GetString("DocumentsPage_FoldersPage_SameFolderMessage"));
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                    return;
                }

                if (IsPinItemVisible)
                {
                    _RootFolder.Insert(_RootFolder.Count - 1, item);
                }
                else
                {
                    _RootFolder.Add(item);
                }
                // Pinning a folder via browse should send user back to root level 
                int times = _FoldersList.Count - 1;
                GoBackInFolder(times);
            }
        }

        private async void FolderClicked(PinnedItem item)
        {
            if (_PinnedItemClicked)
            {
                return;
            }

            _PinnedItemClicked = true;
            IsNavigating = true;

            try
            {
                if (item != null)
                {
                    // get associated storagefile/folder if it does not exist
                    if (item.File == null)
                    {
                        item.File = await _PinnedRootFolders.GetFolderFromPinnedItemAsync(item);
                    }
                    if (item.File == null)
                    {
                        ResourceLoader loader = ResourceLoader.GetForCurrentView();
                        Windows.UI.Popups.MessageDialog md = new Windows.UI.Popups.MessageDialog(loader.GetString("DocumentsPage_RecentItems_FileNoLongerThere_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                        await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        _PinnedRootFolders.DeletePinnedFolder(item);
                        _RootFolder.Remove(item);
                        return;
                    }

                    DocumentPreviewCache.DocumentPreviewCacheResponse -= DocumentPreviewCache_DocumentPreviewCacheResponse;


                    _FolderPathSequenceNumber++;
                    CurrentlyPinnedItems = null;

                    // If navigating from root folder, we know we're interacting with a pinned item
                    if (_FoldersList.Count == 1)
                    {
                        _CurrentPinItem = item;
                    }
                    FolderPathList.Add(new KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>(null, item.DocumentName));

                    pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> newFolder = await GetStorageItemsFromFolder(item.File as StorageFolder);

                    _FoldersList.Add(newFolder);

                    if (FolderPathList[FolderPathList.Count - 1].Key == null)
                    {
                        FolderPathList[FolderPathList.Count - 1] = new KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>(newFolder, item.DocumentName);
                    }
                    else
                    {
                        FolderPathList.Add(new KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>(newFolder, item.DocumentName));
                    }

                    CurrentlyPinnedItems = newFolder;
                    SaveFolderPathToSettings();

                    UpdateItemAvailabilityStatus();
                    RaisePropertyChanged("IsDeleteEnabled");
                    RaisePropertyChanged("IsDeleteAllEnabled");

                    await Task.Delay(100);
                    DocumentPreviewCache.DocumentPreviewCacheResponse += DocumentPreviewCache_DocumentPreviewCacheResponse;
                    IsNavigating = false;
                }
            }
            catch (Exception)
            {
                // message
            }
            finally
            {
                _PinnedItemClicked = false;
                IsNavigating = false;
                FilteredPinnedItems.RequestResources();
            }

        }

        private async void PinnedItemClicked(PinnedItem pinned)
        {
            if (_PinnedItemClicked)
            {
                return;
            }

            _PinnedItemClicked = true;

            try
            {
                if (pinned != null)
                {
                    RecentDocumentProperties recentProperties = null;

                    try
                    {
                        using (Windows.Storage.Streams.IRandomAccessStream iras = await (pinned.File as StorageFile).OpenReadAsync())
                        {
                            recentProperties = new RecentDocumentProperties();
                            recentProperties.File = pinned.File; // we do this here to ensure compiler doesn't optimize this out...
                        }
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        // This means that the file was deleted, so we want recentProperties to be null so that we can delete it from our list
                    }
                    catch (Exception)
                    {
                        // all other errors, set this value. Opening will fail and handle it.
                        recentProperties = new RecentDocumentProperties();
                        recentProperties.File = pinned.File; // we do this here to ensure compiler doesn't optimize this out...
                    }

                    if (recentProperties != null)
                    {
                        if (FolderDocumentSelected != null)
                        {
                            DocumentPreviewCache.CancelAllRequests();
                            FolderDocumentSelected(recentProperties.File as StorageFile);
                        }
                    }
                    else
                    {
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        Windows.UI.Popups.MessageDialog md = new Windows.UI.Popups.MessageDialog(loader.GetString("DocumentsPage_RecentItems_FileNoLongerThere_Info"), loader.GetString("DocumentsPage_FileOpeningError_Title"));
                        await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                        CurrentlyPinnedItems.Remove(pinned);
                        FilteredPinnedItems.Remove(pinned);
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                _PinnedItemClicked = false;
            }
        }

        private List<PinnedItem> _SelectedItems = new List<PinnedItem>();

        private void ResolveSelection(SelectionChangedEventArgs args)
        {
            int oldSelectionCount = _SelectedItems.Count;
            foreach (PinnedItem item in args.RemovedItems)
            {
                if (_SelectedItems.Contains(item))
                {
                    _SelectedItems.Remove(item);
                }
            }
            foreach (PinnedItem item in args.AddedItems)
            {
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

        public void RefreshRootFolder()
        {

        }

        public void UpdateListAndSelectionValues()
        {
            RaisePropertyChanged("PinnedItems");
            RaisePropertyChanged("IsOneItemSelected");
            RaisePropertyChanged("HasSelection");
            UpdateItemAvailabilityStatus();
            RaisePropertyChanged("SelectedPinnedItem");
            RaisePropertyChanged("IsDeleteEnabled");
            RaisePropertyChanged("IsShareEnabled");
            RaisePropertyChanged("CurrVisibleIconView");
        }

        private void DeleteSelectedItems()
        {
            // Can only remove from root path (pinned folders, not subfolders of them)
            if (_FoldersList.Count != 1)
                return;

            IList<PinnedItem> pinnedItems = new List<PinnedItem>();

            // Add items to be removed
            foreach (PinnedItem item in _SelectedItems)
            {
                pinnedItems.Add(item);
            }
            // Remove items UI wise
            foreach (PinnedItem item in pinnedItems)
            {
                _PinnedRootFolders.DeletePinnedFolder(item);
                _RootFolder.Remove(item);
            }

            IsPinItemVisible = true;
            UpdateListAndSelectionValues();
        }

        private void DeleteItem(PinnedItem item)
        {
            if (_FoldersList.Count != 1)
                return;

            _PinnedRootFolders.DeletePinnedFolder(item);
            _RootFolder.Remove(item);
            IsPinItemVisible = true;

            UpdateListAndSelectionValues();
        }

        private async void DeleteAllItems()
        {
            if (_FoldersList.Count != 1)
                return;

            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            Windows.UI.Popups.MessageDialog messageDialog = new Windows.UI.Popups.MessageDialog(loader.GetString("DocumentsPage_FoldersPage_ClearDialog_Info"), loader.GetString("DocumentsPage_RecentItems_ClearDialog_Title"));
            messageDialog.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("DocumentsPage_RecentItems_ClearDialog_Clear_Option"), (command) =>
            {
                _PinnedRootFolders.ClearAllPinnedFolders();
                int itemsToKeep = 0;
                if (IsPinItemVisible)
                {
                    itemsToKeep = 1;
                }
                while (_RootFolder.Count > itemsToKeep)
                {
                    _RootFolder.RemoveAt(0);
                }
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
            if (param as PinnedItem != null)
            {
                RightClickedItem = param as PinnedItem;
            }
            else
            {
                RightClickedItem = null;
            }
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        /// <summary>
        ///  Fetches the file info for a FolderItem and sends an event to notify the View 
        ///  Folders contain 4 parameters: Path, number of Folders, number of PDFs, and Date Modified
        ///  Files contain 6 parameters: Title, Author, Page Count, Path, Size, and Date Modified
        /// </summary>
        /// <param name="param"></param>
        private async void FileInfoCommandImpl(object param)
        {
            PinnedItem item = null;
            if (IsOneItemSelected)
            {
                item = _SelectedItems[0];
            }
            if (param != null)
            {
                item = param as PinnedItem;
            }

            if (item == null)
                return;

            string title = item.DocumentName;
            string path = item.DocumentPath;
            string author = "";
            string pageCount = "";

            bool isFile = item.PinnedItemType == PinnedItem.PinnedType.File;
            IStorageItem file = null;

            // Handle file retrieval differently depending it is a file or folder
            if (isFile)
            {
                // If it is a file in the pinned folders section, we know we already fetched the storage file for it
                file = item.File;
                PDFDoc doc = new PDFDoc(file as StorageFile);
                PDFDocInfo docInfo = doc.GetDocInfo();

                author = docInfo.GetAuthor();
                pageCount = doc.GetPageCount().ToString();
            }
            else
            {
                // Otherwise if it is a folder, we need to retrieve the folder via the MRU list
                if (item.File == null)
                {
                    item.File = await _PinnedRootFolders.GetFolderFromPinnedItemAsync(item);
                }

                file = item.File;
            }

            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();

            string fileSizeStr = "";
            int numFolders = 0;
            int numPDFs = 0;

            // Get either file size or numFolders, numPDFs if it is a file vs folder
            if (isFile)
            {
                // Compute file size 
                double fileSize = basicProperties.Size;
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
            }
            else
            {
                var subFolders = await (file as StorageFolder).GetFoldersAsync();
                numFolders = subFolders.Count;

                var subFiles = await (file as StorageFolder).GetFilesAsync();
                numPDFs = subFiles.Where(x => x.FileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase)).Count();
            }

            // Get date modified for both file and folder
            string lastModified = basicProperties.DateModified.LocalDateTime.ToString();

            if (isFile)
            {
                SelectedFileInfo = new SelectedFileInfo(title, author, pageCount, path, fileSizeStr, lastModified);
            }
            else
            {
                SelectedFileInfo = new SelectedFileInfo(path, numFolders, numPDFs, lastModified);
            }

            // Raise the event that a selected file has been fetched for the FoldersPage code behind to know and programatically set values
            SelectedFileInfoFetched?.Invoke(item, null);
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
            if (_MatchedPinnedItem != null)
            {
                _MatchedPinnedItem.IsSelected = false;
            }

            SelectionMode = ListViewSelectionMode.Multiple;
            RaisePropertyChanged("CurrVisibleIconView");
            IsSelectButtonVisible = false;
            IsInDerivedAppBarState = true;
            await System.Threading.Tasks.Task.Delay(100); // makes the AppBar buttons appear and disappear nicer
            IsDeleteButtonVisible = true;
            IsPinItemVisible = false;
        }

        private async void ExitSelectionMode()
        {
            IsDeleteButtonVisible = false;
            SelectionMode = ListViewSelectionMode.None;
            RaisePropertyChanged("CurrVisibleIconView");
            await System.Threading.Tasks.Task.Delay(100); // makes the AppBar buttons appear and disappear nicer
            IsInDerivedAppBarState = false;
            IsSelectButtonVisible = _IsSelectedView;
            IsPinItemVisible = true;
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
            set
            {
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

        public bool IsIconViewVisible
        {
            get
            {
                return SelectionMode == ListViewSelectionMode.None && !Constants.IsPhoneWidth();
            }
        }

        public void UpdateUI()
        {
            CurrentIconView = Constants.IsPhoneWidth() ? IconView.List : (IconView)Settings.Settings.FolderIconView;
            RaisePropertyChanged("CurrentIconView");
            RaisePropertyChanged("IsIconViewVisible");
        }

#endregion AppBar


#region BackButton
        private void BackButtonHandler_BackPressed(object sender, BackRequestedEventArgs e)
        {
            if (GoBack())
            {
                e.Handled = true;
            }
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


#region Folder Depth Navigation

        private async Task<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>>
            RestoreFolderLocation(pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> root)
        {
            List<string> folderPathList = Settings.Settings.FolderPathList.Split('*').ToList();
            pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> currFolder = root;
            for (int i = 0; i < folderPathList.Count; i++)
            {
                string nextFolderName = folderPathList[i];

                // Get the next folder according to our saved path directories
                PinnedItem nextFolderItem = null;
                if (i == 0 && !string.IsNullOrEmpty(nextFolderName))
                {
                    // First item will be saved by index, rather than name since root folder can contain folders from any directory
                    int index = System.Convert.ToInt32(nextFolderName);
                    nextFolderItem = _PinnedRootFolders.PinnedItems[index];
                    _CurrentPinItem = nextFolderItem;
                    nextFolderName = nextFolderItem.DocumentName;
                }
                else
                {
                    nextFolderItem = currFolder.Where(x => x.DocumentName == nextFolderName).FirstOrDefault();
                }

                if (nextFolderItem == null)
                    break;

                // retrieve the associated storage item (will be null if at pinned folders location)
                if (nextFolderItem.File == null)
                {
                    nextFolderItem.File = await _PinnedRootFolders.GetFolderFromPinnedItemAsync(nextFolderItem);
                }

                // retrieve all the PDF files and storage folders 
                pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> nextFolder = await GetStorageItemsFromFolder(nextFolderItem.File as StorageFolder);

                FolderPathList.Add(new KeyValuePair<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>, string>(nextFolder, nextFolderName));
                _FoldersList.Add(nextFolder);

                currFolder = nextFolder;
            }

            foreach (var item in currFolder)
            {
                item.IsSelected = false;
            }

            return currFolder;
        }

        private async Task<pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>> GetStorageItemsFromFolder(StorageFolder folder)
        {
            pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo> items
                = new pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>();
            var subFolders = await folder.GetFoldersAsync();
            foreach (StorageFolder subFolder in subFolders)
            {
                PinnedItem newItem = _PinnedRootFolders.CreateNewPinnedItem(subFolder);
                items.Add(newItem);
            }
            var subFiles = await folder.GetFilesAsync();
            foreach (StorageFile subFile in subFiles)
            {
                if (!Settings.Settings.AssociatedFileTypes.Contains(subFile.FileType))
                {
                    continue;
                }

                PinnedItem newItem = _PinnedRootFolders.CreateNewPinnedItem(subFile);
                items.Add(newItem);
            }

            return items;
        }

        private async void GoBackInFolder(int times)
        {
            // Do not go past the root folder
            if (times >= _FoldersList.Count || times == 0)
                return;

            IsNavigating = true;
            DocumentPreviewCache.DocumentPreviewCacheResponse -= DocumentPreviewCache_DocumentPreviewCacheResponse;

            int lastIndex = _FoldersList.Count - 1 - times;
            for (int i = _FoldersList.Count - 1; i > lastIndex; --i)
            {
                _FoldersList.RemoveAt(i);
                FolderPathList.RemoveAt(i);
            }

            CurrentlyPinnedItems = _FoldersList.LastOrDefault();
            SaveFolderPathToSettings();

            DocumentPreviewCache.DocumentPreviewCacheResponse += DocumentPreviewCache_DocumentPreviewCacheResponse;
            await Task.Delay(100);
            IsNavigating = false;

            UpdateItemAvailabilityStatus();
            RaisePropertyChanged("IsDeleteEnabled");
            RaisePropertyChanged("IsDeleteAllEnabled");
        }

        private void SaveFolderPathToSettings()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < FolderPathList.Count; i++)
            {
                // use asterisk as a delimiter since Windows filepaths don't allow their usage 
                if (i == 1)
                {
                    // Pin index if saving a pin folder (can be duplicate names)
                    sb.Append(_PinnedRootFolders.PinnedItems.IndexOf(_CurrentPinItem) + "*");
                }
                else
                {
                    // Otherwise, save file name 
                    sb.Append(FolderPathList[i].Value + "*");
                }
            }
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1); // remove last asterisk 

            Settings.Settings.FolderPathList = sb.ToString();
        }

#endregion Folder Depth Navigation


#region Typing Navigation

        /// <summary>
        /// Will parse a hotkey and attempt to perform an action associated with the hotkey.
        /// Will handle left/right/up/down/enter/back keys. 
        /// <param name="args"></param>
        /// <returns> Returns true if it handled the hotkey, false otherwise.</returns>
        /// </summary>
        public bool ParseHotKeyPress(KeyEventArgs args)
        {
            if (_SelectionMode != ListViewSelectionMode.None || _IsModal)
                return false;

            // Handle interaction with a selected item 
            if (_MatchedPinnedItem != null)
            {
                if (args.VirtualKey == Windows.System.VirtualKey.Enter)
                {
                    _MatchedPinnedItem.IsSelected = false;
                    FolderItemClickCommandImpl(_MatchedPinnedItem);
                    _CurrentTypedName.Clear();
                    _MatchedPinnedItem = null;
                    return true;
                }

                if (args.VirtualKey == Windows.System.VirtualKey.Left)
                {
                    int index = FilteredPinnedItems.IndexOf(_MatchedPinnedItem) - 1;
                    if (index < 0)
                        return false;

                    _MatchedPinnedItem.IsSelected = false;
                    _MatchedPinnedItem = FilteredPinnedItems[index];
                    _MatchedPinnedItem.IsSelected = true;
                    UserKeyboardTyping(_MatchedPinnedItem);
                    return true;
                }

                if (args.VirtualKey == Windows.System.VirtualKey.Right)
                {
                    int index = FilteredPinnedItems.IndexOf(_MatchedPinnedItem) + 1;
                    if (index > FilteredPinnedItems.Count - 1)
                        return false;

                    _MatchedPinnedItem.IsSelected = false;
                    _MatchedPinnedItem = FilteredPinnedItems[index];
                    _MatchedPinnedItem.IsSelected = true;
                    UserKeyboardTyping(_MatchedPinnedItem);
                    return true;
                }
            }
            else if (FilteredPinnedItems != null && FilteredPinnedItems.Count > 0
                && (args.VirtualKey == Windows.System.VirtualKey.Left
                || args.VirtualKey == Windows.System.VirtualKey.Right
                || args.VirtualKey == Windows.System.VirtualKey.Up
                || args.VirtualKey == Windows.System.VirtualKey.Down))
            {
                _MatchedPinnedItem = FilteredPinnedItems[0];
                _MatchedPinnedItem.IsSelected = true;
                UserKeyboardTyping(_MatchedPinnedItem);
                return true;
            }

            // Move back one folder
            if (args.VirtualKey == Windows.System.VirtualKey.Back)
            {
                if (_FoldersList.Count > 1)
                {
                    GoBackInFolder(1);

                    // Restart typed streak
                    _CurrentTypedName.Clear();

                    return true;
                }
            }

            return false;
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
            PinnedItem matchedItem = FilteredPinnedItems.Where(x => x.DocumentName.ToLower().StartsWith(currTypedName)).FirstOrDefault();

            if (matchedItem != null && matchedItem.PinnedItemType != PinnedItem.PinnedType.Pin)
            {
                if (_MatchedPinnedItem != null)
                {
                    _MatchedPinnedItem.IsSelected = false;
                }

                _MatchedPinnedItem = matchedItem;
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
            if (FilteredPinnedItems == null || _SelectionMode != ListViewSelectionMode.None || _IsModal || _MatchedPinnedItem == null || itemsPerRow <= 0)
                return;

            // check if last element is on the same row as current matched item. Don't allow down arrow to move down if there is no bottom row.
            if (((FilteredPinnedItems.Count - 1) / itemsPerRow == FilteredPinnedItems.IndexOf(_MatchedPinnedItem) / itemsPerRow) && isDown)
                return;

            int index = isDown ? FilteredPinnedItems.IndexOf(_MatchedPinnedItem) + itemsPerRow : FilteredPinnedItems.IndexOf(_MatchedPinnedItem) - itemsPerRow;

            if (index < 0)
                return;

            if (index > FilteredPinnedItems.Count - 1)
            {
                index = FilteredPinnedItems.Count - 1;
            }

            _MatchedPinnedItem.IsSelected = false;
            _MatchedPinnedItem = FilteredPinnedItems[index];
            _MatchedPinnedItem.IsSelected = true;
            UserKeyboardTyping(_MatchedPinnedItem);
        }

#endregion Typing Navigation


#region Thumbnails 

        private void FilteredPinnedItems_AllRequestsCancelled(object sender, object e)
        {
            DocumentPreviewCache.CancelAllRequests();
        }

        private void FilteredPinnedItems_ResourceNeededForIndex(object sender, int index)
        {
            if (_IsActive)
            {
                PinnedItem item = FilteredPinnedItems[index];
                if (item.PinnedItemType == PinnedItem.PinnedType.File && CurrentlyPinnedItems.Contains(item))
                {
                    bool isDoc = item.DocumentExtension != null && item.DocumentExtension.Equals(".doc", StringComparison.OrdinalIgnoreCase);
                    bool isExcel = item.DocumentExtension != null && item.DocumentExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
                    if (!isDoc && !isExcel && ( !_BadThumbnails.ContainsKey(item) || _BadThumbnails[item] > 0))
                    {
                        int allIndex = CurrentlyPinnedItems.IndexOf(item);
                        int length = ThumbnailSideLength;
                        string idString = item.DocumentPath;
                        if (idString != null)
                        {
                            idString += length;
                        }
                        DocumentPreviewCache.GetBitmapWithID(idString, length, length, new ThumbnailRequestBundle(allIndex, _FolderPathSequenceNumber));
                    }
                    else
                    {
                        if (FilteredPinnedItems.ItemsWithResource.TryAdd(index, new PinnedInfo(item)))
                        {
                            if (isDoc)
                            {
                                SetThumbnailForItem(item, index, "ms-appx:///Assets/DocumentPage/FilePlaceHolder_doc.png", true);
                            }
                            else if (isExcel)
                            {
                                SetThumbnailForItem(item, index, "ms-appx:///Assets/DocumentPage/FilePlaceHolder_xlsx.png", true);
                            }
                            else
                            {
                                SetThumbnailForItem(item, index, "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png", true);
                            }
                        }                        
                        FilteredPinnedItems.RequestResources();
                    }
                }
            }
        }

        private void FilteredPinnedItems_DiscardResourceForIndex(object sender, int index)
        {
            PinnedItem item = FilteredPinnedItems[index];
            if (item.PinnedItemType == PinnedItem.PinnedType.File)
            {
                item.ThumbnailLocation = null;
                item.ShowNameInCoverMode = true;
                item.ThumbLoaded = false;
            }
        }

        private async void DocumentPreviewCache_DocumentPreviewCacheResponse(DocumentPreviewCachePreviewResult result, string previewPath, object requestExtraData)
        {
            ThumbnailRequestBundle bundle = requestExtraData as ThumbnailRequestBundle;
            if (bundle == null)
            {
                return;
            }

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
            {
                if (CurrentlyPinnedItems == null || IsNavigating || bundle.SequenceNumber != _FolderPathSequenceNumber)
                {
                    // this can happen when navigating into folders. If transitioning folders, do not do anything until we have the new folder
                    return;
                }

                int index = bundle.Index;
                if (CurrentlyPinnedItems.Count > index)
                {
                    PinnedItem item = CurrentlyPinnedItems[index];

                    if (FilteredPinnedItems.Contains(item))
                    {
                        int filteredIndex = FilteredPinnedItems.IndexOf(item);

                        if (result == DocumentPreviewCachePreviewResult.e_not_found)
                        {
                            StorageFile sfFile = item.File as StorageFile;
                            if (_IsActive)
                            {
                                int length = ThumbnailSideLength;
                                string idString = item.DocumentPath;
                                if (idString != null)
                                {
                                    idString += length;
                                }
                                DocumentPreviewCache.CreateBitmapWithID(idString, sfFile, length, length, new ThumbnailRequestBundle(index, _FolderPathSequenceNumber));
                            }
                        }
                        else if (result == DocumentPreviewCachePreviewResult.e_success)
                        {
                            if (_BadThumbnails.ContainsKey(item))
                            {
                                // if a success was received, then we know there are no problems with this thumbnail
                                _BadThumbnails[item] = MAX_RETRIES;
                            }
                            if (FilteredPinnedItems.ItemsWithResource.TryAdd(filteredIndex, new PinnedInfo(item)))
                            {
                                SetThumbnailForItem(CurrentlyPinnedItems[index], filteredIndex, previewPath);
                                FilteredPinnedItems.RequestResources();
                            }
                        }
                        else
                        {
                            if (result == DocumentPreviewCachePreviewResult.e_security_error)
                            {
                                if (FilteredPinnedItems.ItemsWithResource.TryAdd(filteredIndex, new PinnedInfo(item)))
                                {
                                    SetThumbnailForItem(CurrentlyPinnedItems[index], filteredIndex, "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf_locked.png", true);
                                }
                                FilteredPinnedItems.RequestResources();
                            }

                            else if (result == DocumentPreviewCachePreviewResult.e_package_error)
                            {
                                if (FilteredPinnedItems.ItemsWithResource.TryAdd(filteredIndex, new PinnedInfo(item)))
                                {
                                    SetThumbnailForItem(CurrentlyPinnedItems[index], filteredIndex, "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png", true);
                                }
                                FilteredPinnedItems.RequestResources();
                            }

                            // Rest are failures
                            else if (result != DocumentPreviewCachePreviewResult.e_cancel)
                            {
                                FilteredPinnedItems.ReportRequestFailed(filteredIndex);
                                if (!_BadThumbnails.ContainsKey(item))
                                {
                                    // If response was a cancel, give it more retries, otherwise never check it again 
                                    int val = MAX_RETRIES;
                                    _BadThumbnails.Add(item, val);
                                }
                                else
                                {
                                    if (_BadThumbnails[item] > 0)
                                    {
                                        _BadThumbnails[item]--;
                                    }
                                    if (_BadThumbnails[item] <= 0)
                                    {
                                        if (FilteredPinnedItems.ItemsWithResource.TryAdd(filteredIndex, new PinnedInfo(item)))
                                        {
                                            SetThumbnailForItem(CurrentlyPinnedItems[index], filteredIndex, "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png", true);
                                        }
                                    }
                                }
                                FilteredPinnedItems.RequestResources();
                            }

                            FilteredPinnedItems.RequestResources();
                        }
                    }
                }
            });
        }

        private void SetThumbnailForItem(PinnedItem item, int index, string imageLocation, bool isPlaceHolder = false)
        {
            item.ThumbnailLocation = imageLocation;
            item.NeedsBorder = !isPlaceHolder;
            item.ShowNameInCoverMode = isPlaceHolder;
            if (!item.ThumbLoaded)
            {
                item.ThumbLoaded = true;
                ThumbnailReceived?.Invoke(item, index);
            }
        }

#endregion ThumbNails


#region Filter

        private bool _LockFilters = false;
        private bool LockFilters
        {
            get { return _LockFilters; }
            set
            {
                if (Set(ref _LockFilters, value))
                {
                    if (!_LockFilters)
                    {
                        ApplyNewFilter();
                        SaveFiltersToSettings();
                    }
                }
            }
        }
        private bool _ShowAllFileTypes = true;
        public bool ShowAllFileTypes
        {
            get { return _ShowAllFileTypes; }
            set
            {
                if (Set(ref _ShowAllFileTypes, value))
                {
                    if (!LockFilters)
                    {
                        LockFilters = true;
                        if (_ShowAllFileTypes)
                        {
                            ShowPDFs = false;
                            ShowOfficeDocuments = false;
                            ShowImages = false;
                        }
                        LockFilters = false;
                    }
                }
            }
        }

        private bool _ShowPDFs = false;
        public bool ShowPDFs
        {
            get { return _ShowPDFs; }
            set
            {
                if (Set(ref _ShowPDFs, value))
                {
                    if (!LockFilters)
                    {
                        LockFilters = true;
                        ShowAllFileTypes = NoSpecificFileTypeSelected;
                        LockFilters = false;
                    }
                }
            }
        }

        private bool _ShowOfficeDocuments = false;
        public bool ShowOfficeDocuments
        {
            get { return _ShowOfficeDocuments; }
            set
            {
                if (Set(ref _ShowOfficeDocuments, value))
                {
                    if (!LockFilters)
                    {
                        LockFilters = true;
                        ShowAllFileTypes = NoSpecificFileTypeSelected;
                        LockFilters = false;
                    }
                }
            }
        }

        private bool _ShowImages = false;
        public bool ShowImages
        {
            get { return _ShowImages; }
            set
            {
                if (Set(ref _ShowImages, value))
                {
                    if (!LockFilters)
                    {
                        LockFilters = true;
                        ShowAllFileTypes = NoSpecificFileTypeSelected;
                        LockFilters = false;
                    }
                }
            }
        }

        private bool NoSpecificFileTypeSelected { get { return !ShowPDFs && !ShowOfficeDocuments && !ShowImages; } }

        private bool IncludePDFs { get { return ShowPDFs || ShowAllFileTypes; } }

        private bool IncludeOfficeDocumentss { get { return ShowOfficeDocuments || ShowAllFileTypes; } }

        private bool IncludeImages { get { return ShowImages || ShowAllFileTypes; } }

        private void ApplyNewFilter()
        {
            DocumentPreviewCache.CancelAllRequests();
            List<PinnedItem> itemsWithResources = new List<PinnedItem>();
            foreach (PinnedInfo info in FilteredPinnedItems.ItemsWithResource.Values)
            {
                itemsWithResources.Add(info.Item);
            }
            FilteredPinnedItems.ItemsWithResource.Clear();

            IsNavigating = true;
            try
            {
                FilterItems();
            }
            catch (Exception) { }
            finally
            {
                IsNavigating = false;
            }

            int filterIndex = 0;
            int filteredItems = FilteredPinnedItems.Count;
            foreach (PinnedItem item in itemsWithResources)
            {
                if (ItemBelongsInFilteredList(item))
                {
                    while (filterIndex < filteredItems && item != FilteredPinnedItems[filterIndex])
                    {
                        filterIndex++;
                    }
                    if (!FilteredPinnedItems.ItemsWithResource.TryAdd(new KeyValuePair<int, PinnedInfo>(filterIndex, new PinnedInfo(item))))
                    {
                        item.ThumbnailLocation = null;
                        item.ShowNameInCoverMode = true;
                        item.ThumbLoaded = false;
                    }
                }
                else
                {
                    item.ThumbnailLocation = null;
                    item.ShowNameInCoverMode = true;
                    item.ThumbLoaded = false;
                }
            }
        }

        private void LoadFiltersFromSettings()
        {
            int setting = Settings.Settings.FileTypeFilter;
            _ShowAllFileTypes = (setting & (int)Settings.Settings.FilterKeys.AllFiles) == (int)Settings.Settings.FilterKeys.AllFiles;
            _ShowPDFs = (setting & (int)Settings.Settings.FilterKeys.PDFs) == (int)Settings.Settings.FilterKeys.PDFs;
            _ShowOfficeDocuments = (setting & (int)Settings.Settings.FilterKeys.OfficeFiles) == (int)Settings.Settings.FilterKeys.OfficeFiles;
            _ShowImages = (setting & (int)Settings.Settings.FilterKeys.Images) == (int)Settings.Settings.FilterKeys.Images;
        }

        private void SaveFiltersToSettings()
        {
            int setting = 0;
            if (ShowAllFileTypes)
            {
                setting |= (int)Settings.Settings.FilterKeys.AllFiles;
            }
            if (ShowPDFs)
            {
                setting |= (int)Settings.Settings.FilterKeys.PDFs;
            }
            if (ShowOfficeDocuments)
            {
                setting |= (int)Settings.Settings.FilterKeys.OfficeFiles;
            }
            if (ShowImages)
            {
                setting |= (int)Settings.Settings.FilterKeys.Images;
            }
            Settings.Settings.FileTypeFilter = setting;
        }

#endregion Filter

    }
}
