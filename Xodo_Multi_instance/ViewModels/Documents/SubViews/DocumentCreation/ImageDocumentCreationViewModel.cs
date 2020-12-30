using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompleteReader.ViewModels.Common;
using pdftron.PDF;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Capture;

namespace CompleteReader.ViewModels.DocumentsPage
{
    class ImageDocumentCreationViewModel : DocumentCreationViewModelBase
    {
        private bool _ImagePickingInProgress = false;
        private IList<StorageFile> _FilesPicked = new List<StorageFile>();
        private static IList<string> Valid_Image_Types = null;

        public ImageDocumentCreationViewModel()
            : base()
        {
            PickDocumentCommand = new RelayCommand(PickDocumentCommandImpl);
            GetPictureFromCameraCommand = new RelayCommand(GetPictureFromCameraCommandImpl);

            if (Valid_Image_Types == null)
            {
                Valid_Image_Types = new List<string>() { ".jpg", ".bmp", ".jpeg", ".tif", ".tiff", ".png", ".gif", ".ico", ".jpeg-xr" };
                // , ".xps", ".oxps" (Add this back to the array above to be able to handle xps documents. It does require quite a bit of UI changes
                // Slower document creation -> Need to handle cancelling, need to show spinner, etc.
                // Cannot say "from image" anymore.
            }
        }


        #region Commands

        public RelayCommand PickDocumentCommand { get; private set; }

        public RelayCommand GetPictureFromCameraCommand { get; private set; }

        public void PickDocumentCommandImpl(object sender)
        {
            PickImageFromFileSystem();
        }

        public void GetPictureFromCameraCommandImpl(object sender)
        {
            GetImageFromCamera();
        }

        #endregion Commands

        #region Properties

        protected override bool CheckValidity()
        {
            return _FilesPicked.Count > 0;
        }

        public bool IsImageSelected
        {
            get { return _PreviewImage != null; }
        }

        private BitmapImage _PreviewImage = null;
        public BitmapImage PreviewImage
        {
            get { return _PreviewImage; }
            set
            {
                _PreviewImage = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsImageSelected");
            }
        }

        private string _ErrorMessage = "";
        protected override Windows.UI.Popups.MessageDialog GetErrorMessageDialog()
        {
            if (string.IsNullOrWhiteSpace(_ErrorMessage))
            {
                return base.GetErrorMessageDialog();
            }
            else
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                return new Windows.UI.Popups.MessageDialog(_ErrorMessage, loader.GetString("DocumentCreation_ImageDocument_CreationFailed_Title"));
            }
        }

        #endregion Properties
        private async void PickImageFromFileSystem()
        {
            if (_ImagePickingInProgress)
            {
                return;
            }

            try
            {
                _ImagePickingInProgress = true;

                IReadOnlyList<StorageFile> pickedFiles = await GetOpenFileFromPicker(Valid_Image_Types);
                if (pickedFiles[0] == null)
                {
                    return;
                }
                _FilesPicked.Clear();
                _FilesPicked.Add(pickedFiles[0]);

                AddFileToBeShown(_FilesPicked[0]);

                RaisePropertyChanged("IsInputValid");
            }
            catch (Exception)
            {

            }
            finally
            {
                _ImagePickingInProgress = false;
            }
        }

        
        private async void GetImageFromCamera()
        {
            if (_ImagePickingInProgress)
            {
                return;
            }
            try
            {
                _ImagePickingInProgress = true;

                CameraCaptureUI cameraUI = new CameraCaptureUI();
                cameraUI.PhotoSettings.AllowCropping = false;
                cameraUI.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;
                cameraUI.PhotoSettings.AllowCropping = true;

                StorageFile capturedMedia = await cameraUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
                if (capturedMedia != null)
                {
                    _FilesPicked.Clear();
                    _FilesPicked.Add(capturedMedia);

                    AddFileToBeShown(capturedMedia);

                    RaisePropertyChanged("IsInputValid");
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                _ImagePickingInProgress = false;
            }
        }

        #region Utility Function

        private async Task<IReadOnlyList<StorageFile>> GetOpenFileFromPicker(IList<string> fileTypes)
        {
            StorageFile theFile = null;

            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
            fileOpenPicker.CommitButtonText = loader.GetString("DocumentCreation_ImageDocument_PickFileConform");
            foreach (string filetype in fileTypes)
            {
                fileOpenPicker.FileTypeFilter.Add(filetype);
            }
            
            theFile = await fileOpenPicker.PickSingleFileAsync();

            List<StorageFile> pickedFiles = new List<StorageFile>();
            pickedFiles.Add(theFile);

            return pickedFiles;
        }


        private async void AddFileToBeShown(StorageFile file)
        {
            try
            {
                if (file.FileType == ".xps" || file.FileType == ".oxps")
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.UriSource = new Uri("ms-appx:///Assets/DocumentPage/FilePlaceHolder_pdf.png", UriKind.Absolute);
                    PreviewImage = bitmapImage;
                }
                else
                {
                    using (IRandomAccessStream fileStream = await file.OpenReadAsync())
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(fileStream);
                        PreviewImage = bitmapImage;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        #endregion Utility Function

        override protected async Task<PDFDoc> GetPDFDocAsync()
        {
            _ErrorMessage = "";
            PDFDoc doc = new PDFDoc();

            if (_FilesPicked.Count > 0 && (_FilesPicked[0].FileType == ".xps" || _FilesPicked[0].FileType == ".oxps"))
            {
                await GetDocFromConverter(doc, _FilesPicked[0]).ConfigureAwait(false);
                return doc;
            }

            pdftron.SDF.SDFDoc sdoc = doc.GetSDFDoc();

            ElementBuilder builder = new ElementBuilder();		// ElementBuilder is used to build new Element objects
            ElementWriter writer = new ElementWriter();	// ElementWriter is used to write Elements to the page	

            foreach (StorageFile file in _FilesPicked)
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                _ErrorMessage = string.Format(loader.GetString("DocumentCreation_ImageDocument_CreationFailed_Content"), file.Name);
                IRandomAccessStream ras = await file.OpenReadAsync();
                Page page = doc.PageCreate();
                writer.Begin(page);	// begin writing to this page

                pdftron.PDF.Image img = null;

                try
                {
                    img = await pdftron.PDF.Image.CreateAsync(sdoc, ras);
                }
                catch (Exception)
                {
                    img = null;
                }

                try
                {
                    if (img == null)
                    {
                        Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ras);
                        Windows.Graphics.Imaging.PixelDataProvider pdp = await decoder.GetPixelDataAsync();

                        uint height = decoder.PixelHeight;
                        uint width = decoder.PixelWidth;

                        Windows.Graphics.Imaging.PixelDataProvider pdp2 = await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8,
                            Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
                            new Windows.Graphics.Imaging.BitmapTransform(),
                            Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation,
                            Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);

                        byte[] pixels = pdp2.DetachPixelData();
                        byte[] noAlphaPixels = new byte[(pixels.Length * 3) / 4];

                        int j = -1;
                        for (int i = 0; i < pixels.Length; i += 4)
                        {
                            noAlphaPixels[++j] = pixels[i];
                            noAlphaPixels[++j] = pixels[i + 1];
                            noAlphaPixels[++j] = pixels[i + 2];
                        }
                        try
                        {
                            img = pdftron.PDF.Image.Create(sdoc, noAlphaPixels, (int)width, (int)height, 8, ColorSpace.CreateDeviceRGB(), pdftron.PDF.ImageInputFilter.e_none);
                        }
                        catch (Exception)
                        {
                            img = null;
                        }
                    }
                }
                catch (Exception)
                { }

                if (img == null)
                {
                    return null;
                }
                page.SetCropBox(new Rect(0, 0, img.GetImageWidth(), img.GetImageHeight()));
                page.SetMediaBox(new Rect(0, 0, img.GetImageWidth(), img.GetImageHeight()));

                Element element = builder.CreateImage(img, new pdftron.Common.Matrix2D(img.GetImageWidth(), 0, 0, img.GetImageHeight(), 0, 0));

                writer.WritePlacedElement(element);

                writer.End();  // save changes to the current page
                doc.PagePushBack(page);
            }
            _ErrorMessage = "";

            return doc;
        }

        private async Task GetDocFromConverter(PDFDoc doc, StorageFile file)
        {
            StorageFile newfile = await file.CopyAsync(ApplicationData.Current.TemporaryFolder, file.Name, NameCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);

            pdftron.PDF.Convert.FromXps(doc, newfile.Path);
        }

    }
}
