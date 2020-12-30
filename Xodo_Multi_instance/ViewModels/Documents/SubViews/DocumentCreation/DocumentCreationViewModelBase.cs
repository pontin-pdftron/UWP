using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompleteReader.ViewModels.Common;
using Windows.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using pdftron.PDF;
using Windows.Storage;
using Windows.UI.Popups;

namespace CompleteReader.ViewModels.DocumentsPage
{
    class DocumentCreationViewModelBase : ViewModelBase
    {
        private static Regex containsABadCharacter = 
            new Regex("[" + Regex.Escape(new string(System.IO.Path.GetInvalidPathChars())) + "]");

        // Page sizes from Wikipedia
        protected const double A0_Width = 2383.2;
        protected const double A0_Height = 3369.6;
        protected const double A1_Width = 1684.8;
        protected const double A1_Height = 2383.2;
        protected const double A2_Width = 1188.0;
        protected const double A2_Height = 1684.8;
        protected const double A3_Width = 842.4;
        protected const double A3_Height = 1188.0;
        protected const double A4_Width = 595.44;
        protected const double A4_Height = 842.4;
        protected const double A5_Width = 419.76;
        protected const double A5_Height = 595.44;

        protected const double B0_Width = 2835.8;
        protected const double B0_Height = 4010.4;
        protected const double B1_Width = 2005.2;
        protected const double B1_Height = 2835.8;
        protected const double B2_Width = 1417.9;
        protected const double B2_Height = 2005.2;
        protected const double B3_Width = 1002.6;
        protected const double B3_Height = 1417.9;
        protected const double B4_Width = 708.9;
        protected const double B4_Height = 1002.6;
        protected const double B5_Width = 499.0;
        protected const double B5_Height = 708.9;

        protected const double Letter_Width = 612.0;
        protected const double Letter_Height = 792.0;
        protected const double Legal_Width = 612.0;
        protected const double Legal_Height = 1008;

        protected const double _inConversionRate = 1 / 72.0;
        protected const double _mmConversionRate = 0.3528; 

        private static Dictionary<PaperSizes, Tuple<double, double>> _PaperSizeMap;
        protected static Dictionary<PaperSizes, Tuple<double, double>> PaperSizeMap
        {
            get 
            {
                if (_PaperSizeMap == null)
                {
                    _PaperSizeMap = new Dictionary<PaperSizes,Tuple<double,double>>();
                    _PaperSizeMap[PaperSizes.Legal] = new Tuple<double,double>(Legal_Width, Legal_Height);
                    _PaperSizeMap[PaperSizes.Letter] = new Tuple<double,double>(Letter_Width, Letter_Height);
                    _PaperSizeMap[PaperSizes.A0] = new Tuple<double,double>(A0_Width, A0_Height);
                    _PaperSizeMap[PaperSizes.A1] = new Tuple<double,double>(A1_Width, A1_Height);
                    _PaperSizeMap[PaperSizes.A2] = new Tuple<double,double>(A2_Width, A2_Height);
                    _PaperSizeMap[PaperSizes.A3] = new Tuple<double,double>(A3_Width, A3_Height);
                    _PaperSizeMap[PaperSizes.A4] = new Tuple<double,double>(A4_Width, A4_Height);
                    _PaperSizeMap[PaperSizes.A5] = new Tuple<double,double>(A5_Width, A5_Height);
                    _PaperSizeMap[PaperSizes.B0] = new Tuple<double,double>(B0_Width, B0_Height);
                    _PaperSizeMap[PaperSizes.B1] = new Tuple<double,double>(B1_Width, B1_Height);
                    _PaperSizeMap[PaperSizes.B2] = new Tuple<double,double>(B2_Width, B2_Height);
                    _PaperSizeMap[PaperSizes.B3] = new Tuple<double,double>(B3_Width, B3_Height);
                    _PaperSizeMap[PaperSizes.B4] = new Tuple<double,double>(B4_Width, B4_Height);
                    _PaperSizeMap[PaperSizes.B5] = new Tuple<double,double>(B5_Width, B5_Height);
                }
                return _PaperSizeMap;
            }
        }

        public enum PaperSizes
        {
            Letter,
            Legal,
            A0,
            A1,
            A2,
            A3,
            A4,
            A5,
            B0,
            B1,
            B2,
            B3,
            B4,
            B5,
        }

        public enum PageOrientations
        {
            Portrait,
            Landscape,
        }

        public bool Cancel { get; set; }

        public DocumentCreationViewModelBase()
        {
            Cancel = false;
            TitleTextChangedCommand = new RelayCommand(TitleTextChangedCommandImpl);

            CreateNewDocument = new RelayCommand(CreateNewDocumentImpl);
        }


        #region Events

        public delegate void NewDocumentCreatedDelegate(StorageFile file);

        public event NewDocumentCreatedDelegate NewDocumentCreated;

        #endregion Events


        #region Commands

        public RelayCommand TitleTextChangedCommand { get; private set; }

        public RelayCommand CreateNewDocument { get; private set; }

        public void TitleTextChangedCommandImpl(object sender)
        {
            string title = sender as string;
            if (title != null)
            {
                DocumentTitle = title;
            }
        }

        public void CreateNewDocumentImpl(object sender)
        {
            CreateDocument();
        }

        #endregion Commands


        #region Properties

        

        private string _DocumentTitle = "";
        public string DocumentTitle
        {
            get { return _DocumentTitle; }
            set
            {
                if (!string.Equals(_DocumentTitle, value))
                {
                    _DocumentTitle = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("IsDocumentTitleValid");
                    RaisePropertyChanged("IsInputValid");
                }
            }
        }

        public bool IsDocumentTitleValid
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(GetFileName()))
                {
                    if (!containsABadCharacter.IsMatch(GetFileName()))
                    { 
                        return true; 
                    }
                }

                return false;
            }
        }

        public string DefaultDocumentName
        {
            get
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                return loader.GetString("DocumentCreation_AllDocuments_DefaultTitle");
            }
        }

        public virtual bool IsInputValid
        {
            get
            {
                return IsDocumentTitleValid && CheckValidity();
            }
        }

        /// <summary>
        /// Override this to do your own checks
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckValidity()
        {
            return true;
        }

        protected bool _WorkInProgress = false;
        public bool WorkInProgress
        {
            get { return _WorkInProgress; }
            set
            {
                if (_WorkInProgress != value)
                {
                    _WorkInProgress = value;
                    RaisePropertyChanged();
                }
            }
        }

        protected virtual MessageDialog GetErrorMessageDialog()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            
            return new MessageDialog(loader.GetString("DocumentCreation_AllDocuments_CreationFailed_Content"),
                loader.GetString("DocumentCreation_AllDocuments_CreationFailed_Title"));
        }

        #endregion Properties


        /// <summary>
        /// Override this in order to create a PDFDoc
        /// </summary>
        /// <returns></returns>
        virtual protected Task<PDFDoc> GetPDFDocAsync()
        {
            return null;
        }



        private async void CreateDocument()
        {
            bool success = false;
            StorageFile saveFile = null;
            PDFDoc doc = null;
            try
            {
                doc = await GetPDFDocAsync();
                if (Cancel)
                {
                    return;
                }
                if (doc == null)
                {
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(GetErrorMessageDialog());
                    return;
                }
                saveFile = await Utilities.UtilityFunctions.SaveToTemporaryFileAsync(doc, GetFileNameWithExtension());
                doc.GetDocInfo().SetTitle(GetFileName());
                if (Cancel)
                {
                    return;
                }

                success = true;
            }
            catch (Exception)
            {

            }

            if (saveFile != null)
            {
                if (success)
                {
                    if (NewDocumentCreated != null)
                    {
                        NewDocumentCreated(saveFile);
                    }

                }
                else
                {
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(GetErrorMessageDialog());
                }
            }
        }

        private async Task<StorageFile> PickSaveFileAsync()
        {
            StorageFile theFile = null;

            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            Windows.Storage.Pickers.FileSavePicker fileSavePicker = new Windows.Storage.Pickers.FileSavePicker();
            fileSavePicker.CommitButtonText = loader.GetString("DocumentCreation_AllDocuments_CreateDocument_PickerConfirm");
            fileSavePicker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
            fileSavePicker.SuggestedFileName = GetFileName();

            theFile = await fileSavePicker.PickSaveFileAsync();

            return theFile;
        }

        private string GetFileName()
        {
            if (_DocumentTitle.Length == 0)
            {
                return DefaultDocumentName;
            }
            return _DocumentTitle;
        }

        private string GetFileNameWithExtension()
        {
            string fileName = GetFileName();
            if (fileName.Length < 4)
            {
                fileName = fileName + ".pdf";
            }
            string substr = fileName.Substring(fileName.Length - 4);
            if (!fileName.Substring(fileName.Length - 4).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName + ".pdf";
            }
            return fileName;
        }

        protected void GetPaperDimensionsFromSize(PaperSizes size, out double width, out double height)
        {
            width = PaperSizeMap[size].Item1;
            height = PaperSizeMap[size].Item2;
        }
    }
}
