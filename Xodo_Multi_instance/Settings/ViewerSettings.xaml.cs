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

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace CompleteReader.Settings
{
    public sealed partial class ViewerSettings : UserControl
    {
        public ViewerSettings()
        {
            this.InitializeComponent();
            if (!App.CAN_TURN_ON_LOGGING)
            {
                DeveloperHeaderStack.Visibility = Visibility.Collapsed;
            }
            ShowDeveloperOptionsButton.Click += ShowDeveloperOptionsButton_Click;
            OptionsScroller.SizeChanged += OptionsScroller_SizeChanged;
        }

        private void OptionsScroller_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollRootGrid.Width = e.NewSize.Width;
        }

        private void ShowDeveloperOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            DeveloperOptionsStack.Visibility = Visibility.Visible;
            ShowDeveloperOptionsButton.IsEnabled = false;

            OptionsScroller.LayoutUpdated += OptionsScroller_LayoutUpdated;
        }

        private void OptionsScroller_LayoutUpdated(object sender, object e)
        {
            OptionsScroller.ScrollToVerticalOffset(OptionsScroller.ScrollableHeight);
            OptionsScroller.LayoutUpdated -= OptionsScroller_LayoutUpdated;
        }
    }
}
