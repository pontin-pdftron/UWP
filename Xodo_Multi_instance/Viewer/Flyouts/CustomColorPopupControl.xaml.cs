using CompleteReader.ViewModels.Viewer.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CompleteReader.Viewer.Flyouts
{
    public sealed partial class CustomColorPopupControl : UserControl
    {
        private CustomColorViewModel _ViewModel;

        private bool _IsColorPressed;

        private bool _IsOpacityPressed;

        public CustomColorPopupControl()
        {
            this.InitializeComponent();
            this.DataContextChanged += CustomColorPopupControl_DataContextChanged;
            this.GradientGrid.SizeChanged += GradientGrid_SizeChanged;

            this.GradientGrid.PointerPressed += GradientGrid_PointerPressed;
            this.OpacityGradient.PointerPressed += OpacityGradient_PointerPressed;
            this.PopupGrid.PointerMoved += GradientGrid_PointerMoved;
            this.PopupGrid.PointerReleased += PopupGrid_PointerGone;
            this.PopupGrid.PointerCanceled += PopupGrid_PointerGone;
            this.PopupGrid.PointerCaptureLost += PopupGrid_PointerGone;
            this.PopupGrid.PointerExited += PopupGrid_PointerGone;

            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                ColorGridView.ItemTemplate = Resources["SmallColorModeIconTemplate"] as DataTemplate;
                ColorScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
        }

        /// <summary> 
        /// Centers the popup
        /// </summary>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CustomColorPopup.HorizontalOffset = e.NewSize.Width / -2;
            CustomColorPopup.VerticalOffset = e.NewSize.Height / -1.8;
        }

        private void PopupGrid_PointerGone(object sender, PointerRoutedEventArgs e)
        {
            _IsColorPressed = false;
            _IsOpacityPressed = false;
            this.PopupGrid.ReleasePointerCaptures();
        }

        /// <summary>
        /// If pressing the HS or V grid, calculate the new color from the position 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GradientGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_IsColorPressed)
            {
                Point point = e.GetCurrentPoint(ColorGradient).Position;
                point.X = Math.Max(0, point.X);
                point.X = Math.Min(point.X, ColorGradient.ActualWidth);
                point.Y = Math.Max(0, point.Y);
                point.Y = Math.Min(point.Y, ColorGradient.ActualHeight);
                SetColorFromPoint(point);
            }
            else if (_IsOpacityPressed)
            {
                Point point = e.GetCurrentPoint(OpacityGradient).Position;
                point.X = Math.Max(0, point.X);
                point.X = Math.Min(point.X, OpacityGradient.ActualWidth);
                point.Y = Math.Max(0, point.Y);
                point.Y = Math.Min(point.Y, OpacityGradient.ActualHeight);
                SetOpacityFromPoint(point);
            }
        }

        private void GradientGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _IsColorPressed = true;

            SetColorFromPoint(e.GetCurrentPoint(ColorGradient).Position);
        }

        private void OpacityGradient_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _IsOpacityPressed = true;

            SetOpacityFromPoint(e.GetCurrentPoint(OpacityGradient).Position);
        }

        /// <summary>
        /// This event saves the reference to the new ViewModel. Also ensures that only one ViewModel and its
        /// associated events are used without duplication. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CustomColorPopupControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_ViewModel != null)
            {
                _ViewModel.CustomColorSelected -= _ViewModel_CustomColorIconSelected;
            }

            _ViewModel = (CustomColorViewModel)args.NewValue;

            if (_ViewModel != null)
            {
                _ViewModel.CustomColorSelected += _ViewModel_CustomColorIconSelected;
            }
        }

        /// <summary>
        /// Calculate the new color when user presses on the HS grid
        /// </summary>
        /// <param name="point"></param>
        private void SetColorFromPoint(Point point)
        {
            double h = point.X / ColorGradient.ActualWidth * 360;
            double s = 100 - point.Y / ColorGradient.ActualHeight * 100;
            double v = 100 - (((double)OpacityCircle.GetValue(Canvas.TopProperty) + OpacityCircle.ActualHeight / 2) / OpacityGradient.ActualHeight * 100);

            Color color = Utilities.UtilityFunctions.GetRGBFromHSV(h, s, v);
            SetCircleStrokeColor(color);
            SetColorCirclePos(point);

            _ViewModel.UpdateColor(color, true);
        }

        /// <summary>
        /// Calculate the new color when user presses on the V grid
        /// </summary>
        /// <param name="point"></param>
        private void SetOpacityFromPoint(Point point)
        {
            Windows.UI.Color color = Utilities.UtilityFunctions.GetColorAtPoint(OpacityGradient, point);
            SetCircleStrokeColor(color);
            SetOpacityCirclePos(point.Y);

            _ViewModel.UpdateColor(color, false);
        }

        /// <summary>
        /// Once a custom color icon has been pressed, initialize its HSV position on the UI 
        /// </summary>
        /// <param name="icon"></param>
        private void _ViewModel_CustomColorIconSelected(CustomColorIcon icon)
        {
            if (ColorGradient.ActualHeight == 0)
                return;

            SetHSVCircleInitPos();
            SetValueGradient();
        }

        private void GradientGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height == 0)
                return;

            SetHSVCircleInitPos();
            SetValueGradient();
        }

        /// <summary>
        /// Sets the initial position for the circles in the HS and V grids
        /// </summary>
        private void SetHSVCircleInitPos()
        {
            Tuple<double, double, double> hsv = Utilities.UtilityFunctions.GetHSVFromRGB(_ViewModel.CurrEditBrush);

            Point point = new Point();
            point.X = (hsv.Item1 / 360.0) * ColorGradient.ActualWidth;
            point.Y = (1.0 - (hsv.Item2 / 100.0)) * ColorGradient.ActualHeight;
            SetColorCirclePos(point);

            SetCircleStrokeColor(_ViewModel.CurrEditBrush);

            double y = (1.0 - (hsv.Item3 / 100.0)) * OpacityGradient.ActualHeight;
            SetOpacityCirclePos(y);
        }

        /// <summary>
        /// Set the gradient (topmost color) for the V grid
        /// </summary>
        private void SetValueGradient()
        {
            Tuple<double, double, double> hsv = Utilities.UtilityFunctions.GetHSVFromRGB(_ViewModel.CurrEditBrush);
            Color color = Utilities.UtilityFunctions.GetRGBFromHSV(hsv.Item1, hsv.Item2, 100);
            _ViewModel.CurrEditBrushGradient = color;
        }

        private void SetColorCirclePos(Point point)
        {
            point.X -= ColorCircle.ActualWidth / 2;
            point.Y -= ColorCircle.ActualHeight / 2;

            double x = Math.Max(ColorCircle.ActualWidth / -2, point.X);
            x = Math.Min(x, ColorGradient.ActualWidth - ColorCircle.ActualWidth / 2);
            double y = Math.Max(ColorCircle.ActualHeight / -2, point.Y);
            y = Math.Min(y, ColorGradient.ActualHeight - ColorCircle.ActualHeight / 2);

            ColorCircle.SetValue(Canvas.LeftProperty, x);
            ColorCircle.SetValue(Canvas.TopProperty, y);
        }

        private void SetOpacityCirclePos(double pointY)
        {
            pointY -= OpacityCircle.ActualHeight / 2;
            double y = Math.Max(OpacityCircle.ActualHeight / -2, pointY);
            y = Math.Min(y, OpacityGradient.ActualHeight - OpacityCircle.ActualHeight / 2);
            OpacityCircle.SetValue(Canvas.TopProperty, y);
        }

        /// <summary>
        /// Checks whether to set the HS and V circle markers to be white/black depending on the current color 
        /// </summary>
        /// <param name="color"></param>
        private void SetCircleStrokeColor(Color color)
        {
            SolidColorBrush brush = Utilities.UtilityFunctions.GetLuminance(color) > 130 ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White);
            ColorCircle.Stroke = brush;
            OpacityCircle.Stroke = brush;
        }
    }
}
