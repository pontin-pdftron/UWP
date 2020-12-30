using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using UIRect = Windows.Foundation.Rect;
using UIPoint = Windows.Foundation.Point;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.System.Profile;
using Windows.UI.ViewManagement;

namespace CompleteReader.Utilities
{
    partial class UtilityFunctions
    {
        public static Flyout ShowTimedFlyout(string message, FrameworkElement target, FlyoutPlacementMode placement)
        {
            Flyout flyout = new Flyout();
            Border b = new Border();
            flyout.Content = b;

            b.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 220, 210));

            Grid g = new Grid();
            b.Child = g;
            g.Margin = new Thickness(10);

            TextBlock block = new TextBlock();
            g.Children.Add(block);
            b.Tag = flyout;
            b.Tapped += (s, e) =>
            {
                Border bor = s as Border;
                Flyout fly = bor.Tag as Flyout;
                fly.Hide();
            };

            block.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            block.FontSize = 20;
            block.TextWrapping = TextWrapping.WrapWholeWords;
            block.MaxWidth = 350;
            block.Text = message;

            flyout.Placement = placement;
            flyout.ShowAt(target);
            DelayAndHideFlyout(flyout);
            return flyout;
        }

        private static async void DelayAndHideFlyout(Flyout flyout)
        {
            await Task.Delay(5000);
            flyout.Hide();
        }

        private static System.Text.RegularExpressions.Regex _Email_Regex;
        private static System.Text.RegularExpressions.Regex Email_Regex
        { 
            get
            {
                if (_Email_Regex == null)
                {
                    // This was found here: http://haacked.com/archive/2007/08/21/i-knew-how-to-validate-an-email-address-until-i.aspx/
                    //string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                    //  + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                    //  + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

                    // we're using a very simple one.
                    string pattern = @"^.+@.+\...+$";
                    _Email_Regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                return _Email_Regex;
            }
        }

        public static bool IsEmail(string url)
        {
            return !string.IsNullOrWhiteSpace(url) && Email_Regex.IsMatch(url);
        }

        public static string GetPagePresentationModeName(PDFViewCtrlPagePresentationMode mode)
        {
            string modeName = "Unknown Presentation Mode";
            switch (mode)
            {
                case PDFViewCtrlPagePresentationMode.e_single_continuous:
                    modeName = "Continuous";
                    break;
                case PDFViewCtrlPagePresentationMode.e_single_page:
                    modeName = "Single Page";
                    break;
                case PDFViewCtrlPagePresentationMode.e_facing:
                    modeName = "Facing";
                    break;
                case PDFViewCtrlPagePresentationMode.e_facing_cover:
                    modeName = "Facing Cover";
                    break;
            }
            return modeName;
        }

        #region File Source

        private static Regex _SkyDriveRegex = new Regex(@"\\SkyDrive\\");
        private static Regex _OneDriveRegex = new Regex(@"\\OneDrive\\");
        private static Regex _BoxRegex = new Regex(@"\\Packages\\[A-Za-z0-9_-]*\.Box[A-Za-z0-9_-]*\\LocalState\\");
        private static Regex _DropBoxRegex = new Regex(@"\\Packages\\[A-Za-z0-9_-]*\.Dropbox[A-Za-z0-9_-]*\\");
        private static Regex _SkyDriveTempFolder = new Regex(@"\\Packages\\.*skydrive.*\\LocalState\\OpenWithTempFolder");
        private static Regex _OneDriveTempFolder = new Regex(@"\\Packages\\.*onedrive.*\\LocalState\\OpenWithTempFolder");

        public static bool IsOneDrive(string path)
        {
            return _SkyDriveRegex.IsMatch(path) || _OneDriveRegex.IsMatch(path);
        }

        public static bool IsBox(string path)
        {
            return _BoxRegex.IsMatch(path);
        }

        public static bool IsDropBox(string path)
        {
            return string.IsNullOrEmpty(path) || _DropBoxRegex.IsMatch(path);
        }

        public static bool IsRemoteStorage(string path)
        {
            return IsOneDrive(path) || IsBox(path) || IsDropBox(path);
        }

        public static bool IsOneDriveTempFile(string path)
        {
            return _OneDriveTempFolder.IsMatch(path) || _SkyDriveTempFolder.IsMatch(path);
        }

        public static bool ShouldAutosave(string path)
        {
            return !IsRemoteStorage(path) && !IsOneDrive(path);
        }

        // returns true if we can add the file to the recent list safely.
        public static bool DoesFileBelongInRecentList(Windows.Storage.StorageFile file)
        {
            if (file == null)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(file.Path))
            {
                return false;
            }
            else
            {
                string pathy = file.Path;
                bool isDropBox = UtilityFunctions.IsDropBox(pathy);
                bool isBox = UtilityFunctions.IsBox(pathy);
                bool isOneDrive = UtilityFunctions.IsOneDrive(pathy);
                if (UtilityFunctions.IsRemoteStorage(file.Path) && !UtilityFunctions.IsOneDrive(file.Path))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool NeedsReadWriteStream(Windows.Storage.StorageFile file)
        {
            return IsBox(file.Path) || IsDropBox(file.Path);
        }

        public static async Task<StorageFile> SaveToTemporaryFileAsync(PDFDoc doc, string newName, bool cleanTempfolder = true)
        {
            StorageFile file = await GetTemporarySaveFileAsync(newName, cleanTempfolder);
            if (file != null)
            {
                try
                {
                    await doc.SaveAsync(file, pdftron.SDF.SDFDocSaveOptions.e_incremental);
                    return file;
                }
                catch (Exception) { }
            }

            return null;
        }

        public static async Task<StorageFolder> GetTemporarySaveFilefolderAsync(bool cleanTempfolder = true)
        {
            StorageFolder folder = null;
            try
            {
                folder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("TemporaryPDFS",
                    Windows.Storage.CreationCollisionOption.OpenIfExists);
            }
            catch (Exception) { }

            if (folder == null)
            {
                try
                {
                    folder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("TemporaryPDFS",
                        Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                }
                catch (Exception) { }
            }
            
            if (folder != null && cleanTempfolder && CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.IsReady)
            {
                try
                {
                    IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                    foreach(StorageFile file in files)
                    {
                        if (!CompleteReader.ViewModels.Viewer.Helpers.CompleteReaderTabControlViewModel.Instance.ContainsFile(file))
                        {
                            await file.DeleteAsync();
                        }
                    }
                }
                catch (Exception) { }
            }

            return folder;
        }

        public static async Task<StorageFile> GetTemporarySaveFileAsync(string newName, bool cleanTempfolder = true)
        {
            StorageFolder folder = await GetTemporarySaveFilefolderAsync(cleanTempfolder);

            if (folder != null)
            {
                StorageFile file = null;
                if (file == null)
                {
                    try
                    {
                        file = await folder.CreateFileAsync(newName, CreationCollisionOption.GenerateUniqueName);
                    }
                    catch (Exception) { }
                }
                return file;
            }
            return null;
        }

        /// <summary>
        /// Removes invalid characters from a file name. Will return temp.ext if there are no valid characters left.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string SanitizeFileName(string filename)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string invalidString = Regex.Escape(new string(invalidChars));
            string valid = Regex.Replace(filename, "[" + invalidString + "]", "");
            string extension = System.IO.Path.GetExtension(valid);
            string name = System.IO.Path.GetFileNameWithoutExtension(valid);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "temp";
            }
            return name + extension;
        }

        /// <summary>
        /// Gets a maximal file path, meaning that if the StorageFile object has a file path, we use that. Otherwise, we use the file name.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetMaximalAvailablePath(StorageFile file)
        {
            string path = file.Path;
            if (string.IsNullOrEmpty(path))
            {
                path = file.Name;
            }

            return path;
        }

        public static string ShortenFileName(string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName);
            string fn = System.IO.Path.GetFileNameWithoutExtension(fileName);
            if (!string.IsNullOrWhiteSpace(fn) && fn.Length > 1)
            {
                fn = fn.Substring(0, fn.Length / 2);
                if (!string.IsNullOrWhiteSpace(ext))
                {
                    fn = fn + ext;
                    return fn;
                }
            }
            return fileName;
        }

        public static bool IsFilePathInList(IList<StorageFile> files, string filePath)
        {
            foreach (StorageFile f in files)
            {
                if (f != null && !string.IsNullOrEmpty(f.Path) && f.Path.Equals(filePath))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsFileInList(IList<StorageFile> files, StorageFile file)
        {
            foreach (StorageFile f in files)
            {
                if (AreFilesEqual(f, file))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AreFilesEqual(StorageFile file1, StorageFile file2)
        {
            if (file1 == null && file2 == null)
            {
                return true;
            }
            else if (file1 != null && file2 != null)
            {
                return file1.IsEqual(file2);

                // TODO Phone? Is this necessary on Win10
                //string path1 = file1.Path;
                //string path2 = file2.Path;
                //if (!string.IsNullOrEmpty(path1) && !string.IsNullOrEmpty(path2))
                //{
                //    return path1.Equals(path2);
                //}
            }
            return false;
        }

        public static async Task<Tuple<DateTimeOffset, ulong>> GetDateModifiedAndSizeAsync(StorageFile file)
        {
            if (file == null)
            {
                return new Tuple<DateTimeOffset, ulong>(DateTimeOffset.MinValue, 0);
            }
            Windows.Storage.FileProperties.BasicProperties props = await file.GetBasicPropertiesAsync();
            return new Tuple<DateTimeOffset, ulong>(props.DateModified, props.Size);
        }

        public static async Task RemoveReadOnlyAndTemporaryFlagsAsync(StorageFile file)
        {
            string fileAttributeString = "System.FileAttributes";
            List<string> props = new List<string>();
            props.Add(fileAttributeString);
            IDictionary<string, object> propertyDictionary = await file.Properties.RetrievePropertiesAsync(props);
            if (propertyDictionary.ContainsKey(fileAttributeString))
            {
                uint properties = (uint)propertyDictionary[props[0]];
                uint temporaryProp = (uint)FileAttributes.Temporary;
                uint nonTemporaryMark = 0xFFFFFFFF ^ temporaryProp;
                uint readOnlyProp = (uint)FileAttributes.ReadOnly;
                uint nonReadOnlyMask = 0xFFFFFFFF ^ readOnlyProp;
                properties &= nonTemporaryMark;
                properties &= nonReadOnlyMask;
                propertyDictionary[fileAttributeString] = properties;
                await file.Properties.SavePropertiesAsync(propertyDictionary);
            }
        }

        #endregion File Source

        #region Internet

        public static bool DEBUG_TOGGLE_INTERNET_ON = true;
        public static bool HasInternet()
        {
#if DEBUG
            if (!DEBUG_TOGGLE_INTERNET_ON)
            {
                return false;
            }
#endif
            Windows.Networking.Connectivity.ConnectionProfile connections = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            
            bool hasInternet = connections != null &&
                connections.GetNetworkConnectivityLevel() == Windows.Networking.Connectivity.NetworkConnectivityLevel.InternetAccess;
            return hasInternet;
        }

        public static bool HasWifiInternet()
        {
            Windows.Networking.Connectivity.ConnectionProfile connections = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            bool hasWifi = connections != null && connections.IsWlanConnectionProfile;
            return hasWifi;
        }


#endregion Internet

        public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        return (T)child;
                    }

                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }

        public static FrameworkElement FindVisualChildByName(DependencyObject depObj, string name)
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null)
                    {
                        FrameworkElement childElem = child as FrameworkElement;
                        if (childElem != null && name.Equals(childElem.Name))
                        {
                            return childElem;
                        }
                    }

                    FrameworkElement childItem = FindVisualChildByName(child, name);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }

        public static UIRect GetElementRect(Windows.UI.Xaml.FrameworkElement element, Windows.UI.Xaml.FrameworkElement target = null)
        {
            Windows.UI.Xaml.Media.GeneralTransform elementtransform = element.TransformToVisual(target);
            UIRect rect = elementtransform.TransformBounds(new UIRect(new UIPoint(0, 0), new Windows.Foundation.Size(element.ActualWidth, element.ActualHeight)));
            return rect;
        }

        public static void HideElement(FrameworkElement element)
        {
            element.Opacity = 0;
            element.IsHitTestVisible = false;
            element.Width = 0;
        }

        public static void ShowElement(FrameworkElement element, double width)
        {
            element.Opacity = 1;
            element.IsHitTestVisible = true;
            element.Width = width;
        }

        public static Size GetUnscaledScreenSize()
        {
            var bounds = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            return new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);
        }

        // Calculates the color of a point in a rectangle that is filled
        // with a LinearGradientBrush.
        public static Color GetColorAtPoint(Grid grid, UIPoint thePoint)
        {
            // Get properties
            LinearGradientBrush br = (LinearGradientBrush)grid.Background;

            double y3 = thePoint.Y;
            double x3 = thePoint.X;

            double x1 = br.StartPoint.X * grid.ActualWidth;
            double y1 = br.StartPoint.Y * grid.ActualHeight;
            Windows.Foundation.Point p1 = new Windows.Foundation.Point(x1, y1); // Starting point

            double x2 = br.EndPoint.X * grid.ActualWidth;
            double y2 = br.EndPoint.Y * grid.ActualHeight;
            Windows.Foundation.Point p2 = new Windows.Foundation.Point(x2, y2);  // End point

            // Calculate intersecting points 
            Windows.Foundation.Point p4 = new Windows.Foundation.Point(); // with tangent

            if (y1 == y2) // Horizontal case
            {
                p4 = new Windows.Foundation.Point(x3, y1);
            }

            else if (x1 == x2) // Vertical case
            {
                p4 = new Windows.Foundation.Point(x1, y3);
            }

            else // Diagnonal case
            {
                double m = (y2 - y1) / (x2 - x1);
                double m2 = -1 / m;
                double b = y1 - m * x1;
                double c = y3 - m2 * x3;

                double x4 = (c - b) / (m - m2);
                double y4 = m * x4 + b;
                p4 = new Windows.Foundation.Point(x4, y4);
            }

            // Calculate distances relative to the vector start
            double d4 = dist(p4, p1, p2);
            double d2 = dist(p2, p1, p2);

            double x = d4 / d2;

            // Clip the input if before or after the max/min offset values
            double max = br.GradientStops.Max(n => n.Offset);
            if (x > max)
            {
                x = max;
            }
            double min = br.GradientStops.Min(n => n.Offset);
            if (x < min)
            {
                x = min;
            }

            // Find gradient stops that surround the input value
            GradientStop gs0 = br.GradientStops.Where(n => n.Offset <= x).OrderBy(n => n.Offset).Last();
            GradientStop gs1 = br.GradientStops.Where(n => n.Offset >= x).OrderBy(n => n.Offset).First();

            float y = 0f;
            if (gs0.Offset != gs1.Offset)
            {
                y = (float)((x - gs0.Offset) / (gs1.Offset - gs0.Offset));
            }

            // Interpolate color channels
            Color cx = new Color();
            if (br.ColorInterpolationMode == ColorInterpolationMode.ScRgbLinearInterpolation)
            {
                byte aVal = (byte)((gs1.Color.A - gs0.Color.A) * y + gs0.Color.A);
                byte rVal = (byte)((gs1.Color.R - gs0.Color.R) * y + gs0.Color.R);
                byte gVal = (byte)((gs1.Color.G - gs0.Color.G) * y + gs0.Color.G);
                byte bVal = (byte)((gs1.Color.B - gs0.Color.B) * y + gs0.Color.B);
                cx = Color.FromArgb(aVal, rVal, gVal, bVal);
            }
            else
            {
                byte aVal = (byte)((gs1.Color.A - gs0.Color.A) * y + gs0.Color.A);
                byte rVal = (byte)((gs1.Color.R - gs0.Color.R) * y + gs0.Color.R);
                byte gVal = (byte)((gs1.Color.G - gs0.Color.G) * y + gs0.Color.G);
                byte bVal = (byte)((gs1.Color.B - gs0.Color.B) * y + gs0.Color.B);
                cx = Color.FromArgb(aVal, rVal, gVal, bVal);
            }
            return cx;
        }

        // Helper method for GetColorAtPoint
        // Returns the signed magnitude of a point on a vector with origin po and pointing to pf
        private static double dist(Windows.Foundation.Point px, Windows.Foundation.Point po, Windows.Foundation.Point pf)
        {
            double d = Math.Sqrt((px.Y - po.Y) * (px.Y - po.Y) + (px.X - po.X) * (px.X - po.X));
            if (((px.Y < po.Y) && (pf.Y > po.Y)) ||
                ((px.Y > po.Y) && (pf.Y < po.Y)) ||
                ((px.Y == po.Y) && (px.X < po.X) && (pf.X > po.X)) ||
                ((px.Y == po.Y) && (px.X > po.X) && (pf.X < po.X)))
            {
                d = -d;
            }
            return d;
        }

        public static Tuple<double, double, double> GetHSVFromRGB(Color color)
        {
            double h = 0;
            double s = 0;
            double v = 0;
            
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double cMax = Math.Max(b, Math.Max(r, g));
            double cMin = Math.Min(b, Math.Min(r, g));

            double delta = cMax - cMin;

            if (delta  == 0)
            {
                h = 0;
            }
            else if (cMax == r)
            {
                h = 60 * (((g - b) / delta) % 6);
            }
            else if (cMax == g)
            {
                h = 60 * (((b - r) / delta) + 2);
            }
            else
            {
                h = 60 * (((r - g) / delta) + 4);
            }

            if (cMax == 0)
            {
                s = 0;
            }
            else
            {
                s = (delta / cMax) * 100;
            }

            if (h < 0)
            {
                h += 360;
            }

            v = cMax * 100;

            return new Tuple<double, double, double>(h, s, v);
        }

        public static Color GetRGBFromHSV(double h, double s, double v)
        {
            Color color = new Color();

            double c = v/100.0 * s/100.0;
            double x = c * (1 - Math.Abs(((h / 60) % 2) - 1));
            double m = v/100.0 - c;

            double r = 0;
            double g = 0;
            double b = 0;

            if (h < 60)
            {
                r = c;
                g = x;
                b = 0;
            }
            else if (h < 120)
            {
                r = x;
                g = c;
                b = 0;
            }
            else if (h < 180)
            {
                r = 0;
                g = c;
                b = x;
            }
            else if (h < 240)
            {
                r = 0;
                g = x;
                b = c;
            }
            else if (h < 300)
            {
                r = x;
                g = 0;
                b = c;
            }
            else
            {
                r = c;
                g = 0;
                b = x;
            }

            color.A = 255;
            color.R = (byte)((r + m) * 255);
            color.G = (byte)((g + m) * 255);
            color.B = (byte)((b + m) * 255);

            return color;
        }

        public static int GetLuminance(Color color)
        {
            return (int)Math.Sqrt(0.241 * color.R * color.R + 0.691 * color.G * color.G + 0.068 * color.B * color.B);
        }

        private static int _NativeLoggingState = 0;
        public static void SetNativeLoggingState(int state)
        {
            if (state != _NativeLoggingState)
            {
                _NativeLoggingState = state;
                if (state == 1 && App.CAN_TURN_ON_LOGGING)
                {
                    string fileName = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".log";
                    string folder = System.IO.Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Logs");
                    pdftron.PDF.PDFNetInternalTools.SetLogLocation(folder, fileName);
                    pdftron.PDF.PDFNetInternalTools.SetLogLevel(InternalToolsLogLevel.eTrace);
                    //PDFNetInternalTools.SetThresholdForLogStream("Paragraph", InternalToolsLogLevel.eDisabled);
                    //PDFNetInternalTools.SetThresholdForLogStream("Word2FlowConverter", InternalToolsLogLevel.eDisabled);
                    //PDFNetInternalTools.SetThresholdForLogStream("fonts", InternalToolsLogLevel.eDisabled);
                    //PDFNetInternalTools.SetThresholdForLogStream("tracer", InternalToolsLogLevel.eDisabled);
                    //PDFNetInternalTools.SetThresholdForLogStream("doclocker", InternalToolsLogLevel.eDisabled);
                    //pdftron.PDF.PDFNetInternalTools.SetCutoffLogThreshold(InternalToolsLogLevel.eDebug);
                    pdftron.PDF.PDFNetInternalTools.DisableLogBackend(InternalToolsLogBackend.eDebugger);
                    System.Diagnostics.Debug.WriteLine("Created log file in {0} - {1}", folder, fileName);
                }
                else
                {
                    pdftron.PDF.PDFNetInternalTools.SetCutoffLogThreshold(InternalToolsLogLevel.eDisabled);
                }
            }
        }

        public static void SetWebFontDownloaderState()
        {
            try
            {
                bool turnOn = HasInternet();
                if (turnOn && GetDeviceFormFactorType() == DeviceFormFactorType.Phone)
                {
                    turnOn = HasWifiInternet();
                }
                if (turnOn)
                {
                    WebFontDownloader.EnableDownloads();
                    WebFontDownloader.PreCacheAsync();
                    System.Diagnostics.Debug.WriteLine("Turning on web fonts");
                }
                else
                {
                    WebFontDownloader.DisableDownloads();
                    System.Diagnostics.Debug.WriteLine("Turning off web fonts");
                }
            }
            catch (Exception) { }
        }

        public enum DeviceFormFactorType
        {
            Phone,
            Desktop,
            Tablet,
            IoT,
            SurfaceHub,
            Other
        }

        public static DeviceFormFactorType GetDeviceFormFactorType()
        {
            switch (AnalyticsInfo.VersionInfo.DeviceFamily)
            {
                case "Windows.Mobile":
                    return DeviceFormFactorType.Phone;
                case "Windows.Desktop":
                    return DeviceFormFactorType.Desktop;
                case "Windows.Universal":
                    return DeviceFormFactorType.IoT;
                case "Windows.Team":
                    return DeviceFormFactorType.SurfaceHub;
                default:
                    return DeviceFormFactorType.Other;
            }
        }

        
        public static async Task SetFullScreenModeAsync(bool fullScreen)
        {
            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                if (fullScreen)
                {
                    await statusBar.HideAsync();
                }
                else
                {
                    await statusBar.ShowAsync();
                }
            }
            else
            {
                if (fullScreen)
                {
                    if (!ApplicationView.GetForCurrentView().TryEnterFullScreenMode())
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to set fullscreen mode");
                    }
                }
                else
                {
                    ApplicationView.GetForCurrentView().ExitFullScreenMode();
                }
            }
        }

        public static async void SetFullScreenModeDontWait(bool fullscreen)
        {
            await SetFullScreenModeAsync(fullscreen);
        }

        public static bool IsFullScreen()
        {
            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                return Settings.Settings.PhoneFullScreen == 1;
            }
            bool fs = ApplicationView.GetForCurrentView().IsFullScreenMode;
            return fs;
        }

        public static Color GetViewerBackgroundColor(Color pageColor)
        {
            pdftron.PDF.Tools.UtilityFunctions.HSV hsv = pdftron.PDF.Tools.UtilityFunctions.RGBtoHSV(new pdftron.PDF.Tools.UtilityFunctions.RGB(pageColor.R, pageColor.G, pageColor.B));
            double lowEarthHue = 0.05;
            double hightEarthHue = 0.11;
            bool earthTones = hsv.H >= lowEarthHue && hsv.H <= hightEarthHue;
            if (hsv.V > 0.5)
            {
                if (earthTones)
                {
                    hsv.V -= 0.2;
                    hsv.S = Math.Min(hsv.S * 2, Math.Min(hsv.S + 0.05, 1.0));
                }
                else
                {
                    hsv.V *= 0.6;
                }
            }
            else if (hsv.V >= 0.3)
            {
                hsv.V = (hsv.V / 2) + 0.05;
            }
            else if (hsv.V >= 0.1)
            {
                hsv.V -= 0.1;
            }
            else
            {
                hsv.V += 0.1;
            }
            if (!earthTones)
            {
                double dist = Math.Min(0.05, lowEarthHue - hsv.H);
                if (hsv.H > hightEarthHue)
                {
                    dist = Math.Min(0.05, hsv.H - hightEarthHue);
                }
                hsv.S = hsv.S - (hsv.S * (20 * dist) * 0.6);
            }
            pdftron.PDF.Tools.UtilityFunctions.RGB rgb = pdftron.PDF.Tools.UtilityFunctions.HSVtoRGB(hsv);
            return Color.FromArgb(255, (byte)rgb.R, (byte)rgb.G, (byte)rgb.B);
        }

        static public bool MatchesColorPostProcessMode(PDFViewCtrl ctrl, PDFRasterizerColorPostProcessMode mode, Color newWhite, Color newblack)
        {
            if (ctrl.GetColorPostProcessMode() != mode)
            {
                return false;
            }
            else if (mode != PDFRasterizerColorPostProcessMode.e_postprocess_gradient_map)
            {
                return true;
            }

            ColorPt white = new ColorPt(1, 1, 1);
            ColorPt processedWhite = ctrl.GetPostProcessedColor(white);
            ColorPt black = new ColorPt(0, 0, 0);
            ColorPt processedBlack = ctrl.GetPostProcessedColor(black);

            Color currentWhite = Color.FromArgb(255, (byte)(255 * processedWhite.Get(0)), (byte)(255 * processedWhite.Get(1)), (byte)(255 * processedWhite.Get(2)));
            Color currentBlack = Color.FromArgb(255, (byte)(255 * processedBlack.Get(0)), (byte)(255 * processedBlack.Get(1)), (byte)(255 * processedBlack.Get(2)));

            int rwDiff = Math.Abs((int)newWhite.R - (int)currentWhite.R);
            int gwDiff = Math.Abs((int)newWhite.G - (int)currentWhite.G);
            int bwDiff = Math.Abs((int)newWhite.B - (int)currentWhite.B);

            int rbDiff = Math.Abs((int)newblack.R - (int)currentBlack.R);
            int gbDiff = Math.Abs((int)newblack.G - (int)currentBlack.G);
            int bbDiff = Math.Abs((int)newblack.B - (int)currentBlack.B);

            return rwDiff < 2 && gwDiff < 2 && bwDiff < 2 && rbDiff < 2 && gbDiff < 2 && bbDiff < 2;
        }
    }
}
