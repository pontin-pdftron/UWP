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
    public sealed partial class About : UserControl
    {
        public About()
        {
            this.InitializeComponent();

            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("About");

            AppNameTextBlock.Text = Settings.DisplayName + AppNameTextBlock.Text;
            string version = string.Format("{0}.{1}.{2}", Windows.ApplicationModel.Package.Current.Id.Version.Major, Windows.ApplicationModel.Package.Current.Id.Version.Minor, Windows.ApplicationModel.Package.Current.Id.Version.Revision);

            VersionTextBlock.Text = loader.GetString("Settings_About_Version") + " " + version;

            BuildTextBlock.Text = loader.GetString("Settings_About_Build") + " " + Windows.ApplicationModel.Package.Current.Id.Version.Build;

            PreLinkRun.Text = loader.GetString("Settings_About_PreLink");
            PostLinkRun.Text = loader.GetString("Settings_About_PostLink");
        }
    }
}
