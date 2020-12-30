using CompleteReader.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using CompleteReader.Documents;
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;
using Windows.UI.Core;
using CompleteReader.Settings;

// TODO Phone ?
using Windows.UI.ApplicationSettings;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace CompleteReader
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        public const bool CAN_TURN_ON_LOGGING = false;
        public const int LOGGING_ON_BY_DEFAULT = 1; // 0 - off, 1 - on

        public static App Current { get; private set; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            Current = this;
            this.RequestedTheme = SharedSettings.ThemeOption;
            pdftron.PDFNet.AddResourceSearchPath(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Settings.Settings.FONT_DIRECTORY));
#if XODO
#if DEBUG
            //DeviceTasks.TaskHandler.ObfuscateKey();
#endif
            try
            {
                DeviceTasks.TaskHandler.InitializeTaskHandling();
            }
            catch (Exception)
            {
            }
#else
            pdftron.PDFNet.Initialize("Insert commercial license key here after purchase");
#endif


#if IS_64_BIT
            uint cacheSize = 800;
#else
            uint cacheSize = 400;
            if (UtilityFunctions.GetDeviceFormFactorType() == UtilityFunctions.DeviceFormFactorType.Phone)
            {
                cacheSize = 200;
            }
#endif
            var getSystemFonts = pdftron.PDF.Tools.UtilityFunctions.FontBundle.GetSystemFontListAsync();
#if XODO
            pdftron.PDFNet.AddFontSubst((pdftron.CharacterOrdering)56, "Arial");
#endif
            pdftron.PDFNet.SetViewerCache(cacheSize * 1024 * 1024, true);
            pdftron.PDFNet.SetDefaultDiskCachingEnabled(true);

            UtilityFunctions.SetWebFontDownloaderState();
            pdftron.PDF.Analytics.AnalyticsHandler.Instance.Report += Instance_Report;

            string res_path = Windows.Storage.ApplicationData.Current.LocalFolder.Path;

            //pdftron.PDFNet.SetResourcesPath(res_path);
            //pdftron.PDFNet.SetPersistentCachePath(res_path);
            //string additioanlResourcePath = System.IO.Path.Combine(Package.Current.InstalledLocation.Path, "Resources");
            //pdftron.PDFNet.AddResourceSearchPath(additioanlResourcePath);
            
            pdftron.PDF.PDFViewCtrl.RegisterDependencyProperties();

            AnalyticsHandler.CURRENT = new AnalyticsHandler();

            /*
            try
            {
                uint numthumbs = 25;
                pdftron.Common.RecentlyUsedCache.InitializeRecentlyUsedCache(numthumbs, numthumbs * 2 * 1024 * 1024, 0.3);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to initialize RecentlyUsedCache: " + e.Message);
            }

            
            pdftron.PDF.DocumentPreviewCache.Initialize(100 * 1024 * 1024, 0.1);
            pdftron.PDF.ReflowProcessor.Initialize();
            pdftron.PDF.DocumentPreviewCache.ClearCache(); // attempt to prevent startup crash */

            // Setup AnalyticsHandler
            pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.CURRENT = AnalyticsHandler.CURRENT;
#if XODO
            Settings.Settings.DisplayName = "Xodo";
            Settings.Settings.SupportName = "support@xodo.com";

#if XODO_STORE_RELEASE
            // google Analytics
            AnalyticsHandler.CURRENT.AddHandler(new AHGoogleAnalyticsHelper("UA-51014902-4")); // global

            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                AnalyticsHandler.CURRENT.AddHandler(new AHGoogleAnalyticsHelper("UA-51014902-10")); // phone
            }
            else
            {
                AnalyticsHandler.CURRENT.AddHandler(new AHGoogleAnalyticsHelper("UA-51014902-7")); // windows
            }
            

#else // XODO_STORE_RELEASE
            AnalyticsHandler.CURRENT.AddHandler(new AHGoogleAnalyticsHelper("UA-51014902-3"));
#endif // XODO_STORE_RELEASE

#endif

            Windows.Devices.Input.TouchCapabilities touch = new Windows.Devices.Input.TouchCapabilities();
            bool has_touch = touch.TouchPresent == 1;
            Windows.Devices.Input.MouseCapabilities mouse = new Windows.Devices.Input.MouseCapabilities();
            bool has_mouse = mouse.MousePresent == 1;

            AnalyticsHandler.CURRENT.AddCrashExtraData("Has Touch", has_touch.ToString());
            AnalyticsHandler.CURRENT.AddCrashExtraData("Has Mouse", has_mouse.ToString());

            if (has_mouse)
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.HARDWARE, "Can input with Mouse");
            if (has_touch)
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.HARDWARE, "Can input with Touch");


            this.Suspending += this.OnSuspending;
            this.Resuming += this.OnResuming;

            Utilities.UtilityFunctions.SetNativeLoggingState(Settings.Settings.LogNativeCode);
            if (!CAN_TURN_ON_LOGGING)
            {
                pdftron.PDF.PDFNetInternalTools.DisableLogBackend(pdftron.PDF.InternalToolsLogBackend.eDebugger);
                pdftron.PDF.PDFNetInternalTools.SetThresholdForLogStream("UWP_WRAPPER", pdftron.PDF.InternalToolsLogLevel.eDisabled);
                pdftron.PDF.PDFNetInternalTools.SetCutoffLogThreshold(pdftron.PDF.InternalToolsLogLevel.eDisabled);
            }

            try
            {
                DateTime lastAccessed = System.IO.Directory.GetLastAccessTime("C:\\Windows\\Fonts");
            }
            catch (UnauthorizedAccessException)
            {
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_SEVERE_EVENT_CATEGORY, "Access to C:\\Windows\\Fonts folder denied");
            }
            catch (Exception) { }
        }

        private static List<pdftron.PDF.Analytics.AnalyticsHandlerReportCode> ReportsMade = new List<pdftron.PDF.Analytics.AnalyticsHandlerReportCode>();
        private void Instance_Report(pdftron.PDF.Analytics.AnalyticsHandlerReportCode reportCode, string message, string payload)
        {
            if (!ReportsMade.Contains(reportCode))
            {
                ReportsMade.Add(reportCode);
                string reportString = string.Format("Error report of type: {0}\nmessage: {1}\npayload: {2}", reportCode, message, payload);
                System.Diagnostics.Debug.WriteLine(reportString);
                AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.EXCEPTION_SEVERE_EVENT_CATEGORY, reportString);
            }

        }

        /// <summary>
        /// Set a method to be executed before the app crashes (ex. save state)
        /// </summary>
        private void BugSenseLastBreathMethod()
        {
            //do stuff here, ex. save state etc ...
        }

        private Frame CreateRootFrame()
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async System.Threading.Tasks.Task RestoreStatusAsync(ApplicationExecutionState previousExecutionState)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (previousExecutionState == ApplicationExecutionState.Terminated)
            {
                // Restore the saved session state only when appropriate
                //try
                //{
                //    await SuspensionManager.RestoreAsync();
                //}
                //catch (SuspensionManagerException)
                //{
                //    //Something went wrong restoring state.
                //    //Assume there is no state and continue
                //}
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = CreateRootFrame();
            //Window.Current.Content = new DocumentBasePage(rootFrame);

            await RestoreStatusAsync(e.PreviousExecutionState);

            if (rootFrame.Content == null)
            {

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter

                if (!rootFrame.Navigate(typeof(DocumentBasePage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            ListenOnRootFrameDrop(rootFrame);

            SetUpTitleBarColor();

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        // Handle file activations.
        /// </summary>
        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.GENERAL, "Activated with file");

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active

            DateTime nowTime = DateTime.Now;
            int eventno = 0;

            if (rootFrame == null)
            {
                AnalyticsHandler.CURRENT.AddCrashExtraData(string.Format("[WinRT File Activation] ({0}.{1}:{2})", nowTime.Minute.ToString("d2"), nowTime.Second.ToString("d2"), eventno++), "RootFrame was null");

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    AnalyticsHandler.CURRENT.AddCrashExtraData(string.Format("[WinRT File Activation] ({0}.{1}:{2})", nowTime.Minute.ToString("d2"), nowTime.Second.ToString("d2"), eventno++), "It was terminated");
                    AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.GENERAL, "Was Terminated");
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            else
            {
                AnalyticsHandler.CURRENT.AddCrashExtraData(string.Format("[WinRT File Activation] ({0}.{1}:{2})", nowTime.Minute.ToString("d2"), nowTime.Second.ToString("d2"), eventno++), "RootFrame was not null");
            }

            // Allow root frame to interact with files by dragging outside of app
            ListenOnRootFrameDrop(rootFrame);

            SetUpTitleBarColor();

            if (ViewModels.Document.DocumentViewModel.Current.DocumentOpener != null)
            {
                ViewModels.Document.DocumentViewModel.Current.DocumentOpener.CancelDocumentOpening();
            }
            MessageDialogHelper.CancelLastMessageDialog();
            pdftron.PDF.Tools.UtilityFunctions.CloseAllOpenPopups();
            Viewer.ViewerPage viewerPage = rootFrame.Content as Viewer.ViewerPage;
            if (viewerPage == null)
            {
                if (!rootFrame.Navigate(typeof(CompleteReader.Viewer.ViewerPage)))
                {
                    throw new Exception("Failed to create initial page. Why?");
                }
                viewerPage = rootFrame.Content as Viewer.ViewerPage;
            }
            if (viewerPage != null)
            {
                viewerPage.ActivateWithFile(args.Files[0] as Windows.Storage.StorageFile);
            }

            // Ensure the current window is active
            Window.Current.Activate();
            AnalyticsHandler.CURRENT.AddCrashExtraData(string.Format("[WinRT File Activation] ({0}.{1}:{2})", nowTime.Minute.ToString("d2"), nowTime.Second.ToString("d2"), eventno++), "Activate Happened");
        }


        public CompleteReader.ViewModels.Viewer.ViewerViewModel ActiveViewer { get; set; }
        public CompleteReader.ViewModels.Document.SubViews.FolderDocumentsViewModel ActiveFolderDocumentsViewModel { get; set; }
        public CompleteReader.ViewModels.Document.SubViews.RecentDocumentsViewModel ActiveRecentDocumentsViewModel { get; set; }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            if (ActiveViewer != null)
            {
                await ActiveViewer.SuspendAsync();
            }
            if (ActiveFolderDocumentsViewModel != null)
            {
                ActiveFolderDocumentsViewModel.StopAllActivity();
            }
            if (ActiveRecentDocumentsViewModel != null)
            {
                ActiveRecentDocumentsViewModel.StopAllActivity();
            }

            deferral.Complete();
        }

        private void OnResuming(object sender, object e)
        {
            if (ActiveViewer != null)
            {
                ActiveViewer.Resume();
            }

            if (ActiveFolderDocumentsViewModel != null)
            {
                ActiveFolderDocumentsViewModel.StartActivity();
            }
            if (ActiveRecentDocumentsViewModel != null)
            {
                ActiveRecentDocumentsViewModel.StartActivity();
            }
        }

        private void ListenOnRootFrameDrop(Frame rootFrame)
        {
            rootFrame.AllowDrop = true;
            if (_RootFrameDragOverHandler == null)
            {
                _RootFrameDragOverHandler = new DragEventHandler(RootFrame_DragOver);
                rootFrame.DragOver += _RootFrameDragOverHandler;
            }
            if (_RootFrameDropHandler == null)
            {
                _RootFrameDropHandler = new DragEventHandler(RootFrame_Drop);
                rootFrame.Drop += _RootFrameDropHandler;
            }
        }

        DragEventHandler _RootFrameDragOverHandler = null;
        DragEventHandler _RootFrameDropHandler = null;

        private void RootFrame_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = ResourceLoader.GetForCurrentView().GetString("App_RootFrameDrop_Caption");
        }

        private async void RootFrame_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                if (items.Count == 1)
                {
                    StorageFile storageFile = items[0] as StorageFile;

                    if (storageFile.FileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageDialogHelper.CancelLastMessageDialog();
                        Frame rootFrame = Window.Current.Content as Frame;

                        Viewer.ViewerPage viewerPage = rootFrame.Content as Viewer.ViewerPage;
                        if (viewerPage == null)
                        {
                            if (!rootFrame.Navigate(typeof(CompleteReader.Viewer.ViewerPage)))
                            {
                                throw new Exception("Failed to create initial page. Why?");
                            }
                            viewerPage = rootFrame.Content as Viewer.ViewerPage;
                        }
                        if (viewerPage != null)
                        {
                            ViewModels.FileOpening.NewDocumentProperties props = new ViewModels.FileOpening.NewDocumentProperties();
                            props.OpenedThroughDrop = true;
                            viewerPage.ActivateWithFile(storageFile, props);
                        }
                    }
                }
                // show an error message if user tries to drag more than 1 pdf
                else
                {
                    MessageDialog md = new MessageDialog(ResourceLoader.GetForCurrentView().GetString("App_RootFrameDrop_TooManyMessage"));
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                }
            }
        }

        private void SetUpTitleBarColor()
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            Windows.UI.Color? color = (Current.Resources["SystemChromeLowColor"]) as Windows.UI.Color?;

            titleBar.BackgroundColor = color;
            titleBar.InactiveBackgroundColor = color;
            titleBar.ButtonBackgroundColor = color;
            titleBar.ButtonInactiveBackgroundColor = color;
        }
    }
}