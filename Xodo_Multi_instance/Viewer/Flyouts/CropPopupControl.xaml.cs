using CompleteReader.ViewModels.Viewer.Helpers;
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

namespace CompleteReader.Viewer.Flyouts
{
    public sealed partial class CropPopupControl : UserControl
    {
        public CropPopupControl()
        {
            this.InitializeComponent();
        }

        /// <summary> 
        /// Centers the popup
        /// </summary>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CropPopup.HorizontalOffset = e.NewSize.Width / -2;
            CropPopup.VerticalOffset = e.NewSize.Height / -1.8;
        }
    }
}
