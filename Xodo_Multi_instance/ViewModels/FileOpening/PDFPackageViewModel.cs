using CompleteReader.ViewModels.Common;
using pdftron.PDF;
using pdftron.SDF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Windows.Storage;

namespace CompleteReader.ViewModels.FileOpening
{
    public class PDFPackageItem
    {
        public string FileName { get; set; }
        public bool IsPDF { get; set; }

        public PDFPackageItem(string fileName, bool isPDF)
        {
            this.FileName = fileName;
            this.IsPDF = isPDF;
        }
    }

    public class PDFPackageViewModel : ViewModelBase
    {
        public PDFPackageViewModel(PDFDoc doc, string fileName)
        {
            InitCommands();
            PackageItems = new ObservableCollection<PDFPackageItem>();
            _DocumentStack = new List<PDFDoc>();
            _DocumentStack.Add(doc);
            DisplayFileName = fileName;
            PopulatePackageList();
        }

        private ObservableCollection<PDFPackageItem> _PackageItems;
        public ObservableCollection<PDFPackageItem> PackageItems
        {
            get { return _PackageItems; }
            set { Set(ref _PackageItems, value); }
        }

        private string _DisplayFileName = String.Empty;
        public string DisplayFileName 
        {
            get { return _DisplayFileName; }
            set { Set(ref _DisplayFileName, value); }
        }

        private List<PDFDoc> _DocumentStack;
        private bool _ErrorHappened;
        private string _ErrorMessage;

        private StorageFile _SelectedFile;

        public delegate void OnFileSelectedHandler(PDFDoc doc, StorageFile file, bool err, string errorMessage);
        public event OnFileSelectedHandler FileSelcted;

        #region Commands

        private void InitCommands()
        {
            ItemClickCommand = new RelayCommand(ItemClickCommandImpl);
            PackageDialogBackCommand = new RelayCommand(PackageDialogBackCommandImpl);
        }

        public RelayCommand ItemClickCommand { get; private set; }
        public RelayCommand PackageDialogBackCommand { get; private set; }

        private void ItemClickCommandImpl(object clickedItem)
        {
            PDFPackageItem item = clickedItem as PDFPackageItem;
            if (item.IsPDF)
            {
                OpenPDF(item);
            }
            else
            {
                OpenNonPDF(item);
            }
        }

        private void PackageDialogBackCommandImpl(object sender)
        {
            HandleBackButton();
        }


        #endregion Commands


        #region Impl

        private void PopulatePackageList()
        {
            PackageItems.Clear();
            IList<PDFPackageItem> items = GetPackageContent(_DocumentStack[_DocumentStack.Count - 1]);
            foreach (PDFPackageItem item in items)
            {
                PackageItems.Add(item);
            }
        }

        private IList<PDFPackageItem> GetPackageContent(PDFDoc doc)
        {
            List<PDFPackageItem> items = new List<PDFPackageItem>();

            NameTree files = NameTree.Find(doc.GetSDFDoc(), "EmbeddedFiles");
            if (files.IsValid())
            {
                NameTreeIterator nameTreeIterator = files.GetIterator();
                while (nameTreeIterator.HasNext())
                {
                    String entryName = nameTreeIterator.Key().GetAsPDFText();
                    bool isPDF = false;
                    if (HasExtension(entryName))
                    {
                        string extension = GetExtension(entryName);
                        if (extension.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            isPDF = true;
                        }
                    }
                    items.Add(new PDFPackageItem(entryName, isPDF));
                    nameTreeIterator.Next();
                }
            }
            return items;
        }

        private async System.Threading.Tasks.Task<StorageFile> ExtractFileFromPortFolio(PDFDoc doc, string fileName)
        {
            try
            {
                //StorageFolder folder = await Utilities.UtilityFunctions.GetTemporarySaveFilefolderAsync();
                //StorageFolder folder = await GetPDFPackageFolderAsync();
                NameTree files = NameTree.Find(doc.GetSDFDoc(), "EmbeddedFiles");
                if (files.IsValid())
                {
                    // Traverse the list of embedded files.
                    NameTreeIterator nameTreeIterator = files.GetIterator();
                    while (nameTreeIterator.HasNext())
                    {
                        String entryName = nameTreeIterator.Key().GetAsPDFText();
                        if (entryName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            pdftron.PDF.FileSpec fileSpec = new pdftron.PDF.FileSpec(nameTreeIterator.Value());
                            pdftron.Filters.IFilter filter = fileSpec.GetFileData();
                            if (filter != null)
                            {
                                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("<\\d*>");

                                string tagFreeFileName = regex.Replace(fileName, "");
                                string cleanFileName = Utilities.UtilityFunctions.SanitizeFileName(tagFreeFileName);
                                _SelectedFile = await Utilities.UtilityFunctions.GetTemporarySaveFileAsync(cleanFileName);
                                //string newFileName = System.IO.Path.Combine(folder.Path, cleanFileName);
                                string newFileName = _SelectedFile.Path;
                                filter.WriteToFile(newFileName, false);
                                //_SelectedFile = await folder.GetFileAsync(cleanFileName);
                                //PDFDoc newDoc = new PDFDoc(_SelectedFile);
                                return _SelectedFile;
                            }
                        }
                        nameTreeIterator.Next();
                    }
                }
            }
            catch (Exception)
            {
                _ErrorHappened = true;

                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                _ErrorMessage = string.Format(loader.GetString("PackageDialog_FileExtractionError_Info"), Settings.Settings.DisplayName);
            }
            return null;
        }

        private async System.Threading.Tasks.Task<Windows.Storage.StorageFile> ExtractNonPDFDocFromPortFolio(PDFDoc doc, string fileName)
        {
            try
            {
                StorageFolder folder = await GetPDFPackageFolderAsync();
                NameTree files = NameTree.Find(doc.GetSDFDoc(), "EmbeddedFiles");
                if (files.IsValid())
                {
                    // Traverse the list of embedded files.
                    NameTreeIterator nameTreeIterator = files.GetIterator();
                    while (nameTreeIterator.HasNext())
                    {
                        String entryName = nameTreeIterator.Key().GetAsPDFText();
                        if (entryName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            pdftron.PDF.FileSpec fileSpec = new pdftron.PDF.FileSpec(nameTreeIterator.Value());
                            pdftron.Filters.IFilter filter = fileSpec.GetFileData();
                            if (filter != null)
                            {
                                string newFileName = "tempfile." + GetExtension(fileName);
                                string newFullName = System.IO.Path.Combine(folder.Path, newFileName);
                                filter.WriteToFile(newFullName, false);
                                Windows.Storage.StorageFile file = await folder.GetFileAsync(newFileName);
                                return file;
                            }
                        }
                        nameTreeIterator.Next();
                    }
                }
            }
            catch (Exception)
            {
                _ErrorHappened = true;
                _ErrorMessage = "Sorry, " + Settings.Settings.DisplayName + " failed to extract the selected file.";
            }
            return null;
        }

        private async void OpenPDF(PDFPackageItem item)
        {
            StorageFile file = await ExtractFileFromPortFolio(_DocumentStack[_DocumentStack.Count - 1], item.FileName);
            PDFDoc newDoc = null;
            try
            {
                newDoc = new PDFDoc(file);
            }
            catch (Exception) { }
            Obj collectionObj = null;
            if (newDoc != null)
            {
                collectionObj = newDoc.GetRoot().FindObj("Collection");
            }
            if (collectionObj != null)
            {
                _DocumentStack.Add(newDoc);
                PopulatePackageList();
            }
            else if (FileSelcted != null)
            {
                FileSelcted(newDoc, _SelectedFile, _ErrorHappened, _ErrorMessage);
            }
        }

        private async void OpenNonPDF(PDFPackageItem item)
        {
            bool success = false;
            StorageFile file = await ExtractFileFromPortFolio(_DocumentStack[_DocumentStack.Count - 1], item.FileName);
            if (file != null)
            {
                string xpsExtension = ".oxps";
                string fileExtension = System.IO.Path.GetExtension(file.DisplayName);
                if (xpsExtension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    if (FileSelcted != null)
                    {
                        FileSelcted(null, file, _ErrorHappened, _ErrorMessage);
                    }
                }
                else
                {
                    try
                    {
                        success = await Windows.System.Launcher.LaunchFileAsync(file);
                    }
                    catch (Exception) { }
                }
            }

            if (!success)
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

                Windows.UI.Popups.MessageDialog md = new Windows.UI.Popups.MessageDialog(string.Format(loader.GetString("PackageDialog_FailedToLaunchNonPDF_Info"), item.FileName));
                await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
            }
        }

        private void HandleBackButton()
        {
            if (FileSelcted != null)
            {
                FileSelcted(null, null, false, null);
            }
        }

        #region Utility Funcions


        /// <summary>
        /// Because the path might not be valid, we need to implement our own check for this
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool HasExtension(string fileName)
        {
            if (fileName.Contains("."))
            {
                return true;
            }
            return false;
        }

        private string GetExtension(string fileName)
        {
            if (HasExtension(fileName))
            {
                string[] pathParts = fileName.Split('.');
                if (pathParts.Length > 0)
                {
                    return pathParts[pathParts.Length - 1];
                }
            }
            return null;
        }

        private async System.Threading.Tasks.Task<StorageFolder> GetPDFPackageFolderAsync()
        {
            StorageFolder folder = null;
            IStorageItem item = await Windows.Storage.ApplicationData.Current.TemporaryFolder.TryGetItemAsync("PDFPackageFolder");
            if (item == null)
            {
                folder = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFolderAsync("PDFPackageFolder", CreationCollisionOption.ReplaceExisting);
            }
            else
            {
                folder = item as StorageFolder;
            }
            return folder;
        }


        #endregion Utility functions

        #endregion Impl


        #region Back Button

        public override bool GoBack()
        {
            HandleBackButton();
            return true;
        }

        #endregion Back button

    }

}
