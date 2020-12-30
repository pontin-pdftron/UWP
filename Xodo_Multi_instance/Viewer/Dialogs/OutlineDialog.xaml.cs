using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CompleteReader.Viewer.Dialogs
{
    public class NumberToColumnNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            int numColumns = CompleteReader.Settings.SharedSettings.OutlineDialogNumThumbnailColumns;
            string formatString = "";
            try
            {
                numColumns = System.Convert.ToInt32(value);
            }
            catch (Exception) { }
            if (numColumns == 1)
            {
                formatString = loader.GetString("OutlineDialog_SelectNumberOfThumbnailColumns_Singular");
            }
            else
            {
                formatString = loader.GetString("OutlineDialog_SelectNumberOfThumbnailColumns_Plural");
            }

            return string.Format(formatString, numColumns);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Not used
            return 1;
        }
    }

    public class GreaterThanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int numColumns = CompleteReader.Settings.SharedSettings.OutlineDialogNumThumbnailColumns;
            try
            {
                numColumns = System.Convert.ToInt32(value);
            }
            catch (Exception) { }
            int expectedColumns = 0;
            try
            {
                expectedColumns = System.Convert.ToInt32(parameter);
            }
            catch (Exception) { }
            if (numColumns > expectedColumns)
            {
                return 1.0;
            }
            return 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Not used
            return 1;
        }
    }

    public sealed partial class OutlineDialog : UserControl
    {
        public enum AnchorSides
        {
            None = 0,
            Left,
            Right
        }

        public enum WidthLayouts
        {
            Narrow,
            Full,
        }

        private static Dictionary<WidthLayouts, double> _LayoutMinWidths;
        public Dictionary<WidthLayouts, double> LayoutMinWidths
        {
            get
            {
                if (_LayoutMinWidths == null)
                {
                    _LayoutMinWidths = new Dictionary<WidthLayouts, double>();
                    _LayoutMinWidths.Add(WidthLayouts.Narrow, 4 * (double)Resources["OutlineTabButtonSize"]);
                    _LayoutMinWidths.Add(WidthLayouts.Full, 6 * (double)Resources["OutlineTabButtonSize"]);
                }
                return _LayoutMinWidths;
            }
        }

        public OutlineDialog()
        {
            this.InitializeComponent();
            this.SizeChanged += OutlineDialog_SizeChanged;
            this.DataContextChanged += OutlineDialog_DataContextChanged;
            SetupColumnButtons();
        }

        ViewModels.Viewer.Helpers.OutlineDialogViewModel _OldDataContext = null;
        private void OutlineDialog_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_OldDataContext != null)
            {
                _OldDataContext.PropertyChanged -= OutlineDialog_PropertyChanged;
                _OldDataContext = null;
            }
            if (this.DataContext is CompleteReader.ViewModels.Viewer.Helpers.OutlineDialogViewModel)
            {
                _OldDataContext = this.DataContext as CompleteReader.ViewModels.Viewer.Helpers.OutlineDialogViewModel;
                _OldDataContext.PropertyChanged += OutlineDialog_PropertyChanged;
                if (_OldDataContext.Isconverting)
                {
                    if (BookmarksOptions.Flyout == null)
                    {
                        BookmarksOptions.Flyout = BookmarksOptions.Resources["BookmarkButtonFlyout"] as Flyout;
                        BookmarksOptions.Foreground = Resources["SystemControlDisabledBaseMediumLowBrush"] as SolidColorBrush;
                    }
                }
                else
                {
                    BookmarksOptions.Flyout = null;
                    BookmarksOptions.Foreground = Resources["ThemeBrushHighlightDifferentBrightness"] as SolidColorBrush;
                }
            }
        }

        private void OutlineDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResolveWidthButtons();
            if (e.NewSize.Width < LayoutMinWidths[WidthLayouts.Full])
            {
                WidthLayout = WidthLayouts.Narrow;
            }
            else
            {
                WidthLayout = WidthLayouts.Full;
            }
        }

        private void SetupColumnButtons()
        {
            try
            {
                StackPanel stack = NumColumnsFlyout.Content as StackPanel;
                if (stack != null)
                {
                    NumberToColumnNameConverter conv = new NumberToColumnNameConverter();
                    for (int i = 0; i < stack.Children.Count; ++i)
                    {
                        Button button = stack.Children[i] as Button;
                        if (button != null)
                        {
                            button.Content = conv.Convert(i + 1, typeof(object), null, "");
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void ColumnNumberButton_Click(object sender, RoutedEventArgs e)
        {
            NumColumnsFlyout.Hide();
        }

        private void DockAndWidthFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string tagText = button.Tag as string;
            if (!string.IsNullOrWhiteSpace(tagText))
            {
                int width = 0;
                try
                {
                    width = System.Convert.ToInt32(tagText);
                    Settings.Settings.OutlineDialogWidth = width;
                    ResolveWidthButtons();
                }
                catch (Exception) { }

            }
            DockAndWidthFlyout.Hide();
        }

        private void ResolveWidthButtons()
        {
            Brush SelectedBrush = Resources["ThemeBrushHighlightDifferentBrightness"] as Brush;
            Brush UnselectedBrush = Resources["SystemControlForegroundBaseHighBrush"] as Brush;
            if (Settings.Settings.OutlineDialogWidth == 0)
            {
                WidthButton0.Foreground = SelectedBrush;
            }
            else
            {
                WidthButton0.Foreground = UnselectedBrush;
            }
            if (Settings.Settings.OutlineDialogWidth == 1)
            {
                WidthButton1.Foreground = SelectedBrush;
            }
            else
            {
                WidthButton1.Foreground = UnselectedBrush;
            }
            if (Settings.Settings.OutlineDialogWidth == 2)
            {
                WidthButton2.Foreground = SelectedBrush;
            }
            else
            {
                WidthButton2.Foreground = UnselectedBrush;
            }
        }

        void OutlineDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is CompleteReader.ViewModels.Viewer.Helpers.OutlineDialogViewModel)
            {
                (this.DataContext as CompleteReader.ViewModels.Viewer.Helpers.OutlineDialogViewModel).FadeAnimation = FadeOutStoryBoard;
                (this.DataContext as CompleteReader.ViewModels.Viewer.Helpers.OutlineDialogViewModel).PropertyChanged += OutlineDialog_PropertyChanged;
            }
        }

        private void OutlineDialog_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Isconverting", StringComparison.OrdinalIgnoreCase))
            {
                if ((this.DataContext as CompleteReader.ViewModels.Viewer.Helpers.OutlineDialogViewModel).Isconverting)
                {
                    if (BookmarksOptions.Flyout == null)
                    {
                        BookmarksOptions.Flyout = BookmarksOptions.Resources["BookmarkButtonFlyout"] as Flyout;
                    }
                }
                else
                {
                    BookmarksOptions.Flyout = null;
                }
            }
        }

        public static readonly DependencyProperty AnchorSideProperty = DependencyProperty.Register(
          "AnchorSide",
          typeof(AnchorSides),
          typeof(OutlineDialog),
          new PropertyMetadata(AnchorSides.None, new PropertyChangedCallback(OnAnchorSideChanged))
        );

        public AnchorSides AnchorSide
        {
            get
            {
                return (AnchorSides)GetValue(AnchorSideProperty);
            }
            set
            {
                SetValue(AnchorSideProperty, value);
            }
        }

        private static void OnAnchorSideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OutlineDialog ctrl = d as OutlineDialog; //null checks omitted
            AnchorSides s = (AnchorSides)e.NewValue; //null checks omitted
            if (ctrl != null)
            {
                if (s == AnchorSides.None)
                {
                    ctrl.DockButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Windows.ApplicationModel.Resources.ResourceLoader _ResourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    ToolTip toolTip = new ToolTip();
                    ctrl.DockButton.Visibility = Visibility.Visible;
                    if (s == AnchorSides.Left)
                    {
                        ctrl.DockButtonFontIcon.Glyph = new string((char)0xE146, 1);
                        toolTip.Content = _ResourceLoader.GetString("OutlineDialog_DockButton_Right_ToolTip");
                        ctrl.SwitchDockSideButton.Content = _ResourceLoader.GetString("OutlineDialog_DockButton_Right_ToolTip");
                    }
                    else if (s == AnchorSides.Right)
                    {
                        ctrl.DockButtonFontIcon.Glyph = new string((char)0xE145, 1);
                        toolTip.Content = _ResourceLoader.GetString("OutlineDialog_DockButton_Left_ToolTip");
                        ctrl.SwitchDockSideButton.Content = _ResourceLoader.GetString("OutlineDialog_DockButton_Left_ToolTip");
                    }
                    ToolTipService.SetToolTip(ctrl.DockButton, toolTip);
                }
            }
        }

        private WidthLayouts _WidthLayout = WidthLayouts.Full;
        private WidthLayouts WidthLayout
        {
            get { return _WidthLayout; }
            set
            {
                if (_WidthLayout != value)
                {
                    _WidthLayout = value;
                    UpdateWidthLayout();
                }
            }
        }
        
        private void UpdateWidthLayout()
        {
            if (WidthLayout == WidthLayouts.Narrow)
            {
                VisualStateManager.GoToState(this, "Thin", false);
                if (ThumbnailsOptions.Flyout != null)
                {
                    ThumbnailsOptions.Flyout = null;
                }
                SecondaryColumnsButton.Flyout = NumColumnsFlyout;
            }
            else
            {
                VisualStateManager.GoToState(this, "Middle", false);
                if (SecondaryColumnsButton.Flyout != null)
                {
                    SecondaryColumnsButton.Flyout = null;
                }
                ThumbnailsOptions.Flyout = NumColumnsFlyout;
            }
        }
    }
}
