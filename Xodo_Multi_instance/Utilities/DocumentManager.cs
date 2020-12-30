using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace CompleteReader.Utilities
{
    /// <summary>
    /// DocumentManager is there to aid in creating temporary files for opening documents, and to make sure that these temporary documents work with the recent list.
    /// In short, the DocumentManager will generate temporary files and keeps a mapping of standard file paths to temporary file paths.
    /// </summary>
    public class DocumentManager
    {
        private static Dictionary<string, string> _FileNameMap;
        private static List<string> _OpenFiles;

        private static DocumentManager _Current = null;
        public static async Task<DocumentManager> GetInstanceAsync()
        {
            await LoadingSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_IsReady)
                {

                    if (_Current == null)
                    {
                        _Current = new DocumentManager();
                    }
                    if (_FileNameMap == null)
                    {
                        await LoadFileMappingAsync().ConfigureAwait(false);
                    }
                    if (_Current._Guard == null)
                    {
                        _Current._Guard = new SemaphoreSlim(1);
                    }
                    _IsReady = true;
                }
            }
            catch (Exception e) { System.Diagnostics.Debug.WriteLine("Error: " + e.ToString()); }
            finally
            {
                LoadingSemaphore.Release();
            }
            return _Current;
        }

        public static DocumentManager Instance
        {
            get
            {
                if (IsReady)
                {
                    return _Current;
                }
                return null;
            }
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

        private SemaphoreSlim _Guard;

        #region File Name Mapping Storage

        private const string FILE_NAME_MAP_FILE = "FileNameMapping.txt";
        public class KeyValuePair // We're using this since using the build in KeyValuePair<string, string> seems to serialize to null values
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public KeyValuePair() { }

            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        private static async Task LoadFileMappingAsync()
        {
            _FileNameMap = null;
            _OpenFiles = null;

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FILE_NAME_MAP_FILE, CreationCollisionOption.OpenIfExists);
                if (file != null)
                {
                    string dictString = await FileIO.ReadTextAsync(file);

                    _FileNameMap = DeSerializeFileDict(dictString);
                    _OpenFiles = new List<string>();
                    _TemporaryFilesFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(TEMP_FILE_FOLDER, CreationCollisionOption.OpenIfExists);
                    
                    _ = CleanTemporaryFolderAsync(); // Note: don't need to wait for it, just a cleanup
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating file dictionary: " + ex.ToString());
            }
            finally
            {

            }
        }

        private static async Task SaveFileMappingAsync()
        {
            if (_FileNameMap == null)
            {
                return;
            }

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(FILE_NAME_MAP_FILE);
                string dictString = SerializeFileDictionary(_FileNameMap);
                await FileIO.WriteTextAsync(file, dictString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error opening file dictionary: " + ex.ToString());
            }
        }

        private static string SerializeFileDictionary(Dictionary<string, string> fileDict)
        {
            try
            {
                List<KeyValuePair> tuples = new List<KeyValuePair>();
                foreach (KeyValuePair<string, string> keyValPair in fileDict)
                {
                    tuples.Add(new KeyValuePair(keyValPair.Key, keyValPair.Value));
                }
                XmlSerializer xmlIzer = new XmlSerializer(typeof(List<KeyValuePair>));
                System.IO.StringWriter writer = new System.IO.StringWriter();
                xmlIzer.Serialize(writer, tuples);
                return writer.ToString();
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
            }
            return string.Empty;
        }

        private static Dictionary<string, string> DeSerializeFileDict(string fileDictString)
        {
            if (!string.IsNullOrWhiteSpace(fileDictString))
            {
                Dictionary<string, string> fileDict = new Dictionary<string, string>();
                try
                {
                    List<KeyValuePair> tuples = new List<KeyValuePair>();
                    XmlSerializer xmlIzer = new XmlSerializer(typeof(List<KeyValuePair>));
                    using (System.IO.StringReader sr = new System.IO.StringReader(fileDictString))
                    {
                        tuples = (xmlIzer.Deserialize(sr)) as List<KeyValuePair>;
                        if (tuples != null)
                        {
                            foreach (KeyValuePair tuple in tuples)
                            {
                                fileDict[tuple.Key] = tuple.Value;
                            }
                            return fileDict;
                        }
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Debug.WriteLine(exc);
                }
            }
            return new Dictionary<string, string>();
        }

        #endregion File Name Mapping Storage

        /// <summary>
        /// Gets the path at which the document would have been stored in the recently used cache.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetRecentThumbnailPath(string fileName)
        {
            System.Diagnostics.Debug.Assert(!Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess);
            _Guard.Wait();

            try
            {
                if (_FileNameMap.ContainsKey(fileName))
                {
                    return _FileNameMap[fileName];
                }
            }
            catch (Exception) { }
            finally
            {
                _Guard.Release();
            }
            return null;
        }

        public async Task<string> GetRecentThumbnailPathAsync(string fileName)
        {
            await _Guard.WaitAsync();

            try
            {
                if (_FileNameMap.ContainsKey(fileName))
                {
                    return _FileNameMap[fileName];
                }
            }
            catch (Exception) { }
            finally
            {
                _Guard.Release();
            }
            return null;
        }


        /// <summary>
        /// Copies the file to the temporary folder and returns the new file
        /// The same file will always come back with the same path, provided it is in the recent list.
        /// </summary>
        /// <param name="file">The StorageFile to create a temp copy for</param>
        /// <param name="generateNew">Whether we should always create a new temp file, even if it's already in the list.</param>
        /// <returns></returns>
        public async Task<StorageFile> OpenTemporaryCopyAsync(StorageFile file, bool generateNew = false)
        {
            await _Guard.WaitAsync();
            try
            {
                return await OpenTempCopyAsync(file, generateNew);
            }
            catch (Exception) { }
            finally
            {
                _Guard.Release();
            }
            return null;
        }

        /// <summary>
        /// Call this when a StorageFile is no longer needed.
        /// </summary>
        /// <param name="file"></param>
        public void CloseFile(StorageFile file)
        {
            _Guard.Wait();

            try
            {
                if (!string.IsNullOrWhiteSpace(file.Path) && _OpenFiles.Contains(file.Path))
                {
                    _OpenFiles.Remove(file.Path);
                }
            }
            finally
            {
                _Guard.Release();
            }
        }

        public async Task AddChangesToOriginal(StorageFile original, StorageFile temporaryFile, bool isDropBox = false)
        {
            await _Guard.WaitAsync();
            Exception exc = null;
            try
            {
                await temporaryFile.CopyAndReplaceAsync(original);
            }
            catch (Exception e) { exc = e; }
            finally
            {
                _Guard.Release();
            }
            if (exc != null)
            {
                throw exc;
            }
        }


        #region Impl

        private const string TEMP_FILE_FOLDER = "OpenDocuments";

        private static StorageFolder _TemporaryFilesFolder;

        private static async Task<StorageFile> OpenTempCopyAsync(StorageFile file, bool generateNew)
        {
            if (!CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.IsReady)
            {
                return null;
            }
            StorageFile newFile = null;
            Tuple<string, string> tempFileName = GetFileName(file.Path, generateNew);
            StorageFolder tempFolder = null;
            try
            {
                tempFolder = await _TemporaryFilesFolder.CreateFolderAsync(tempFileName.Item1, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception) { }
            if (tempFolder == null)
            {
                try
                {
                    tempFolder = await _TemporaryFilesFolder.CreateFolderAsync(tempFileName.Item1, CreationCollisionOption.GenerateUniqueName);
                }
                catch (Exception) { }
            }
            if (tempFolder == null)
            {
                return null;
            }

            bool noTempFile = false;

            if (CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.ContainsFile(file))
            {
                try
                {
                    IStorageItem item = await tempFolder.TryGetItemAsync(tempFileName.Item2);
                    if (item != null && item is StorageFile)
                    {
                        newFile = item as StorageFile;
                    }
                    if (UtilityFunctions.AreFilesEqual(newFile, file))
                    {
                        newFile = null;
                        noTempFile = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            if (!noTempFile)
            {
                if (newFile == null)
                {
                    bool canShortenName = true;
                    string nameOfTempFile = tempFileName.Item2;
                    string ext = System.IO.Path.GetExtension(nameOfTempFile);
                    if (!ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        nameOfTempFile = System.IO.Path.GetFileNameWithoutExtension(nameOfTempFile) + ".pdf";
                    }
                    while (canShortenName)
                    {
                        try
                        {
                            newFile = await file.CopyAsync(tempFolder, nameOfTempFile, NameCollisionOption.ReplaceExisting);
                            canShortenName = false;
                        }
                        catch (System.IO.PathTooLongException)
                        {
                            nameOfTempFile = UtilityFunctions.ShortenFileName(nameOfTempFile);
                        }
                        catch (Exception ex)
                        {
                            canShortenName = false;
                            System.Diagnostics.Debug.WriteLine(ex);
                        }
                    }
                }

                if (newFile == null)
                {
                    bool canShortenName = true;
                    string nameOfTempFile = tempFileName.Item2;
                    while (canShortenName)
                    {
                        try
                        {
                            newFile = await file.CopyAsync(tempFolder, tempFileName.Item2, NameCollisionOption.GenerateUniqueName);
                            canShortenName = false;
                        }
                        catch (System.IO.PathTooLongException)
                        {
                            nameOfTempFile = UtilityFunctions.ShortenFileName(nameOfTempFile);
                        }
                        catch (Exception ex)
                        {
                            canShortenName = false;
                            AnalyticsHandler.CURRENT.SendEvent(ex.ToString());
                        }
                    }
                }
            }

            if (newFile != null)
            {
                if (!string.IsNullOrWhiteSpace(file.Path))
                {
                    try
                    {
                        await AddFileNameToListAsync(file.Path, newFile.Path).ConfigureAwait(false);
                    }
                    catch (Exception) { }
                    _OpenFiles.Add(file.Path);
                }
                return newFile;
            }
            else
            {
                if (!string.IsNullOrEmpty(file.Path))
                {
                    await AddFileNameToListAsync(file.Path, file.Path);
                }
            }

            return null;
        }

        private static Tuple<string, string> GetFileName(string fileName, bool generateNew)
        {
            if (!string.IsNullOrWhiteSpace(fileName) && !generateNew)
            {
                if (_FileNameMap.ContainsKey(fileName))
                {
                    string dirName = System.IO.Path.GetDirectoryName(_FileNameMap[fileName]);
                    string[] dirs = dirName.Split("\\".ToCharArray());
                    return new Tuple<string, string>(dirs[dirs.Length - 1], System.IO.Path.GetFileName(_FileNameMap[fileName]));
                }
            }

            
            Guid guid = Guid.NewGuid();
            return new Tuple<string, string>(guid.ToString(), System.IO.Path.GetFileName(fileName));
        }

        private static async Task AddFileNameToListAsync(string fileName, string tempFileName)
        {
            try
            {
                await CleanUpFileMappingList();
            }
            catch (Exception) { }

            _FileNameMap[fileName] = tempFileName;
            await SaveFileMappingAsync();
        }

        private static async Task CleanTemporaryFolderAsync()
        {
            if (!ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.IsReady)
                return;

            // Note: Keep all tasks in a list so we can run them in parallel
            List<Task> tasksCleanup = new List<Task>();

            try
            {
                IReadOnlyList<StorageFile> files = await _TemporaryFilesFolder.GetFilesAsync();

                //IList<StorageFile> tabFiles = await ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.GetOrGenerateTabFilesAsync();
                //CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel tabs = new ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel();
                //await tabs.LoadTabsAsync();

                foreach (StorageFile file in files)
                {
                    if (!ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.ContainsFile(file))
                    {
                        tasksCleanup.Add(Task.Run(() => file.DeleteAsync()));
                    }
                }
                IReadOnlyList<StorageFolder> folders = await _TemporaryFilesFolder.GetFoldersAsync();
                foreach (StorageFolder folder in folders)
                {
                    files = await folder.GetFilesAsync();
                    foreach (StorageFile file in files)
                    {
                        if (!ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.ContainsFile(file))
                        {
                            tasksCleanup.Add(Task.Run(() => folder.DeleteAsync()));
                        }
                    }
                }
            }
            catch (Exception) { }
                                
            // Note: here just wait for all parallel tasks to be completed and then we proceed.
            await Task.WhenAll(tasksCleanup);
        }

        private static async Task CleanUpFileMappingList()
        {
            if (!CompleteReader.Collections.RecentItemsData.IsReady || !ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.IsReady)
            {
                return;
            }
            //CompleteReader.Collections.RecentItemsData recentItems = await CompleteReader.Collections.RecentItemsData.GetItemSourceAsync();

            //IList<StorageFile> tabFiles = await ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.GetOrGenerateTabFilesAsync();
            //CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel tabs = new ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel();
            //await tabs.LoadTabsAsync();

            Dictionary<string, string> itemsToDelete = new Dictionary<string, string>();
            foreach (string key in _FileNameMap.Keys)
            {
                if (!ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.ContainsFilePath(key))
                {
                    itemsToDelete.Add(key, null);
                }
            }
            foreach (CompleteReader.Collections.RecentItem item in CompleteReader.Collections.RecentItemsData.Instance.RecentFiles)
            {
                if (_FileNameMap.ContainsKey(item.DocumentPath) || _OpenFiles.Contains(item.DocumentPath))
                {
                    if (itemsToDelete.ContainsKey(item.DocumentPath))
                    {
                        itemsToDelete.Remove(item.DocumentPath);
                    }
                }
            }
            foreach (string key in itemsToDelete.Keys)
            {
                _FileNameMap.Remove(key);
            }
        }

        #endregion Impl

    }
}
