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
    public sealed partial class UserVoiceFeedbackForm : UserControl
    {
        public UserVoiceFeedbackForm()
        {
            this.InitializeComponent();
            this.DataContext = UserVoiceFeedbackFormViewModel.Current;
            UserVoiceFeedbackFormViewModel.Current.Viewer = FeedbackwebView;
        }

        private void WebViewHostGrid_sizeChanged(object sender, SizeChangedEventArgs e)
        {
            FeedbackwebView.Width = e.NewSize.Width * 1.66;
            FeedbackwebView.Height = e.NewSize.Height;
            FeedbackwebView.Margin = new Thickness(-e.NewSize.Width * 0.66, 0, 0, 0);
        }
    }
}
