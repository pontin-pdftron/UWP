using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompleteReader.ViewModels.Common;

using pdftron.PDF;
using CompleteReader.Utilities;
using Windows.UI.ViewManagement;

namespace CompleteReader.ViewModels.Viewer.Helpers
{
    /// <summary>
    /// All the settings regarding the current ViewerPage are centralized here.
    /// </summary>
    public class ViewerPageSettingsViewModel : ViewModelBase
    {
        public enum RequestableViews
        {
            none,
            Thumbnails,
            Crop,
        }

        public enum CustomColorModes
        {
            none,
            Night,
            Sepia,
            Custom,
        }

        public static Windows.UI.Color SEPIA_BLACK = Windows.UI.Colors.Black;
        public static Windows.UI.Color SEPIA_WHITE = Windows.UI.Color.FromArgb(255, 255, 232, 206);

        public delegate void ViewRequestedDelegate(RequestableViews view);
        public event ViewRequestedDelegate ViewRequestedHandler;

        private CompleteReaderPDFViewCtrlTabInfo _Tab;
        private PDFViewCtrl _PDFViewCtrl;

        public ViewerPageSettingsViewModel()
        {
            Init();
        }

        public ViewerPageSettingsViewModel(CompleteReaderPDFViewCtrlTabInfo tab)
        {
            _Tab = tab;
            _PDFViewCtrl = tab.PDFViewCtrl;
            Init();
        }

        private void Init()
        {
            CustomColorCommand = new RelayCommand(CustomColorCommandImpl);
            PagePresentationModeCommand = new RelayCommand(PagePresentationModeCommandImpl);
            ReflowCommand = new RelayCommand(ReflowCommandImpl);
            RotateCommand = new RelayCommand(RotateCommandImpl);
            ThumbnailViewCommand = new RelayCommand(ThumbnailViewCommandImpl);
            CropViewCommand = new RelayCommand(CropViewCommandImpl);
            FullScreenCommand = new RelayCommand(FullScreenCommandImpl);
            ExitFullScreenCommand = new RelayCommand(ExitFullScreenCommandImpl);

            _ViewerPageSettingsViewModel_VisibleBoundsChanged = 
                new Windows.Foundation.TypedEventHandler<ApplicationView, object>(ViewerPageSettingsViewModel_VisibleBoundsChanged);
            ApplicationView.GetForCurrentView().VisibleBoundsChanged += _ViewerPageSettingsViewModel_VisibleBoundsChanged;
        }

        private Windows.Foundation.TypedEventHandler<ApplicationView, object> _ViewerPageSettingsViewModel_VisibleBoundsChanged = null;
        /// <summary>
        /// Important to call this when done, as the subscription to 
        /// ApplicationView.GetForCurrentView().VisibleBoundsChanged prevents it from being GCd
        /// </summary>
        public void CleanUp()
        {
            if (_ViewerPageSettingsViewModel_VisibleBoundsChanged != null)
            {
                ApplicationView.GetForCurrentView().VisibleBoundsChanged -= _ViewerPageSettingsViewModel_VisibleBoundsChanged;
                _ViewerPageSettingsViewModel_VisibleBoundsChanged = null;
            }
        }

        #region Commands
        public RelayCommand CustomColorCommand { get; private set; }
        public RelayCommand PagePresentationModeCommand { get; private set; }
        public RelayCommand ReflowCommand { get; private set; }
        public RelayCommand RotateCommand { get; private set; }
        public RelayCommand ThumbnailViewCommand { get; private set; }
        public RelayCommand CropViewCommand { get; private set; }
        public RelayCommand FullScreenCommand { get; private set; }
        public RelayCommand ExitFullScreenCommand { get; private set; }


        private void CustomColorCommandImpl(object modeName)
        {
            ResolveCustomColorMode(modeName);
        }

        private void ResolveCustomColorMode(object modeName)
        {
            if (modeName.ToString().Equals("D", StringComparison.OrdinalIgnoreCase))
            {
                ColorMode = CustomColorModes.none;
            }
            else if (modeName.ToString().Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                ColorMode = CustomColorModes.Night;
            }
            else if (modeName.ToString().Equals("S", StringComparison.OrdinalIgnoreCase))
            {
                ColorMode = CustomColorModes.Sepia;
            }
            else if (modeName.ToString().Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                RaisePropertyChanged("CustomColor");
            }
        }

        private void PagePresentationModeCommandImpl(object modeName)
        {
            ResolvePagePresentationMode(modeName);
        }

        private void ReflowCommandImpl(object param)
        {
            _Tab.IsReflow = !_Tab.IsReflow;
            RaiseCurrentPageModePropertyChanged();
        }

        private void ResolvePagePresentationMode(object modeName)
        {
            if (_PDFViewCtrl == null)
            {
                return;
            }

            string selectedMode = modeName as string;
            PDFViewCtrlPagePresentationMode oldMode = _PDFViewCtrl.GetPagePresentationMode();
            PDFViewCtrlPagePresentationMode newMode = oldMode;
            if (selectedMode == "con")
            {
                if (newMode == PDFViewCtrlPagePresentationMode.e_single_page || newMode == PDFViewCtrlPagePresentationMode.e_single_continuous)
                    selectedMode = "s";
                else if (newMode == PDFViewCtrlPagePresentationMode.e_facing || newMode == PDFViewCtrlPagePresentationMode.e_facing_continuous)
                    selectedMode = "f";
                else
                    selectedMode = "fcov";
            }

            if (!string.IsNullOrWhiteSpace(selectedMode))
            {
                if (selectedMode.Equals("s", StringComparison.OrdinalIgnoreCase))
                {
                    newMode = IsContinuousOption ? PDFViewCtrlPagePresentationMode.e_single_continuous : PDFViewCtrlPagePresentationMode.e_single_page;
                }
                else if (selectedMode.Equals("f", StringComparison.OrdinalIgnoreCase))
                {
                    newMode = IsContinuousOption ? PDFViewCtrlPagePresentationMode.e_facing_continuous : PDFViewCtrlPagePresentationMode.e_facing;
                }
                else if (selectedMode.Equals("fcov", StringComparison.OrdinalIgnoreCase))
                {
                    newMode = IsContinuousOption ? PDFViewCtrlPagePresentationMode.e_facing_continuous_cover : PDFViewCtrlPagePresentationMode.e_facing_cover;
                }
            }
            if (oldMode != newMode || IsReflow)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.VIEWER, UtilityFunctions.GetPagePresentationModeName(newMode) + " selected");
                IsReflow = false;
                RaiseCurrentPageModePropertyChanged();
                PagePresentationMode = newMode;
                Settings.Settings.PagePresentationMode = newMode;

                if (!IsContinuous(oldMode) && IsContinuous(newMode))
                {
                    _PDFViewCtrl.SetPageViewMode(PDFViewCtrlPageViewMode.e_fit_width);
                }
                else if (!IsContinuous(newMode))
                {
                    _PDFViewCtrl.SetPageViewMode(_PDFViewCtrl.GetPageRefViewMode());
                }
            }
        }

        private bool IsContinuous(PDFViewCtrlPagePresentationMode mode)
        {
            return mode == PDFViewCtrlPagePresentationMode.e_single_continuous || mode == PDFViewCtrlPagePresentationMode.e_facing_continuous || mode == PDFViewCtrlPagePresentationMode.e_facing_continuous_cover;
        }

        private void RotateCommandImpl(object button)
        {
            if (_PDFViewCtrl != null)
            {
                try
                {
                    _PDFViewCtrl.RotateClockwise();

                }
                catch (Exception e) 
                { 
                    string message = string.Empty;
                    pdftron.Common.PDFNetException pdfEx = new pdftron.Common.PDFNetException(e.HResult);
                    if (pdfEx.IsPDFNetException)
                    {
                        message = pdfEx.ToString();
                    }
                    else
                    {
                        message = e.ToString();
                    }
                    System.Diagnostics.Debug.WriteLine("Error rotating: " + message);
                }
                RaisePropertyChanged("Rotation");
            }
        }

        private void ThumbnailViewCommandImpl(object button)
        {
            if (ViewRequestedHandler != null)
            {
                ViewRequestedHandler(RequestableViews.Thumbnails);
            }
        }

        private void CropViewCommandImpl(object button)
        {
            if (ViewRequestedHandler != null)
            {
                ViewRequestedHandler(RequestableViews.Crop);
            }
        }

        private void FullScreenCommandImpl(object button)
        {
            ToggleFullScreen();
        }

        private void ExitFullScreenCommandImpl(object button)
        {
            ToggleFullScreen();
        }

        private async void ToggleFullScreen()
        {
            bool fullScreen = !IsFullScreen;
            await UtilityFunctions.SetFullScreenModeAsync(fullScreen);
            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                int fullScreenInt = 0;
                if (fullScreen)
                {
                    fullScreenInt = 1;
                }
                Settings.Settings.PhoneFullScreen = fullScreenInt;
            }
            RaisePropertyChanged("FullScreen");
            RaisePropertyChanged("IsFullScreen");
        }

        #endregion Commands

        #region Properties

        public PDFViewCtrlPagePresentationMode PagePresentationMode
        {
            get 
            {
                if (_PDFViewCtrl != null)
                {
                    return _PDFViewCtrl.GetPagePresentationMode();
                }
                return PDFViewCtrlPagePresentationMode.e_single_page;
            }
            set
            {
                if (_PDFViewCtrl != null && value != _PDFViewCtrl.GetPagePresentationMode())
                {
                    IsReflow = false;
                    _PDFViewCtrl.SetPagePresentationMode(value);
                    RaisePropertyChanged();
                    RaiseCurrentPageModePropertyChanged();
                }
            }
        }

        public void UpdateSettings()
        {
            _IsContinuousOption = IsContinuous(_PDFViewCtrl.GetPagePresentationMode());
            RaisePropertyChanged("IsContinuousOption");
            PagePresentationMode = _PDFViewCtrl.GetPagePresentationMode();
            RaisePropertyChanged("IsFullScreen");
            RaisePropertyChanged("IsConverting");
            RaisePropertyChanged("CanCrop");
        }

        public bool IsCurrentPagePresentationModeSinglePage
        {
            get
            {
                if (_PDFViewCtrl == null)
                    return true;
                if (IsReflow)
                    return false;

                return _PDFViewCtrl.GetPagePresentationMode() == PDFViewCtrlPagePresentationMode.e_single_page ||
                    _PDFViewCtrl.GetPagePresentationMode() == PDFViewCtrlPagePresentationMode.e_single_continuous; 
            }
        }
        public bool IsCurrentPagePresentationModeFacing
        {
            get
            {
                if (_PDFViewCtrl == null)
                    return false;
                if (IsReflow)
                    return false;

                return _PDFViewCtrl.GetPagePresentationMode() == PDFViewCtrlPagePresentationMode.e_facing ||
                    _PDFViewCtrl.GetPagePresentationMode() == PDFViewCtrlPagePresentationMode.e_facing_continuous;
            }
        }
        public bool IsCurrentPagePresentationModeFacingCover
        {
            get
            {
                if (_PDFViewCtrl == null)
                    return false;
                if (IsReflow)
                    return false;

                return _PDFViewCtrl.GetPagePresentationMode() == PDFViewCtrlPagePresentationMode.e_facing_cover ||
                    _PDFViewCtrl.GetPagePresentationMode() == PDFViewCtrlPagePresentationMode.e_facing_continuous_cover;
            }
        }

        public bool IsReflow
        {
            get
            {
                if (_Tab != null)
                {
                    return _Tab.IsReflow;
                }
                return _Tab.IsReflow;
            }
            set
            {
                if (_Tab != null && _Tab.IsReflow != value)
                {
                    _Tab.IsReflow = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsConverting
        {
            get { return _Tab == null || _Tab.IsConverting; }
        }

        public bool CanCrop
        {
            get { return !IsConverting && !IsReflow; }
        }

        public CustomColorModes ColorMode
        {
            get { return Settings.Settings.ColorMode; }
            set
            {
                if (value != Settings.Settings.ColorMode)
                {
                    Settings.Settings.ColorMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        public PDFViewCtrl PDFViewCtrl
        {
            set { _PDFViewCtrl = value; }
        }

        private bool _IsContinuousOption;
        public bool IsContinuousOption
        {
            get { return _IsContinuousOption; }
            set
            {
                if (_IsContinuousOption != value)
                {
                    _IsContinuousOption = value;
                    RaisePropertyChanged("IsContinuousOption");
                    ResolvePagePresentationMode("con");
                }
            }
        }

        public bool IsFullScreen
        {
            get
            {
                return UtilityFunctions.IsFullScreen();
            }
        }

        private void ViewerPageSettingsViewModel_VisibleBoundsChanged(ApplicationView sender, object args)
        {
            RaisePropertyChanged("IsFullScreen");
        }

        private void RaiseCurrentPageModePropertyChanged()
        {
            RaisePropertyChanged("IsReflow");
            RaisePropertyChanged("CanCrop");
            RaisePropertyChanged("IsCurrentPagePresentationModeContinuous");
            RaisePropertyChanged("IsCurrentPagePresentationModeSinglePage");
            RaisePropertyChanged("IsCurrentPagePresentationModeFacing");
            RaisePropertyChanged("IsCurrentPagePresentationModeFacingCover");
        }

        #endregion Properties
    }
}
