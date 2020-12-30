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

namespace CompleteReader.Documents.SubPages
{
    /// <summary>
    /// DocumentBasePage possesses a splitview that contains a frame. When navigating to the 
    /// ViewerPage, that splitview's frame is still in memory. Use this StandbyPage as a means
    /// of allowing the splitview's last frame to be navigated from and deactivated. 
    /// </summary>
    public sealed partial class StandbyPage : Page
    {
        public StandbyPage()
        {
            this.InitializeComponent();
        }
    }
}
