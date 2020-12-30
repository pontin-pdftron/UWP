using CompleteReader.ViewModels.Common;
using pdftron.Common;
using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CompleteReader.ViewModels.Viewer.Helpers
{
    public class CropPopupViewModel : ViewModelBase
    {
        private PDFViewCtrl _PDFViewCtrl;

        private bool _IsPopupOpen = false;
        public bool IsPopupOpen
        {
            get { return _IsPopupOpen; }
            set
            {
                if (Set(ref _IsPopupOpen, value))
                {
                    if (!value)
                    {
                        if (PopupClosed != null)
                            PopupClosed();
                    }
                }
            }
        }

        private const int CROP_RECT_WHITE_SPACE_MARGIN = 2;
        private List<pdftron.PDF.Rect> _AutomaticBoxes = new List<pdftron.PDF.Rect>();

        private bool _IsAutomaticCropping = false;
        public bool IsAutomaticCropping
        {
            get { return _IsAutomaticCropping; }
            set { Set(ref _IsAutomaticCropping, value); }
        }

        private int _CurrentAutomaticPage = 1;

        public int AutomaticProgress
        {
            get { return (int)((100.0 *(_CurrentAutomaticPage - 1)) / _PDFViewCtrl.GetPageCount()); }
        }

        public string AutomaticProgressPageText
        {
            get
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                return string.Format(loader.GetString("CropPopup_Automatic_PageText"), _CurrentAutomaticPage, _PDFViewCtrl.GetPageCount());
            }
        }

        private bool _IsAutomaticCancel;


        public delegate void PopupClosedDelegate();
        public event PopupClosedDelegate PopupClosed;

        public delegate void ManualCropRequestedDelegate();
        public event ManualCropRequestedDelegate ManualCropRequested;

        public delegate void DocumentEditedDelegate();
        public event DocumentEditedDelegate DocumentEdited;

        public CropPopupViewModel(PDFViewCtrl ctrl)
        {
            _PDFViewCtrl = ctrl;
            Init();
        }

        private void Init()
        {
            AutomaticCroppingCommand = new RelayCommand(AutomaticCroppingCommandImpl);
            ManualCroppingCommand = new RelayCommand(ManualCroppingCommandImpl);
            RemoveCroppingCommand = new RelayCommand(RemoveCroppingCommandImpl);
            OkCommand = new RelayCommand(OkCommandImpl);
            CancelAutomaticCommand = new RelayCommand(CancelAutomaticCommandImpl);
        }

        public RelayCommand AutomaticCroppingCommand { get; private set; }
        public RelayCommand ManualCroppingCommand { get; private set; }
        public RelayCommand RemoveCroppingCommand { get; private set; }
        public RelayCommand OkCommand { get; private set; }
        public RelayCommand CancelAutomaticCommand { get; private set; }


        private CancellationTokenSource _TokenSource;

        /// <summary>
        /// Iterates through all the pages of the PDF and sets the User Crop equal to the bounding box
        /// </summary>
        /// <param name="command"></param>
        private async void AutomaticCroppingCommandImpl(object command)
        {
            // Need to close popup in order for new IsLightDismissEnabled value to take effect
            IsAutomaticCropping = true;
            _IsPopupOpen = false;
            RaisePropertyChanged("IsPopupOpen");
            IsPopupOpen = true;

            _CurrentAutomaticPage = 1;
            RaisePropertyChanged("AutomaticProgress");
            RaisePropertyChanged("AutomaticProgressPageText");

            PDFDoc pdfDoc = _PDFViewCtrl.GetDoc();
            _AutomaticBoxes.Clear();

            bool hasEditedDoc = false;

            try
            {
                pdfDoc.LockRead();
                PageIterator iter = pdfDoc.GetPageIterator();
                _TokenSource = new CancellationTokenSource();
                CancellationToken token = _TokenSource.Token;

                while (iter.HasNext())
                {
                    if (_IsAutomaticCancel)
                        return;

                    Page page = iter.Current();

                    pdftron.PDF.Rect rect = await Task.Run(() =>
                    {
                        Task<pdftron.PDF.Rect> task2 = Task.Run(() =>
                        {
                            pdftron.PDF.Rect cropRect = page.GetCropBox();
                            pdftron.PDF.Rect visibleRect = page.GetVisibleContentBox();
                            visibleRect.Inflate(CROP_RECT_WHITE_SPACE_MARGIN);
                            bool intersetcs = visibleRect.IntersectRect(visibleRect, cropRect);
                            if (intersetcs)
                            {
                                return visibleRect;
                            }
                            return cropRect;
                        }, token);
                        try
                        {
                            task2.Wait(token);
                        }
                        catch (OperationCanceledException)
                        {
                            return null;
                        }
                        return task2.Result;
                    }, token);

                    if (rect == null)
                        return;

                    _AutomaticBoxes.Add(rect);

                    _CurrentAutomaticPage++;
                    RaisePropertyChanged("AutomaticProgress");
                    RaisePropertyChanged("AutomaticProgressPageText");

                    iter.Next();
                }

                _CurrentAutomaticPage = 1;
                PageIterator iter2 = pdfDoc.GetPageIterator();
                while (iter2.HasNext())
                {
                    Page page = iter2.Current();
                    if (_AutomaticBoxes[_CurrentAutomaticPage - 1] != null)
                    {
                        pdftron.PDF.Rect cropRect = page.GetCropBox();
                        pdftron.PDF.Rect oldUserCropRect = page.GetBox(PageBox.e_user_crop);
                        pdftron.PDF.Rect visibleRect = _AutomaticBoxes[_CurrentAutomaticPage - 1];

                        if (!pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.AreRectsSimilar(visibleRect, oldUserCropRect,
                            pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.RECT_COMPARE_THRESHOLD))
                        {
                            if (!pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.AreRectsSimilar(visibleRect, cropRect,
                            pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.RECT_COMPARE_THRESHOLD))
                            {
                                page.SetBox(PageBox.e_user_crop, (_AutomaticBoxes[_CurrentAutomaticPage - 1]));
                                hasEditedDoc = true;
                            }
                            else
                            {
                                hasEditedDoc |= pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.RemoveUserCropFromPage(page);
                            }
                        }
                    }


                    _CurrentAutomaticPage++;
                    iter2.Next();
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
                IsAutomaticCropping = false;
                IsPopupOpen = false;
                _TokenSource.Dispose();

                if (!_IsAutomaticCancel)
                {
                    _PDFViewCtrl.UpdatePageLayout();
                    if (hasEditedDoc)
                    {
                        DocumentEdited?.Invoke();
                    }
                }
                else
                {
                    _IsAutomaticCancel = false;
                }
            }
        }

        /// <summary>
        /// This command raises an event for the CropView to be used 
        /// </summary>
        /// <param name="command"></param>
        private void ManualCroppingCommandImpl(object command)
        {
            if (ManualCropRequested != null)
                ManualCropRequested();

            IsPopupOpen = false;
        }

        private void RemoveCroppingCommandImpl(object command)
        {
            RemoveAllCrop();
        }

        private void OkCommandImpl(object command)
        {
            IsPopupOpen = false;
        }

        /// <summary>
        /// Iterates through all the pages of the PDF and removes all User Crops
        /// </summary>
        private void RemoveAllCrop()
        {
            PDFDoc pdfDoc = _PDFViewCtrl.GetDoc();
            bool hasEditedDoc = false;
            try
            {
                pdfDoc.LockRead();
                PageIterator iter = pdfDoc.GetPageIterator();
                int pageNum = 1;
                while (iter.HasNext())
                {
                    Page page = iter.Current();

                    hasEditedDoc |= pdftron.PDF.Tools.Controls.ViewModels.CropViewViewModel.RemoveUserCropFromPage(page);

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
                IsPopupOpen = false;
                _PDFViewCtrl.UpdatePageLayout();
                if (hasEditedDoc)
                {
                    DocumentEdited?.Invoke();
                }
            }
        }

        private void CancelAutomaticCommandImpl(object command)
        {
            _IsAutomaticCancel = true;
            if (_TokenSource != null)
            {
                _TokenSource.Cancel();
            }
        }
    }
}
