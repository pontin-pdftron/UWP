using CompleteReader.ViewModels.Common;
using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;

namespace CompleteReader.Collections
{
    /// <summary>
    /// Stores a collection of properties about the state of a recent document
    /// </summary>
    public class RecentDocumentProperties
    {
        public enum AdditionalIcons
        {
            None,
            OneDrive,
        }

        public const string DEFAULT_THUMB_PATH = "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png";
        public const string DEFAULT_PROTECTED_THUMB_PATH = "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf_locked.png";
        public const string DEFAULT_PACKAGE_THUMB_PATH = "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png";
        public const string DEFAULT_FOLDER_PATH = "ms-appx:///Assets/DocumentPage/FolderIcon.png";
        public const string ONEDRIVE_ICON_LOCATION = "ms-appx:///Assets/DocumentPage/OneDrive_rgb_Blue2728.png";

        private string _ThumbnailLocation = string.Empty;
        [System.Xml.Serialization.XmlIgnore]
        public string ThumbnailLocation
        {
            get
            {
                if (IsFolder)
                {
                    return DEFAULT_FOLDER_PATH;
                }
                if (IsProtected)
                {
                    return DEFAULT_PROTECTED_THUMB_PATH;
                }
                if (string.IsNullOrWhiteSpace(_ThumbnailLocation))
                {
                    return DEFAULT_THUMB_PATH;
                }
                return _ThumbnailLocation;
            }
            set { _ThumbnailLocation = value; }
        }

        [System.Xml.Serialization.XmlElement(ElementName = "IsFolder")]
        public bool IsFolder { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "Path")]
        public string FilePath { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public IStorageItem File { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "Pg")]
        public int PageNumber { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "PW")]
        public bool IsProtected { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "Zoom")]
        public double Zoom { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "HP")]
        public int HorizontalScrollPos { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "VP")]
        public int VerticalScrollPos { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "ExtraIcon")]
        public AdditionalIcons AdditonalIcon { get; set; }

        private pdftron.PDF.PDFViewCtrlPagePresentationMode _PagePresentationMode = pdftron.PDF.PDFViewCtrlPagePresentationMode.e_single_page;
        [System.Xml.Serialization.XmlElement(ElementName = "PPM")]
        public pdftron.PDF.PDFViewCtrlPagePresentationMode PagePresentationMode
        {
            get { return _PagePresentationMode; }
            set { _PagePresentationMode = value; }
        }

        [System.Xml.Serialization.XmlElement(ElementName = "RF")]
        public bool IsInReflowMode { get; set; }

        public pdftron.PDF.PageRotate _PageRotation = pdftron.PDF.PageRotate.e_0;
        [System.Xml.Serialization.XmlElement(ElementName = "Rot")]
        public pdftron.PDF.PageRotate PageRotation
        {
            get { return _PageRotation; }
            set { _PageRotation = value; }
        }

        public RecentDocumentProperties(IStorageItem file, int pageNumber, bool isProtected, bool isFolder = false)
        {
            File = file;
            PageNumber = pageNumber;
            IsProtected = isProtected;
            if (isFolder)
            {
                _ThumbnailLocation = DEFAULT_FOLDER_PATH;
                IsFolder = true;
            }
            else
            {
                IsFolder = false;
            }
        }

        public RecentDocumentProperties()
        { }

        public static RecentDocumentProperties FromString(string data)
        {
            System.Xml.Serialization.XmlSerializer xmlIzer = new System.Xml.Serialization.XmlSerializer(typeof(RecentDocumentProperties));
            using (System.IO.StringReader sr = new System.IO.StringReader(data))
            {
                RecentDocumentProperties props = (xmlIzer.Deserialize(sr)) as RecentDocumentProperties;
                return props;
            }
        }

        public string MakeString()
        {
            System.Xml.Serialization.XmlSerializer xmlIzer = new System.Xml.Serialization.XmlSerializer(typeof(RecentDocumentProperties));
            System.IO.StringWriter writer = new System.IO.StringWriter();
            xmlIzer.Serialize(writer, this);
            string stringy = writer.ToString();
            return writer.ToString();
        }
    }

    /// <summary>
    /// Represents a recently viewed item
    /// </summary>
    public class RecentItem : ViewModelBase
    {
        /// <summary>
        /// Then name of the document
        /// </summary>
        public string DocumentName { get; private set; }

        /// <summary>
        /// The path where the thumbnail is located
        /// </summary>
        public string ThumbnailLocation
        {
            get { return Properties.ThumbnailLocation; }
            set
            {
                if (Properties.ThumbnailLocation != value)
                {
                    Properties.ThumbnailLocation = value;
                    RaisePropertyChanged();
                    NeedsBorder = ThumbnailLocation != null &&
                        !ThumbnailLocation.Equals(RecentDocumentProperties.DEFAULT_THUMB_PATH, StringComparison.OrdinalIgnoreCase) &&
                        !ThumbnailLocation.Equals(RecentDocumentProperties.DEFAULT_PROTECTED_THUMB_PATH, StringComparison.OrdinalIgnoreCase) &&
                        !ThumbnailLocation.Equals(RecentDocumentProperties.DEFAULT_PACKAGE_THUMB_PATH, StringComparison.OrdinalIgnoreCase) &&
                        !ThumbnailLocation.Equals(RecentDocumentProperties.DEFAULT_FOLDER_PATH, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        public string AdditionalIconLocation
        {
            get
            {
                switch (Properties.AdditonalIcon)
                {
                    case RecentDocumentProperties.AdditionalIcons.OneDrive:
                        return System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "DocumentPage", "OneDrive_rgb_Blue2728.png");
                    default:
                        return null;
                }
            }
        }

        public bool HasAdditionalLogo { get { return !string.IsNullOrEmpty(AdditionalIconLocation); } }

        public string Token { get; private set; }

        public string DocumentPath
        {
            get { return Properties.FilePath; }
            private set { Properties.FilePath = value; }
        }

        public bool IsProtected
        {
            get { return Properties.IsProtected; }
            set { Properties.IsProtected = value; }
        }

        public int LastPageNumber
        {
            get { return Properties.PageNumber; }
            set { Properties.PageNumber = value; }
        }

        private bool _IsSelected;

        public bool IsSelected
        {
            get { return _IsSelected; }
            set { Set(ref _IsSelected, value); }
        }

        private bool _NeedsBoder = false;
        public bool NeedsBorder
        {
            get { return _NeedsBoder; }
            set { Set(ref _NeedsBoder, value); }
        }

        public RecentDocumentProperties Properties { get; set; }

        public RecentItem(string documentName, string thumbLocation, string token, string documentPath, bool isProtected = false)
        {
            this.DocumentName = documentName;
            Properties = new RecentDocumentProperties(null, 1, isProtected);
            this.ThumbnailLocation = thumbLocation;
            this.Token = token;
            this.DocumentPath = documentPath;
            this.IsSelected = false;

            if (Utilities.UtilityFunctions.IsOneDrive(documentPath))
            {
                Properties.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.OneDrive;
            }
            else
            {
                Properties.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.None;
            }
        }

        public RecentItem(RecentDocumentProperties properties, string token, bool isPin = false)
        {
            // don't parse a root system folder
            if (properties.FilePath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).Length <= 1)
            {
                this.DocumentName = properties.FilePath;
            }
            else
            {
                this.DocumentName = System.IO.Path.GetFileName(properties.FilePath);
            }
            this.Properties = properties;
            this.Token = token;
            if (Utilities.UtilityFunctions.IsOneDrive(this.DocumentPath))
            {
                Properties.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.OneDrive;
            }
            else
            {
                Properties.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.None;
            }
        }
    }

    /// <summary>
    /// This class represents a collection of recent items the user has accessed.
    /// </summary>
    public class RecentItemsData : System.ComponentModel.INotifyPropertyChanged
    {
        private static RecentItemsData _Instance = null;
        public static RecentItemsData Instance
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

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(property));
            }
        }

        private static ObservableCollection<RecentItem> _recentFiles = null;
        public ObservableCollection<RecentItem> RecentFiles
        {
            get { return _recentFiles; }
        }

        private static ObservableCollection<RecentItem> _recentFolders = null;
        public ObservableCollection<RecentItem> RecentFolders
        {
            get { return _recentFolders; }
        }

        private static bool _IsReady = false;
        public static bool IsReady
        {
            get { return _IsReady; }
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

        /// <summary>
        /// Creates a new instance of RecentItemsData.
        /// This function will return immediately, so there is no guarantee that the list of recent items
        /// has been populated.
        /// </summary>
        /// <param name="parent">The owner of the recent items list. Used by this class to invoke the dispatcher</param>
        private RecentItemsData()
        {
            return;
        }

        /// <summary>
        /// This functions will always wait until the RecentItems list is populated, and will then return an instance of RecentItemsData.
        /// This should always be the way to get an instance of this class.
        /// </summary>
        /// <param name="parent">The owner of the recent items list. Used by this class to invoke the dispatcher</param>
        /// <returns>An instance of RecentItem guaranteed to have the list of recent items populated.</returns>
        public static async Task<RecentItemsData> GetItemSourceAsync()
        {
            await LoadingSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsReady && CompleteReader.Utilities.DocumentManager.IsReady)
                {
                    if (_Instance == null)
                    {
                        _Instance = new RecentItemsData();
                    }
                    if (_recentFiles == null)
                    {
                        //CompleteReader.Utilities.DocumentManager docManager = await CompleteReader.Utilities.DocumentManager.GetInstanceAsync();
                        CompleteReader.Utilities.DocumentManager docManager = CompleteReader.Utilities.DocumentManager.Instance;
                        _recentFiles = await _Instance.GetRecentDocumentsAsync(docManager, StorageItemTypes.File);
                        _recentFolders = await _Instance.GetRecentDocumentsAsync(docManager, StorageItemTypes.Folder);
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

        private IAsyncOperation<ObservableCollection<RecentItem>> GetRecentDocumentsAsync(CompleteReader.Utilities.DocumentManager docManager, StorageItemTypes type)
        {
            Task<ObservableCollection<RecentItem>> t = new Task<ObservableCollection<RecentItem>>(() =>
            {
                return GetRecentDocuments(docManager, type);
            });
            t.Start();
            return t.AsAsyncOperation<ObservableCollection<RecentItem>>();
        }


        private ObservableCollection<RecentItem> GetRecentDocuments(CompleteReader.Utilities.DocumentManager docManager, StorageItemTypes type)
        {
            try
            {
                ObservableCollection<RecentItem> recentItemsList = new ObservableCollection<RecentItem>();

                AccessListEntryView entries = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
                foreach (AccessListEntry entry in entries)
                {
                    RecentItem item = null; 
                    try
                    {
                        RecentDocumentProperties props = RecentDocumentProperties.FromString(entry.Metadata);

                        // If we don't have a file vs folder as we expect, then move on
                        if ((type == StorageItemTypes.Folder && props.IsFolder == false)
                        || (type == StorageItemTypes.File && props.IsFolder == true))
                            continue;

                        item = new RecentItem(props, entry.Token);
                        string tempFilePath = null;
                        if (!string.IsNullOrEmpty(item.DocumentPath))
                        {
                            tempFilePath = docManager.GetRecentThumbnailPath(item.DocumentPath);
                        }
                        if (!string.IsNullOrEmpty(tempFilePath))
                        {
                            // This means we're using a temporary file.
                            item.ThumbnailLocation = pdftron.Common.RecentlyUsedCache.GetBitmapPathIfExists(tempFilePath);
                        }

                        if (type == StorageItemTypes.Folder)
                            item.Properties.ThumbnailLocation = "ms-appx:///Assets/DocumentPage/FolderIcon.png";

                        recentItemsList.Add(item);
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to load entry from XML string");
                    } // likely because it's an old entry that hasn't been updated yet


                    if (item == null)
                    {
                        try
                        {
                            string thumbPath = "";
                            string[] filePathAndFlags = entry.Metadata.Split('/');
                            if (filePathAndFlags.Length > 0)
                            {
                                string tempFilePath = docManager.GetRecentThumbnailPath(filePathAndFlags[0]);
                                if (!string.IsNullOrEmpty(tempFilePath))
                                {
                                    // This means we're using a temporary file.
                                    thumbPath = pdftron.Common.RecentlyUsedCache.GetBitmapPathIfExists(tempFilePath);
                                }
                                else
                                {
                                    // this means we're using a 
                                    thumbPath = pdftron.Common.RecentlyUsedCache.GetBitmapPathIfExists(filePathAndFlags[0]);
                                }
                            }
                            bool isProtected = FindOptionInFlags("L", filePathAndFlags);
                            if (type == StorageItemTypes.Folder)
                            {
                                thumbPath = "ms-appx:///Assets/DocumentPage/FolderIcon.png";
                            }
                            else if (isProtected)
                            {
                                thumbPath = "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf_locked.png";
                            }
                            else if (string.IsNullOrEmpty(thumbPath))
                            {
                                thumbPath = "ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png";
                            }
                            item = new RecentItem(System.IO.Path.GetFileName(filePathAndFlags[0]), thumbPath, entry.Token, filePathAndFlags[0], isProtected);
                            item.LastPageNumber = GetPageNumberFromMetadataString(entry.Metadata);
                            recentItemsList.Add(item);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
                return recentItemsList;
            }
            catch (Exception)
            {

            }
            return new ObservableCollection<RecentItem>();
        }

        #region List Management

        /// <summary>
        /// Retrieves the StorageFile associated with the item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<RecentDocumentProperties> GetRecentFileAsync(RecentItem item)
        {
            await LoadingSemaphore.WaitAsync(1);
            try
            {
                IStorageItem file = null;
                if (item.Properties.IsFolder)
                {
                    file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(item.Token);
                }
                else
                {
                    file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(item.Token, AccessCacheOptions.SuppressAccessTimeUpdate);
                }
                item.Properties.File = file;
                return item.Properties;
            }
            catch (Exception) { }
            finally
            {
                LoadingSemaphore.Release();
            }
            return null;
        }

        /// <summary>
        /// Checks to see if there are any new thumbnails available.
        /// </summary>
        public async Task UpdateThumbnailLocations()
        {
            foreach (RecentItem item in RecentFiles)
            {
                string tempFilePath = null;
                if (!string.IsNullOrEmpty(item.DocumentPath))
                {
                    tempFilePath = await CompleteReader.Utilities.DocumentManager.Instance.GetRecentThumbnailPathAsync(item.DocumentPath);
                }
                if (!string.IsNullOrEmpty(tempFilePath))
                {
                    // This means we're using a temporary file.
                    item.ThumbnailLocation = pdftron.Common.RecentlyUsedCache.GetBitmapPathIfExists(tempFilePath);
                }
            }
        }

        /// <summary>
        /// Updates the recent list with a file that was selected from the recent list
        /// This means the only work that has to be done is to move it to the top of the list
        /// </summary>
        /// <param name="file"></param>
        /// <param name="isProtected"></param>
        /// <returns>The token for the file</returns>
        public string UpdateWithRecentFile(IStorageItem file, bool isProtected = false)
        {
            bool isFolder = file.IsOfType(StorageItemTypes.Folder) ? true : false;
            RecentDocumentProperties props = new RecentDocumentProperties(null, 1, isProtected, isFolder);
            props.FilePath = file.Path;
            if (Utilities.UtilityFunctions.IsOneDrive(props.FilePath))
            {
                props.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.OneDrive;
            }
            else
            {
                props.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.None;
            }
            string metaData = props.MakeString();
            string token = StorageApplicationPermissions.MostRecentlyUsedList.Add(file, metaData);
            int listIndex = FindTokenInList(token);
            if (listIndex >= 0)
            {
                if (RecentFiles[listIndex].LastPageNumber > 0)
                {
                    props = RecentFiles[listIndex].Properties;
                    metaData = props.MakeString();
                    StorageApplicationPermissions.MostRecentlyUsedList.Add(file, metaData);
                }
                if (listIndex < RecentFiles.Count)
                {
                    RecentFiles.RemoveAt(listIndex);
                }
            }

            RecentItem item = new RecentItem(props, token);

            if (file.IsOfType(StorageItemTypes.File))
            {
                this.RecentFiles.Insert(0, item);
            }
            else
            {
                this.RecentFolders.Insert(0, item);
            }

            //this.RecentItems.Insert(0, new RecentItem(file.Name, System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "DocumentPage", "FilePlaceHolder_pdf_locked.png"), token, file.Path, isProtected));
            return token;
        }

        /// <summary>
        /// Updates the recent list with a file picked from somewhere other than the recent list
        /// </summary>
        /// <param name="file"></param>
        /// <param name="isProtected"></param>
        /// <returns>The token for the file</returns>
        public string UpdateWithNewFile(IStorageItem file, bool isProtected = false)
        {
            return UpdateWithRecentFile(file, isProtected);
        }

        /// <summary>
        /// Creates a recent item from a storage item without adding it to the future access list 
        /// </summary>
        /// <param name="file"></param>
        /// <returns>The recent item created</returns>
        public RecentItem CreateNewPinnedItem (IStorageItem file)
        {
            bool isFolder = file.IsOfType(StorageItemTypes.Folder);
            RecentDocumentProperties props = new RecentDocumentProperties(null, 1, false, isFolder);
            props.FilePath = file.Path;
            props.File = file;
            if (Utilities.UtilityFunctions.IsOneDrive(props.FilePath))
            {
                props.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.OneDrive;
            }
            else
            {
                props.AdditonalIcon = RecentDocumentProperties.AdditionalIcons.None;
            }

            return new RecentItem(props, "");
        }

        /// <summary>
        /// Removes all items from recentItems from the list of recent items
        /// </summary>
        /// <param name="recentItems"></param>
        public void RemoveItems(IList<RecentItem> recentItems)
        {
            foreach (RecentItem recentItem in recentItems)
            {
                try
                {
                    StorageApplicationPermissions.MostRecentlyUsedList.Remove(recentItem.Token);
                    this.RecentFiles.Remove(recentItem);
                    pdftron.Common.RecentlyUsedCache.RemoveDocument(recentItem.DocumentPath);
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Will remove the first recent item which references the storage file if there is any.
        /// </summary>
        /// <param name="file"></param>
        public async void RemoveItem(StorageFile file)
        {
            await LoadingSemaphore.WaitAsync(1);
            try
            {
                foreach (RecentItem item in _recentFiles)
                {
                    try
                    {
                        StorageFile fileToCheck = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(item.Token);
                        if (fileToCheck != null)
                        {
                            if (Utilities.UtilityFunctions.AreFilesEqual(fileToCheck, file))
                            {
                                StorageApplicationPermissions.MostRecentlyUsedList.Remove(item.Token);
                                this.RecentFiles.Remove(item);
                                pdftron.Common.RecentlyUsedCache.RemoveDocument(item.DocumentPath);

                                RaisePropertyChanged("RecentItems");

                                return;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                LoadingSemaphore.Release();
            }
        }

        /// <summary>
        /// Removes the last entered item in the collection.
        /// </summary>
        public void RemoveLastItem()
        {
            if (_recentFiles.Count > 0)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(_recentFiles[0].Token);
                _recentFiles.RemoveAt(0);
            }
        }


        /// <summary>
        /// Clears the recent list
        /// </summary>
        public void ClearList()
        {
            // clear the cache
            foreach (RecentItem recentItem in _recentFiles)
            {
                pdftron.Common.RecentlyUsedCache.RemoveDocument(recentItem.DocumentPath);
            }
            StorageApplicationPermissions.MostRecentlyUsedList.Clear();
            _recentFiles.Clear();
        }

        /// <summary>
        /// Clears all StorageFiles in the recent list
        /// </summary>
        public void ClearFilesList()
        {
            // clear the cache
            foreach (RecentItem recentItem in _recentFiles)
            {
                if (!recentItem.Properties.IsFolder)
                {
                    pdftron.Common.RecentlyUsedCache.RemoveDocument(recentItem.DocumentPath);
                    // got a crash where MRU was removing a token in RecentFiles that it didn't contain. Protecting against it for now.
                    if (StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(recentItem.Token))
                    {
                        StorageApplicationPermissions.MostRecentlyUsedList.Remove(recentItem.Token);
                    }

                }
            }
            _recentFiles.Clear();
        }

        public async Task UpdatePageAsync(string token, int pageNumber)
        {
            await LoadingSemaphore.WaitAsync(1);
            try
            {
                if (StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count == 0)
                {
                    return;
                }
                AccessListEntry entry = StorageApplicationPermissions.MostRecentlyUsedList.Entries[0];
                if (!entry.Token.Equals(token))
                {
                    return;
                }

                string metadata = entry.Metadata;

                //string[] filePathAndFlags = entry.Metadata.Split('/');
                Regex pageNumberRegex = new Regex(@"/p\d+");
                string pageNumberString = "/p" + pageNumber;

                Match match = pageNumberRegex.Match(metadata);
                if (match.Success)
                {
                    metadata = pageNumberRegex.Replace(metadata, pageNumberString);
                }
                else
                {
                    metadata += pageNumberString;
                }

                entry.Metadata = metadata;
                StorageFile fileToUpdate = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token);
                string token2 = StorageApplicationPermissions.MostRecentlyUsedList.Add(fileToUpdate, metadata);
            }
            catch (Exception) { }
            finally
            {
                LoadingSemaphore.Release();
            }
        }

        public async Task UpdatePropertiesAsync(string token, pdftron.PDF.PDFViewCtrl ctrl, bool isReflow)
        {
            await LoadingSemaphore.WaitAsync();
            try
            {
                if (StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count == 0)
                {
                    return;
                }

                AccessListEntry entry = StorageApplicationPermissions.MostRecentlyUsedList.Entries[0];
                if (!entry.Token.Equals(token))
                {
                    return;
                }

                string metadata = entry.Metadata;
                RecentDocumentProperties properties = RecentDocumentProperties.FromString(metadata);
                properties.PagePresentationMode = ctrl.GetPagePresentationMode();
                properties.PageRotation = ctrl.GetRotation();
                properties.PageNumber = ctrl.GetCurrentPage();
                properties.Zoom = ctrl.GetZoom();
                properties.HorizontalScrollPos = (int)ctrl.GetHScrollPos();
                properties.VerticalScrollPos = (int)ctrl.GetVScrollPos();
                properties.IsInReflowMode = isReflow;

                metadata = properties.MakeString();

                entry.Metadata = metadata;
                StorageFile fileToUpdate = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token);
                string token2 = StorageApplicationPermissions.MostRecentlyUsedList.Add(fileToUpdate, metadata);

                if (!string.IsNullOrWhiteSpace(token))
                {
                    foreach (RecentItem item in _recentFiles)
                    {
                        if (token.Equals(item.Token))
                        {
                            item.Properties = properties;
                        }
                    }

                }
            }
            catch (Exception) { }
            finally
            {
                LoadingSemaphore.Release();
            }
        }

        public int GetPageNumberFromToken(string token)
        {
            if (StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count == 0)
            {
                return -1;
            }
            AccessListEntry entry = StorageApplicationPermissions.MostRecentlyUsedList.Entries[0];
            if (!entry.Token.Equals(token))
            {
                return -1;
            }

            string metadata = entry.Metadata;

            return GetPageNumberFromMetadataString(entry.Metadata);
        }

        public async Task<RecentDocumentProperties> GetPropertiesForFileIfInList(StorageFile file)
        {
            await LoadingSemaphore.WaitAsync(1);
            try
            {
                foreach (RecentItem item in _recentFiles)
                {
                    try
                    {
                        StorageFile fileToCheck = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(item.Token);
                        if (fileToCheck != null)
                        {
                            if (Utilities.UtilityFunctions.AreFilesEqual(fileToCheck, file))
                            {
                                return item.Properties;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                LoadingSemaphore.Release();
            }
            return null;
        }

        #endregion List Management


        #region Utility Functions

        /// <summary>
        /// Returns the index of the list item which contains token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private int FindTokenInList(string token)
        {
            int listSize = this.RecentFiles.Count;
            for (int i = 0; i < listSize; i++)
            {
                RecentItem item = RecentFiles[i];
                if (item.Token.Equals(token, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        private bool FindOptionInFlags(string option, string[] flags)
        {
            int numOptions = flags.Length;
            for (int i = 1; i < numOptions; i++)
            {
                if (flags[i].Equals(option, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private int GetPageNumberFromMetadataString(string metadata)
        {
            int result = -1;

            try
            {
                Regex pageNumberRegex = new Regex(@"/p\d+");
                Match match = pageNumberRegex.Match(metadata);

                if (match.Success)
                {
                    Regex justNMumberRegex = new Regex(@"\d+");
                    Match pgMatch = justNMumberRegex.Match(match.Groups[0].ToString());
                    if (pgMatch.Success)
                    {
                        Int32.TryParse(pgMatch.Groups[0].ToString(), out result);
                    }
                }
            }
            catch (Exception)
            {

            }
            return result;
        }

        #endregion Utility Functions
    }
    public class SelectedFileInfo
    {
        public SelectedFileInfo(string title, string author, string pageCount, string path, string fileSize, string lastModified)
        {
            this.Title = title;
            this.Author = author;
            this.PageCount = pageCount;
            this.Path = path;
            this.FileSize = fileSize;
            this.LastModified = lastModified;
            this.IsFolder = false;

        }
        public SelectedFileInfo(string path, int numFolders, int numPDFs, string lastModified)
        {
            this.Path = path;
            this.LastModified = lastModified;
            this.NumFolders = numFolders;
            this.NumPDFs = numPDFs;
            this.IsFolder = true;
        }

        public bool IsFolder { get; private set; }
        public string Title { get; set; }
        public string Author { get; set; }

        public string PageCount { get; set; }

        public string Path { get; set; }

        public string FileSize { get; set; }

        public string LastModified { get; set; }
        public int NumFolders { get; set; }
        public int NumPDFs { get; set; }

    }
}
