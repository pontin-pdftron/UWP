using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using CompleteReader.ViewModels.Common;
using System.Collections.ObjectModel;

namespace CompleteReader.ViewModels.DocumentsPage
{
    class BlankDocumentCreationViewModel : DocumentCreationViewModelBase
    {
        public class PaperSizeHolder
        {
            public PaperSizes PaperSize { get; private set; }
            private double _Width = 0;
            public double Width { get { return _Width; } }
            private double _Height = 0;
            public double Height { get { return _Height; } }
            private string _Units = "";

            public override string ToString()
            {
                return string.Format("{0} ({1:0.00} x {2:0.00} {3})", PaperSize, _Width, _Height, _Units);
            }

            public PaperSizeHolder(PaperSizes size, double w, double h, string units)
            {
                PaperSize = size;
                _Width = w;
                _Height = h;
                _Units = units;
            }
        }

        private const int MAX_PAGE_COUNT = 50;

        public BlankDocumentCreationViewModel()
            : base()
        {
            PageNumberTextChangedCommand = new RelayCommand(PageNumberTextChangedCommandImpl);
            PageNumberTextLostFocusCommand = new RelayCommand(PageNumberTextLostFocusCommandImpl);
            IncrementOrDecrementCommand = new RelayCommand(IncrementOrDecrementCommandImpl);

            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            PaperSizes regionSize = GetRegionDefaultPageSize();
            double convRate = _inConversionRate;
            string units = loader.GetString("DocumentCreation_AllDocuments_UnitsInch");
            if (regionSize == PaperSizes.A4)
            {
                convRate = _mmConversionRate;
                units = loader.GetString("DocumentCreation_AllDocuments_UnitsMilliMeter");
            }

            foreach (PaperSizes size in (PaperSizes[])Enum.GetValues(typeof(PaperSizes)))
            {
                double width = 0;
                double height = 0;
                GetPaperDimensionsFromSize(size, out height, out width);
                PaperSizeHolder psh = new PaperSizeHolder(size, width * convRate, height * convRate, units);
                AvailablePaperSizes.Add(psh);
                if (size == regionSize)
                {
                    SelectedItem = psh;
                }
            }
        }

        #region Commands

        public RelayCommand PageNumberTextChangedCommand { get; private set; }
        public RelayCommand PageNumberTextLostFocusCommand { get; private set; }
        public RelayCommand IncrementOrDecrementCommand { get; private set; }

        private void PageNumberTextChangedCommandImpl(object sender)
        {
            string numPages = sender as string;
            if (numPages != null)
            {
                TotalPagesString = numPages;
            }
        }

        private void PageNumberTextLostFocusCommandImpl(object sender)
        {
            TotalPagesString = GetStringWithinRange(TotalPagesString);
        }

        private void IncrementOrDecrementCommandImpl(object plusOrMinus)
        {
            string sign = plusOrMinus as string;
            if (!string.IsNullOrEmpty(sign))
            {
                int pgNum = GetValidPageNumberFromString(TotalPagesString);
                if (sign.Equals("+") && pgNum < MAX_PAGE_COUNT)
                {
                    TotalPagesString = "" + (pgNum + 1);
                }
                else if (sign.Equals("-") && pgNum > 1)
                {
                    TotalPagesString = "" + (pgNum - 1);
                }
            }
        }

        #endregion Commands

        #region Properties

        private ObservableCollection<PaperSizeHolder> _AvailablePaperSizes = new ObservableCollection<PaperSizeHolder>();
        public ObservableCollection<PaperSizeHolder> AvailablePaperSizes
        {
            get { return _AvailablePaperSizes; }
            private set
            {
                _AvailablePaperSizes = value;
                RaisePropertyChanged();
            }
        }

        public PaperSizeHolder _SelectedItem = null;
        public PaperSizeHolder SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                if (value != _SelectedItem)
                {
                    _SelectedItem = value;
                    RaisePropertyChanged();
                }
            }
        }


        private PageOrientations _PageOrientation = PageOrientations.Portrait;
        public PageOrientations PageOrientation
        {
            get { return _PageOrientation; }
            set
            {
                if (value != _PageOrientation)
                {
                    _PageOrientation = value;
                    RaisePropertyChanged();
                }
            }
        }

        public Int32 PageOrientationIndex
        {
            get { return (Int32)_PageOrientation; }
            set 
            {
                if (value > 0)
                {
                    PageOrientation = (PageOrientations)value;
                }
            }
        }

        private string _TotalPagesString = "1";
        public string TotalPagesString
        {
            get { return _TotalPagesString; }
            set
            {
                if (!string.Equals(_TotalPagesString, value))
                {
                    _TotalPagesString = value;

                    RaisePropertyChanged();
                    RaisePropertyChanged("IsPageNumberStringValid");
                    RaisePropertyChanged("IsInputValid");
                }
            }
        }

        public bool IsPageNumberStringValid
        {
            get
            {
                // check new string
                Int32 numPages = GetValidPageNumberFromString(TotalPagesString);
                return numPages > 0;
            }
        }

        public string PageNumberPlaceholderText
        {
            get
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                return string.Format(loader.GetString("DocumentCreation_BlankDocument_NumberOfPages"), MAX_PAGE_COUNT);
            }
        }

        protected override bool CheckValidity()
        {
            return IsPageNumberStringValid;
        }

        #endregion Properties

        #region Utility Functions

        private Int32 GetValidPageNumberFromString(string pageNumberString)
        {
            Int32 numPages = 0;
            if (pageNumberString.Length == 0)
            {
                return 1;
            }
            bool success = Int32.TryParse(pageNumberString, out numPages);
            if (success)
            {
                if (numPages > 0 && numPages <= MAX_PAGE_COUNT)
                {
                    return numPages;
                }
            }
            return -1;
        }

        private static List<string> letterSizeCultures = new List<string>() { "US", "CA", "MX", "CU", "DO", "GT", "CR", "SV", "HN", "CO", "VE", "PH", "CL" };
        private PaperSizes GetRegionDefaultPageSize()
        {
            PaperSizes paperSize = PaperSizes.A4;

            string language = string.Empty;
            if (Windows.System.UserProfile.GlobalizationPreferences.Languages.Count > 0)
            {
                language = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
            }

            if (!string.IsNullOrWhiteSpace(language) && language.Length > 2)
            {
                if (letterSizeCultures.Contains(language.Substring(language.Length - 2).ToUpper()))
                {
                    paperSize = PaperSizes.Letter;
                }
            }

            return paperSize;
        }

        private string GetStringWithinRange(string pageNumberString)
        {
            Int32 numPages = 1;
            if (pageNumberString.Length == 0)
            {
                return "1";
            }
            bool success = Int32.TryParse(pageNumberString, out numPages);
            if (success)
            {
                if (numPages < 1)
                {
                    numPages = 1;
                }
                else if (numPages > MAX_PAGE_COUNT)
                {
                    numPages = MAX_PAGE_COUNT;
                }
            }
            else
            {
                numPages = 1;
            }
            return "" + numPages;
        }

        #endregion Utility Functions



        override protected Task<PDFDoc> GetPDFDocAsync()
        {
            return Task.Run<PDFDoc>(() =>
                {
                    PDFDoc doc = new PDFDoc();
                    double width = Letter_Width;
                    double height = Letter_Height;

                    GetPaperDimensionsFromSize(SelectedItem.PaperSize, out width, out height);
                    if (PageOrientation == PageOrientations.Landscape)
                    {
                        double temp = width;
                        width = height;
                        height = temp;
                    }

                    int totalPages = GetValidPageNumberFromString(TotalPagesString);
                    for (int i = 0; i < totalPages; i++)
                    {
                        pdftron.PDF.Page page = doc.PageCreate();
                        page.SetMediaBox(new Rect(0, 0, width, height));
                        page.SetCropBox(new Rect(0, 0, width, height));
                        doc.PagePushBack(page);
                    }

                    return doc;
                });
        }
    }


}
