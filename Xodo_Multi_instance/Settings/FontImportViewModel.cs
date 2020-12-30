using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace CompleteReader.Settings
{
    public class FontItem : ViewModelBase
    {
        private StorageFile _FontFile = null;
        public StorageFile FontFile
        {
            get { return _FontFile; }
            internal set
            {
                if (Set(ref _FontFile, value))
                {
                    RaisePropertyChanged(FontName);
                }
            }
        }

        public string FontName
        {
             get { return _FontFile.Name; }
        }

        internal FontItem(StorageFile file)
        {
            FontFile = file;
        }
    }

    public class FontImportViewModel : ViewModelBase
    {
        public FontImportViewModel()
        {
            InitCommands();
            _FontItems = new ObservableCollection<FontItem>();
            _SelectedItems = new List<FontItem>();
            InitFonts();
        }

        public event Windows.UI.Xaml.RoutedEventHandler ClearSelectionRequested;

        #region Properites

        private ObservableCollection<FontItem> _FontItems = null;
        public ObservableCollection<FontItem> FontItems { get { return _FontItems; } }

        private bool _IsLoading = false;
        public bool IsLoading
        {
            get { return _IsLoading; }
            set { Set(ref _IsLoading, value); }
        }

        private bool _IsReady = false;
        public bool IsReady
        {
            get { return _IsReady; }
            private set
            {
                if (Set(ref _IsReady, value))
                {
                    RaisePropertyChanged("IsEmpty");
                }
            }
        }

        private bool _ErrorLoadingFonts = false;
        public bool ErrorLoadingFonts
        {
            get { return _ErrorLoadingFonts; }
            internal set { Set(ref _ErrorLoadingFonts, value); }
        }

        private bool _HasSelection = false;
        public bool HasSelection
        {
            get { return _HasSelection; }
            private set { Set(ref _HasSelection, value); }
        }

        private bool _IsWorking = false;
        public bool IsWorking
        {
            get { return _IsWorking; }
            set { Set(ref _IsWorking, value); }
        }

        public bool IsEmpty
        {
            get { return IsReady && _FontItems != null && _FontItems.Count == 0; }
        }

        #endregion Properties


        #region Commands

        private void InitCommands()
        {
            InitCommand = new RelayCommand(InitCommandImpl);
            BrowseFileCommand = new RelayCommand(BrowseFileCommandImpl);
            BroseFolderCommand = new RelayCommand(BrowseFolderCommandImpl);
            RemoveFontsCommand = new RelayCommand(RemoveFontsCommandImpl);
            FontItemsSelectionChangedCommand = new RelayCommand(FontItemsSelectionChangedCommandImpl);
        }

        public RelayCommand InitCommand { get; private set; }
        public RelayCommand BrowseFileCommand { get; private set; }
        public RelayCommand BroseFolderCommand { get; private set; }
        public RelayCommand RemoveFontsCommand { get; private set; }
        public RelayCommand FontItemsSelectionChangedCommand { get; private set; }

        private void InitCommandImpl(object parameter)
        {
            if (ErrorLoadingFonts)
            {
                InitFonts();
            }
        }

        private void BrowseFileCommandImpl(object parameter)
        {
            BrowseFile();
        }

        private void BrowseFolderCommandImpl(object parameter)
        {
            BrowseFolder();
        }

        private void RemoveFontsCommandImpl(object parameter)
        {
            RemoveSelectedFonts();
        }
        private void FontItemsSelectionChangedCommandImpl(object changeArgs)
        {
            SelectionChangedEventArgs args = changeArgs as SelectionChangedEventArgs;
            if (args != null)
            {
                foreach (FontItem item in args.RemovedItems)
                {
                    System.Diagnostics.Debug.Assert(_SelectedItems.Contains(item));
                    if (_SelectedItems.Contains(item))
                    {
                        _SelectedItems.Remove(item);
                    }
                }
                foreach (FontItem item in args.AddedItems)
                {
                    System.Diagnostics.Debug.Assert(!_SelectedItems.Contains(item));
                    if (!_SelectedItems.Contains(item))
                    {
                        _SelectedItems.Add(item);
                    }
                }

                HasSelection = _SelectedItems.Count > 0;
            }
        }

        #endregion Commands


        #region Impl

        private StorageFolder _FontFolder = null;
        private List<FontItem> _SelectedItems = null;

        private async void InitFonts()
        {
            try
            {
                ErrorLoadingFonts = false;
                IsLoading = true;
                IsWorking = true;
                if (_FontFolder == null)
                {
                    _FontFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(Settings.FONT_DIRECTORY, CreationCollisionOption.OpenIfExists);
                }
                IReadOnlyList<StorageFile> allFiles = await _FontFolder.GetFilesAsync();
                List<FontItem> fontFiles = new List<FontItem>();

                foreach (StorageFile file in allFiles)
                {
                    fontFiles.Add(new FontItem(file));
                }

                foreach (FontItem fontItem in fontFiles)
                {
                    FontItems.Add(fontItem);
                }

                IsReady = true;
            }
            catch (Exception) { }
            finally
            {
                IsWorking = false;
                IsLoading = false;
                ErrorLoadingFonts = !IsReady;
            }
        }

        private bool _FilePickerOpen = false;
        private async void BrowseFile()
        {
            if (_FilePickerOpen)
            {
                return;
            }
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.ViewMode = PickerViewMode.List;
            StorageFile file = null;
            foreach (string fileType in Settings.FontFileTypes)
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

            try
            {
                if (file != null)
                {
                    IsWorking = true;
                    ClearSelection();
                    IStorageItem existing = await _FontFolder.TryGetItemAsync(file.Name);
                    if (existing != null)
                    {
                        Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        MessageDialog md = new MessageDialog(string.Format(loader.GetString("Settings_Options_Font_FileAlreadyExists"), existing.Name));
                        UICommand yesCommand = new UICommand(loader.GetString("Settings_Options_Font_FileAlreadyExists_Replace"), (s) => { });
                        md.Commands.Add(yesCommand);
                        md.Commands.Add(new UICommand(loader.GetString("Settings_Options_Font_FileAlreadyExists_KeepOriginal"), (s) => { }));
                        IUICommand command = await MessageDialogHelper.ShowMessageDialogAsync(md);
                        if (command != null && command.Equals(yesCommand))
                        {
                            await ReplaceFontFileAsync(existing, file);
                        }
                    }
                    else
                    {
                        await AddFontFileAsync(file);
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                IsWorking = false;
            }
        }

        private async void BrowseFolder()
        {
            if (_FilePickerOpen)
            {
                return;
            }
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.ViewMode = PickerViewMode.List;
            StorageFolder folder = null;
            foreach (string fileType in Settings.FontFileTypes)
            {
                folderPicker.FileTypeFilter.Add(fileType);
            }
            try
            {
                _FilePickerOpen = true;
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
                try
                {
                    ClearSelection();
                    IsWorking = true;
                    List<StorageFile> files = await GetFontFilesAsync(folder);
                    bool replaceAll = false;
                    foreach (StorageFile file in files)
                    {
                        IStorageItem existing = await _FontFolder.TryGetItemAsync(file.Name);
                        if (existing != null)
                        {
                            bool shouldDelete = replaceAll;
                            if (!shouldDelete)
                            {
                                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                                MessageDialog md = new MessageDialog(string.Format(loader.GetString("Settings_Options_Font_FileAlreadyExists"), existing.Name));
                                UICommand yesCommand = new UICommand(loader.GetString("GenericStrings_Yes"), (s) => { });
                                md.Commands.Add(yesCommand);
                                UICommand yesAllCommand = new UICommand(loader.GetString("Settings_Options_Font_YesToAll"), (s) => { });
                                md.Commands.Add(yesAllCommand);
                                if (UtilityFunctions.GetDeviceFormFactorType() != UtilityFunctions.DeviceFormFactorType.Phone)
                                {
                                    md.Commands.Add(new UICommand(loader.GetString("DocumentsPage_RecentItems_ClearDialog_Cancel_Option"), (s) => { }));
                                }
                                IUICommand command = await MessageDialogHelper.ShowMessageDialogAsync(md);
                                if (command != null && command.Equals(yesCommand))
                                {
                                    shouldDelete = true;
                                }
                                else if (command != null && command.Equals(yesAllCommand))
                                {
                                    shouldDelete = true;
                                    replaceAll = true;
                                }
                            }
                            if (shouldDelete)
                            {
                                await ReplaceFontFileAsync(existing, file);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            await AddFontFileAsync(file);
                        }
                    }
                }
                catch (Exception) { }
                finally
                {
                    IsWorking = false;
                }
            }
        }

        private async Task ReplaceFontFileAsync(IStorageItem old, StorageFile newFile)
        {
            try
            {
                FontItem itemToRemove = null;
                foreach (FontItem item in FontItems)
                {
                    if (item.FontName.Equals(newFile.Name))
                    {
                        itemToRemove = item;
                        break;
                    }
                }
                FontItems.Remove(itemToRemove);
                await old.DeleteAsync(StorageDeleteOption.Default);
                await AddFontFileAsync(newFile);
            }
            catch (Exception) { }
        }

        private async Task AddFontFileAsync(StorageFile file)
        {
            try
            {
                StorageFile fontFile = await file.CopyAsync(_FontFolder, file.Name, NameCollisionOption.ReplaceExisting);
                FontItem fontItem = new FontItem(fontFile);
                int insertionIndex = 0;
                foreach (FontItem item in FontItems)
                {
                    if (fontItem.FontName.CompareTo(item.FontName) < 0)
                    {
                        break;
                    }
                    ++insertionIndex;
                }
                FontItems.Insert(insertionIndex, fontItem);
                RaisePropertyChanged("IsEmpty");
            }
            catch (Exception) { }
        }

        public async Task<List<StorageFile>> GetFontFilesAsync(StorageFolder folder)
        {
            Windows.Storage.Search.CommonFileQuery query = Windows.Storage.Search.CommonFileQuery.OrderByName;
            List<StorageFile> files = new List<StorageFile>();

            IReadOnlyList<StorageFile> sortedFiles = await folder.GetFilesAsync(query);
            foreach (StorageFile file in sortedFiles)
            {
                if (!string.IsNullOrEmpty(file.FileType) && Settings.FontFileTypes.Contains(file.FileType))
                {
                    files.Add(file);
                }
            }
            return files;
        }

        private void ClearSelection()
        {
            ClearSelectionRequested(this, new Windows.UI.Xaml.RoutedEventArgs());
        }

        private async void RemoveSelectedFonts()
        {
            try
            {
                List<FontItem> itemsToRemove = new List<FontItem>();
                itemsToRemove.AddRange(_SelectedItems);
                ClearSelection();
                foreach (FontItem item in itemsToRemove)
                {
                    if (FontItems.Contains(item))
                    {
                        FontItems.Remove(item);
                    }
                    await item.FontFile.DeleteAsync();
                }

                HasSelection = false;
            }
            catch (Exception) { }
        }

        #endregion Impl
    }
}
