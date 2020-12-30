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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CompleteReader.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        SettingsViewModel _ViewModel;
        public SettingsPage()
        {
            this.InitializeComponent();
            _ViewModel = new SettingsViewModel();
            this.DataContext = _ViewModel;
            _ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            (FreeTextFontControl.DataContext as pdftron.PDF.Tools.ToolSettings.FreeTextFontViewModel).ExitRequested += FreeTextFontControl_ExitRequested;
        }

        private void FreeTextFontControl_ExitRequested(object sender, RoutedEventArgs e)
        {
            _ViewModel.CurrentView = SettingsViewModel.SettingsViews.Options;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName))
            {
                return;
            }
            if (e.PropertyName.Equals("CurrentView", StringComparison.OrdinalIgnoreCase))
            {
                FontControl.Reset();
                if (_ViewModel.CurrentView == SettingsViewModel.SettingsViews.Fonts)
                {
                    FontControl.InitFonts();
                }
                else if (_ViewModel.CurrentView == SettingsViewModel.SettingsViews.TextAnnotationFonts)
                {
                    FreeTextFontControl.SelectWhiteListedFonts();
                }
            }
        }
    }
}
