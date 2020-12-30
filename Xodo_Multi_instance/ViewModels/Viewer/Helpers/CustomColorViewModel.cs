using CompleteReader.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace CompleteReader.ViewModels.Viewer.Helpers
{
    public class CustomColorIcon : ViewModelBase
    {
        private Color _Background;

        private Color _Foreground;

        public Color Background
        {
            get { return _Background; }
            set
            {
                if (Set(ref _Background, value))
                {
                    RaisePropertyChanged("BackgroundBrush");
                }
            }
        }
        public Color Foreground
        {
            get { return _Foreground; }
            set
            {
                if (Set(ref _Foreground, value))
                {
                    RaisePropertyChanged("ForegroundBrush");
                }
            }
        }

        public SolidColorBrush BackgroundBrush
        {
            get { return new SolidColorBrush(Background); }
        }

        public SolidColorBrush ForegroundBrush
        {
            get { return new SolidColorBrush(Foreground); }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { Set(ref _IsSelected, value); }
        }

        private bool _IsLoadDefault;
        public bool IsLoadDefault
        {
            get { return _IsLoadDefault; }
            set { Set(ref _IsLoadDefault, value); }
        }

        public CustomColorIcon(Color background, Color foreground)
        {
            Background = CloneColor(background); 
            Foreground = CloneColor(foreground);
        }

        private Color CloneColor(Color color)
        {
            return new Color { A = color.A, R = color.R, G = color.G, B = color.B };
        }
    }

    public class CustomColorViewModel : ViewModelBase
    {
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
                        if (ViewMode == View.Edit)
                        {
                            PopulateCustomColorCollection();
                        }
                        PopupClosed();
                    }
                    else
                    {
                        SetDefaultValues();
                    }
                }
            }
        }

        public enum View
        {
            Icon,
            Edit,
        }

        private View _ViewMode;

        public View ViewMode
        {
            get { return _ViewMode; }
            set { Set(ref _ViewMode, value); }
        }

        public enum Edit
        {
            Background,
            Text,
        }

        private Edit _EditMode;

        public Edit EditMode
        {
            get { return _EditMode; }
            set
            {
                if (Set(ref _EditMode, value))
                {
                    UpdateOpacityGradient();
                }
            }

        }

        private const int NUM_ICONS = 15;

        private bool _IsLightDismissable = true;
        public bool IsLightDismissable
        {
            get { return _IsLightDismissable; }
            set { Set(ref _IsLightDismissable, value); }
        }

        // Signals to the view that a color icon is pressed
        public delegate void CustomColorSelectedDelegate(CustomColorIcon icon);
        public event CustomColorSelectedDelegate CustomColorSelected;

        // Signals to the ViewerViewModel to apply the color icon
        public delegate void CustomColorRequestedDelegate(CustomColorIcon icon);
        public event CustomColorRequestedDelegate CustomColorRequested;

        public CustomColorIcon _CurrSelectedIcon;

        public CustomColorIcon CurrSelectedIcon
        {
            get { return _CurrSelectedIcon; }
            set
            {
                if (Set(ref _CurrSelectedIcon, value))
                {
                    if (value != null)
                    {
                        UpdateOpacityGradient();
                        if (CurrSelectedIcon != null)
                        {
                            CustomColorSelected?.Invoke(CurrSelectedIcon);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Essentially, the RGB equivalent of the HSV value of the current color except with V set to 255
        /// since this is the gradient (topmost color) that the V grid will bind to.
        /// </summary>
        private Color _CurrEditBrushGradient;

        public Color CurrEditBrushGradient
        {
            get
            {
                return _CurrEditBrushGradient;
            }
            set
            {
                Set(ref _CurrEditBrushGradient, value);
            }
        }

        public Color CurrEditBrush
        {
            get
            {
                if (CurrSelectedIcon == null)
                    return Colors.Transparent;

                if (EditMode == Edit.Background)
                {
                    Color color = CurrSelectedIcon.Background;
                    return Color.FromArgb(255, color.R, color.G, color.B);
                }
                else
                {
                    Color color = CurrSelectedIcon.Foreground;
                    return Color.FromArgb(255, color.R, color.G, color.B);
                }
            }
        }

        /// <summary>
        /// The current V value of the Background/Foreground grid
        /// </summary>
        public double CurrEditBrushOpacity
        {
            get
            {
                if (CurrSelectedIcon == null)
                    return 0;

                Color color = EditMode == Edit.Background ? CurrSelectedIcon.Background : CurrSelectedIcon.Foreground;
                return Math.Max(color.B, Math.Max(color.R, color.G)) / 255.0;
            }
        }

        public ObservableCollection<CustomColorIcon> CustomColorIconCollection { get; private set; }

        public delegate void PopupClosedDelegate();
        public event PopupClosedDelegate PopupClosed;

        public CustomColorViewModel()
        {
            Init();
        }

        private void Init()
        {
            CancelCommand = new RelayCommand(CancelCommandImpl);
            OkCommand = new RelayCommand(OkCommandImpl);
            IconClickCommand = new RelayCommand(IconClickCommandImpl);
            EditModeCommand = new RelayCommand(EditModeCommandImpl);

            CustomColorIconCollection = new ObservableCollection<CustomColorIcon>();
            PopulateCustomColorCollection();
            if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Custom)
            {
                CurrSelectedIcon = CustomColorIconCollection[Settings.Settings.CurrentCustomColorIcon];
            }
        }

        /// <summary>
        /// Since this View/ViewModel is persistent with the ViewerViewModel, 
        /// when user navigates back here again, set back to the default values
        /// </summary>
        private void SetDefaultValues()
        {
            ViewMode = View.Icon;
            EditMode = Edit.Background;
            if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Custom)
            {
                CurrSelectedIcon = CustomColorIconCollection[Settings.Settings.CurrentCustomColorIcon];
            }
            else if (CurrSelectedIcon != null)
            {
                CurrSelectedIcon.IsSelected = false;
                CurrSelectedIcon = null;
            }
        }

        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand OkCommand { get; private set; }
        public RelayCommand IconClickCommand { get; private set; }
        public RelayCommand EditModeCommand { get; private set; }

        private void CancelCommandImpl(object param)
        {
            if (ViewMode == View.Edit)
            {
                PopulateCustomColorCollection();
                ViewMode = View.Icon;
            }
            else
            {
                IsPopupOpen = false;
            }
        }

        private void OkCommandImpl(object param)
        {
            if (ViewMode == View.Edit)
            {
                Settings.Settings.SetCustomColorIcon(CurrSelectedIcon.Background, CurrSelectedIcon.Foreground, CustomColorIconCollection.IndexOf(CurrSelectedIcon));
                ViewMode = View.Icon;
            }
            else
            {
                // Handle applying chosen custom color
                if (CurrSelectedIcon != null)
                {
                    int index = CustomColorIconCollection.IndexOf(CurrSelectedIcon);
                    if (index != -1)
                    {
                        Settings.Settings.CurrentCustomColorIcon = index;
                    }
                    CustomColorRequested?.Invoke(CurrSelectedIcon);
                }
                IsPopupOpen = false;
            }
        }

        private async void IconClickCommandImpl(object param)
        {
            ItemClickEventArgs item = param as ItemClickEventArgs;
            if (item == null)
                return;

            CustomColorIcon nextIcon = (CustomColorIcon)item.ClickedItem;
            EditMode = Edit.Background;

            if (nextIcon.IsLoadDefault)
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                MessageDialog md = new MessageDialog(loader.GetString("CustomColorPopup_LoadDefault_Content"), loader.GetString("CustomColorPopup_LoadDefault_Title"));
                md.Commands.Add(new Windows.UI.Popups.UICommand(
                    Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("CustomColorPopup_LoadDefault_Ok"), (command) =>
                    {
                        Settings.Settings.SetDefaultCustomColors();
                        PopulateCustomColorCollection();
                        if (CurrSelectedIcon != null)
                        {
                            CurrSelectedIcon.IsSelected = false;
                            CurrSelectedIcon = null;
                        }
                    }));
                md.Commands.Add(new Windows.UI.Popups.UICommand(
                    Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("Generic_Cancel_Text"), (command) =>
                    {
                    }));

                ChangeIsLightDismissable(false);
                await Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                ChangeIsLightDismissable(true);
                return;
            }

            if (CurrSelectedIcon != null)
            {
                if (CurrSelectedIcon != nextIcon)
                {
                    CurrSelectedIcon.IsSelected = false;
                }
                else
                {
                    ViewMode = View.Edit;
                }
            }

            CurrSelectedIcon = nextIcon;
            CurrSelectedIcon.IsSelected = true;
        }

        private void EditModeCommandImpl(object param)
        {
            string val = param.ToString();
            if (string.Equals(val, "B", StringComparison.OrdinalIgnoreCase))
            {
                EditMode = Edit.Background;
                CustomColorSelected?.Invoke(CurrSelectedIcon);
            }
            if (string.Equals(val, "T", StringComparison.OrdinalIgnoreCase))
            {
                EditMode = Edit.Text;
                CustomColorSelected?.Invoke(CurrSelectedIcon);
            }
        }

        private void PopulateCustomColorCollection()
        {
            IList<Tuple<Color,Color>> colors = Settings.Settings.GetCustomColors();
            CustomColorIconCollection.Clear();
            foreach(var color in colors)
            {
                CustomColorIconCollection.Add(new CustomColorIcon(color.Item1, color.Item2));
            }
            CustomColorIcon defaultIcon = new CustomColorIcon(Colors.Transparent, Colors.Transparent);
            defaultIcon.IsLoadDefault = true;
            CustomColorIconCollection.Add(defaultIcon);
        }

        /// <summary>
        /// This is used since the IsLightDismissable property of a popup only takes effect after
        /// the popup is closed then opened again
        /// </summary>
        /// <param name="isDismissable"></param>
        private void ChangeIsLightDismissable(bool isDismissable)
        {
            IsLightDismissable = isDismissable;
            _IsPopupOpen = false;
            RaisePropertyChanged("IsPopupOpen");
            IsPopupOpen = true;
        }

        /// <summary>
        /// This method is used by the view to notify the ViewModel of the new color.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="isHs">Refers to whether the color was taken from the HS grid or the V grid</param>
        public void UpdateColor(Color color, bool isHs)
        {
            if (EditMode == Edit.Background)
            {
                CurrSelectedIcon.Background = color;
            }
            else
            {
                CurrSelectedIcon.Foreground = color;
            }

            if (isHs)
            { 
                var hsv = Utilities.UtilityFunctions.GetHSVFromRGB(color);
                CurrEditBrushGradient = Utilities.UtilityFunctions.GetRGBFromHSV(hsv.Item1, hsv.Item2, 100);
            }
            else
            {
                UpdateOpacityGradient();
            }
        }

        private void UpdateOpacityGradient()
        {
            RaisePropertyChanged("CurrEditBrushOpacity");
        }
    }
}
