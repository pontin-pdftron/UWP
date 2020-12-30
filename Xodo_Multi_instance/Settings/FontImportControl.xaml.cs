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

namespace CompleteReader.Settings
{
    public sealed partial class FontImportControl : UserControl
    {
        private FontImportViewModel _ViewModel;
        public FontImportControl()
        {
            this.InitializeComponent();
            _ViewModel = new CompleteReader.Settings.FontImportViewModel();
            this.DataContext = _ViewModel;
            _ViewModel.ClearSelectionRequested += ViewModel_ClearSelectionRequested;
        }

        private void ViewModel_ClearSelectionRequested(object sender, RoutedEventArgs e)
        {
            FontListView.SelectedItems.Clear();
        }

        public void Reset()
        {
            FontListView.SelectedItems.Clear();
        }

        public void InitFonts()
        {
            _ViewModel.InitCommand.Execute(null);
        }
    }
}
