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
    public sealed partial class PasswordPopupControl : UserControl
    {
        public PasswordPopupControl()
        {
            this.InitializeComponent();
        }

        ///<summary> 
        /// Centers the popup and focuses the password box when this dialog is brought into view
        /// </summary>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PasswordPopup.HorizontalOffset = e.NewSize.Width / -2;
            PasswordPopup.VerticalOffset = e.NewSize.Height / -1.8;
            PasswordEntry.Focus(FocusState.Programmatic);
        }
    }
}
