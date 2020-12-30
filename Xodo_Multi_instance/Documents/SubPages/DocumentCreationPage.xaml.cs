using CompleteReader.Pages.Common;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.Document;
using CompleteReader.ViewModels.Document.SubViews;
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

namespace CompleteReader.Documents.SubPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DocumentCreationPage
    {
        public DocumentCreationPage()
        {
            this.InitializeComponent();
            this.SizeChanged += DocumentCreationPage_SizeChanged;
        }

        private void DocumentCreationPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Show/hide the text in the CommandBar.Content panel depending on screen width
            if (e.NewSize.Width < 450)
            {
                CommandBarContentPanel.Visibility = Visibility.Collapsed;

            }
            else
            {
                CommandBarContentPanel.Visibility = Visibility.Visible;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is INavigable)
            {
                this.DataContext = e.Parameter;
            }

            base.OnNavigatedTo(e);
        }
    }
}
