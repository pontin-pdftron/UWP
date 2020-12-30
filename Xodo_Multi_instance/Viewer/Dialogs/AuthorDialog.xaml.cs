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
    public sealed partial class AuthorDialog : UserControl, pdftron.PDF.Tools.Utilities.IAuthorDialog
    {
        public AuthorDialog()
        {
            this.InitializeComponent();
        }

        public void SetAuthorViewModel(pdftron.PDF.Tools.Controls.ViewModels.AuthorDialogViewModel ViewModel)
        {
            this.DataContext = ViewModel;
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}
