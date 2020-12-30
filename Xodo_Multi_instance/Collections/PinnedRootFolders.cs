using CompleteReader.Collections;
using CompleteReader.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace CompleteReader.Collections
{
    public class PinnedItem : ViewModelBase
    {
        public enum PinnedType
        {
            Pin,
            Folder,
            File
        }

        private bool _IsSelected = false;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { Set(ref _IsSelected, value); }
        }

        private PinnedType _PinnedItemType = PinnedType.Pin;
        public PinnedType PinnedItemType
        {
            get { return _PinnedItemType; }
            set
            {
                if (Set(ref _PinnedItemType, value))
                {
                    RaisePropertyChanged("IsFolder");
                }
            }
        }

        public bool IsFolder {  get { return _PinnedItemType == PinnedType.Folder; } }
        private bool _NeedsBoder = false;
        public bool NeedsBorder
        {
            get { return _NeedsBoder; }
            set { Set(ref _NeedsBoder, value); }
        }

        private string _ThumbnailLocation = null;
        public string ThumbnailLocation
        {
            get { return _ThumbnailLocation; }
            set { Set(ref _ThumbnailLocation, value); }
        }

        private bool _ThumbLoaded = false;
        public bool ThumbLoaded
        {
            get { return _ThumbLoaded; }
            set { Set(ref _ThumbLoaded, value); }
        }

        private bool _ShowNameInCoverMode = true;
        public bool ShowNameInCoverMode
        {
            get { return _ShowNameInCoverMode; }
            set {  Set(ref _ShowNameInCoverMode, value); }
        }

        private double _d_Opacity = 1.0;
        public double d_Opacity
        {
            get { return _d_Opacity; }
            set { Set(ref _d_Opacity, value); }
        }

        private string _DocumentName = "";
        public string DocumentName
        {
            get { return _DocumentName; }
            set { Set(ref _DocumentName, value); }
        }

        public string DocumentExtension
        {
            get
            {
                if (PinnedItemType == PinnedType.File)
                {
                    return System.IO.Path.GetExtension(DocumentName);
                }
                return "";
            }
        }

        private bool _HasAdditionalIcon = false;
        public bool HasAdditionalIcon
        {
            get { return _HasAdditionalIcon; }
            set { Set(ref _HasAdditionalIcon, value); }
        }

        private string _AdditionalIconLocation = null;
        public string AdditionalIconLocation
        {
            get { return _AdditionalIconLocation; }
            set { Set(ref _AdditionalIconLocation, value); }
        }

        public string Token { get; private set; }

        private string _DocumentPath;
        public string DocumentPath
        {
            get { return _DocumentPath; }
            private set { _DocumentPath = value; }
        }

        public IStorageItem File { get; set; }

        public void SetToFolder()
        {
            PinnedItemType = PinnedType.Folder;
            ThumbLoaded = true;
            ThumbnailLocation = "ms-appx:///Assets/DocumentPage/FolderIcon.png";
        }

        public void CheckAdditionalIcon()
        {
            if (Utilities.UtilityFunctions.IsOneDrive(DocumentPath))
            {
                HasAdditionalIcon = true;
                AdditionalIconLocation = RecentDocumentProperties.ONEDRIVE_ICON_LOCATION;
            }
        }

        public PinnedItem(string documentName, string token, string documentPath)
        {
            DocumentName = documentName;
            Token = token;
            DocumentPath = documentPath;
            CheckAdditionalIcon();
        }

        public PinnedItem(IStorageItem file)
        {
            File = file;
            DocumentName = file.Name;
            DocumentPath = file.Path;
            if (file is StorageFile)
            {
                PinnedItemType = PinnedType.File;
                CheckAdditionalIcon();
            }
            else
            {
                SetToFolder();
            }
        }

        public static PinnedItem CreatePin()
        {
            string text = ResourceLoader.GetForCurrentView().GetString("DocumentsPage_FoldersPage_PinItem_Text");
            if (Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Width < 450)
            {
                text = ResourceLoader.GetForCurrentView().GetString("DocumentsPage_FoldersPage_SmallPinItem_Text");
            }
            PinnedItem item = new PinnedItem(text, "", "pin");
            return item;
        }
    }

    public class PinnedInfo : pdftron.PDF.Tools.Controls.Resources.ObservableCollectionWithAsyncResource<PinnedItem, PinnedInfo>.IResourceHolder
    {
        public PinnedItem Item { get; private set; }
        public double TotalSize { get { return 0; } }
        public PinnedInfo(PinnedItem item)
        {
            Item = item;
        }
    }

    class PinnedRootFolders
    {
        #region Settings

        private static string NUMBER_OF_PINNED_FOLDERS_SETTINGS_NAME = "CompleteReader_NumberOfPinnedFolders";
        private static string PINNED_FOLDER_SETTINGS_NAME = "CompleteReader_PinnedFolderNumber_";

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

        private int NumberOfPinnedFolders
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(NUMBER_OF_PINNED_FOLDERS_SETTINGS_NAME))
                    {
                        return (int)LocalSettings.Values[NUMBER_OF_PINNED_FOLDERS_SETTINGS_NAME];
                    }
                }
                catch (Exception) { }
                return 0;
            }
            set
            {
                LocalSettings.Values[NUMBER_OF_PINNED_FOLDERS_SETTINGS_NAME] = value;
            }
        }

        private class PinnedFolderInfo
        {
            public string Token { get; private set; }
            public string FolderPath { get; private set; }

            public PinnedFolderInfo(string token, string folderName)
            {
                Token = token;
                FolderPath = folderName;
            }

            public PinnedFolderInfo(string propertyString)
            {
                string[] splitters = { Environment.NewLine };
                string[] splitProperties = propertyString.Split(splitters, StringSplitOptions.RemoveEmptyEntries);
                if (splitProperties.Length >= 2)
                {
                    Token = splitProperties[0].Trim();
                    FolderPath = splitProperties[1].Trim();
                }
                else
                {
                    Token = "";
                    FolderPath = "";
                }
            }

            public string GetStorageString()
            {
                return Token + Environment.NewLine + FolderPath;
            }
        }

        private PinnedFolderInfo GetPinnedFolder(int index)
        {
            try
            {
                string settingsName = PINNED_FOLDER_SETTINGS_NAME + index;
                if (LocalSettings.Values.ContainsKey(settingsName))
                {
                    return new PinnedFolderInfo((string)LocalSettings.Values[settingsName]);
                }
            }
            catch (Exception) { }
            return null;
        }

        private void SetPinnedFolder(PinnedFolderInfo info, int index)
        {
            string settingsName = PINNED_FOLDER_SETTINGS_NAME + index;
            string test = info.GetStorageString(); 
            LocalSettings.Values[settingsName] = info.GetStorageString();
        }

        /// <summary>
        /// Shifts all the pinned folders down by 1, starting at startingIndex
        /// </summary>
        /// <param name="startingIndex">The index at which to start shifting folders</param>
        private void DeletePinnedFolder(int startingIndex)
        {
            NumberOfPinnedFolders--;
            for (int index = startingIndex; index < NumberOfPinnedFolders; ++index)
            {
                SetPinnedFolder(GetPinnedFolder(index + 1), index);
            }
        }

        #endregion Settings


        #region Singleton and Loading

        private static PinnedRootFolders _Instance;
        private static PinnedRootFolders Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new PinnedRootFolders();
                }
                return _Instance;
            }
        }

        private SemaphoreSlim _LoadingSemaphore = new SemaphoreSlim(1);
        private List<PinnedItem> _PinnedItems;
        public List<PinnedItem> PinnedItems { get { return _PinnedItems; } }

        private PinnedRootFolders()
        {
        }

        /// <summary>
        /// This functions will always wait until the RecentItems list is populated, and will then return an instance of RecentItemsData.
        /// This should always be the way to get an instance of this class.
        /// </summary>
        /// <param name="parent">The owner of the recent items list. Used by this class to invoke the dispatcher</param>
        /// <returns>An instance of RecentItem guaranteed to have the list of recent items populated.</returns>
        public static async Task<PinnedRootFolders> GetItemSourceAsync()
        {
            await Instance._LoadingSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (Instance._PinnedItems == null)
                {
                    Instance._PinnedItems = await Instance.GetPinnedItemsAsync();
                }
            }
            catch (Exception e) { System.Diagnostics.Debug.WriteLine("Error: " + e.ToString()); }
            finally
            {
                Instance._LoadingSemaphore.Release();
            }
            return _Instance;
        }

        private IAsyncOperation<List<PinnedItem>> GetPinnedItemsAsync()
        {
            Task<List<PinnedItem>> t 
                = new Task<List<PinnedItem>>(() =>
            {
                return GetPinnedItems();
            });
            t.Start();
            return t.AsAsyncOperation();
        }

        private List<PinnedItem> GetPinnedItems()
        {
            List<PinnedItem> pinnedItemsList = new List<PinnedItem>();
            try
            {
                int numberOfItems = NumberOfPinnedFolders;
                AccessListEntryView entries = StorageApplicationPermissions.FutureAccessList.Entries;

                for (int index = 0; index < NumberOfPinnedFolders; index++)
                {
                    PinnedFolderInfo folderInfo = GetPinnedFolder(index);
                    //RecentDocumentProperties props = new RecentDocumentProperties();
                    //props.FilePath = folderInfo.FolderPath;
                    //props.ThumbnailLocation = "ms-appx:///Assets/DocumentPage/FolderIcon.png";
                    //props.IsFolder = true;
                    //PinnedItem item = new PinnedItem(props, folderInfo.Token, true);
                    PinnedItem item = new PinnedItem(System.IO.Path.GetFileName(folderInfo.FolderPath), folderInfo.Token, folderInfo.FolderPath);
                    item.SetToFolder();
                    pinnedItemsList.Add(item);
                }
            }
            catch (Exception)
            {
            }
            return pinnedItemsList;
        }

        #endregion Singleton and Loading


        #region ListManagement

        /// <summary>
        /// Adds the folder as a pinned item. Will return the PinnedItem created if the folder was added, 
        /// and null if the folder was already pinned.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public PinnedItem AddPinnedFolder(StorageFolder item)
        {
            int index = NumberOfPinnedFolders;
            try
            {
                string token = StorageApplicationPermissions.FutureAccessList.Add(item);
                PinnedItem pinnedItem = new PinnedItem(item.Name, token, item.Path);
                pinnedItem.SetToFolder();
                
                if (PinnedItems.Where(x => x.Token == pinnedItem.Token).Count() == 0)
                {
                    {
                        PinnedItems.Add(pinnedItem);
                    }
                    SetPinnedFolder(new PinnedFolderInfo(token, pinnedItem.DocumentPath), index);
                    NumberOfPinnedFolders++;
                    return pinnedItem;
                }
            }
            catch (Exception e) { System.Diagnostics.Debug.WriteLine("Error adding folder: " + e.ToString()); }
            return null;
        }

        public void DeletePinnedFolder(PinnedItem item)
        {
            int index = PinnedItems.IndexOf(item);
            PinnedItems.Remove(item);
            DeletePinnedFolder(index);
        }

        public void ClearAllPinnedFolders()
        {
            int desiredNumberOfItems = 0;
            while (PinnedItems.Count > desiredNumberOfItems)
            {
                PinnedItems.RemoveAt(0);
            }
            NumberOfPinnedFolders = 0;
        }

        public async Task<StorageFolder> GetFolderFromPinnedItemAsync(PinnedItem item)
        {
            StorageFolder folder = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(item.Token))
                {
                    folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(item.Token);
                }
            }
            catch (Exception e) { System.Diagnostics.Debug.WriteLine("Error getting storage folder: " + e.ToString()); }

            return folder;
        }

        /// <summary>
        /// Creates a recent item from a storage item without adding it to the future access list 
        /// </summary>
        /// <param name="file"></param>
        /// <returns>The recent item created</returns>
        public PinnedItem CreateNewPinnedItem(IStorageItem file)
        {
            bool isFolder = file.IsOfType(StorageItemTypes.Folder);
            PinnedItem item = new PinnedItem(file);
            if (file is StorageFolder)
            {
                item.SetToFolder();
            }
            return item;
        }

        public bool ContainsToken(string token)
        {
            foreach (PinnedItem item in PinnedItems)
            {
                if (item.Token != null && item.Token.Equals(token))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion ListManagement
    }
}
