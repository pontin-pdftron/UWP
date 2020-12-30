using CompleteReader.Utilities;
using CompleteReader.ViewModels.Document.SubViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;

namespace CompleteReader.Settings
{
    public class SharedSettings
    {
        #region App Version
        
        // The below Class should work to the best of my (Tomas) knowledge, but we haven't been using it for anything so far.
        //public class AppVersion : IComparable<AppVersion>
        //{
        //    private static string MAJOR_NAME = "_Major";
        //    private static string MINOR_NAME = "_Minor";
        //    private static string BUILD_NAME = "_Build";
        //    private static string REVISION_NAME = "_Revision";

        //    /// <summary>
        //    /// First number in version
        //    /// </summary>
        //    public uint Major { get; set; }
        //    /// <summary>
        //    /// Second number in version
        //    /// </summary>
        //    public uint Minor { get; set; }
        //    /// <summary>
        //    /// Third number in version
        //    /// </summary>
        //    public uint Build { get; set; }
        //    /// <summary>
        //    /// Last number in version
        //    /// </summary>
        //    public uint Revision { get; set; }

        //    public static AppVersion CurrentVersion
        //    {
        //        get {
        //            return new AppVersion(Windows.ApplicationModel.Package.Current.Id.Version.Major,
        //                                    Windows.ApplicationModel.Package.Current.Id.Version.Minor,
        //                                    Windows.ApplicationModel.Package.Current.Id.Version.Build,
        //                                    Windows.ApplicationModel.Package.Current.Id.Version.Revision);
        //        }
        //    }

        //    public static AppVersion DefaultAppVersion
        //    {
        //        get { return new AppVersion(1, 0, 0, 0); }
        //    }

        //    public static AppVersion LastVersionForAutoSaveNotification
        //    {
        //        // This should be manually set every time you release. Otherwise, this message will not show
        //        get { return new AppVersion(2, 3, 2, 0); }
        //    }

        //    public AppVersion()
        //    {
        //        Major = 1;
        //        Minor = 0;
        //        Build = 0;
        //        Revision = 0;
        //    }

        //    public AppVersion(uint major, uint minor, uint build, uint revision)
        //    {
        //        Major = major;
        //        Minor = minor;
        //        Build = build;
        //        Revision = revision;
        //    }

        //    public void SaveAppVersion(Windows.Storage.ApplicationDataContainer settings, string prefix)
        //    {
        //        settings.Values[prefix + MAJOR_NAME] = Major;
        //        settings.Values[prefix + MINOR_NAME] = Minor;
        //        settings.Values[prefix + BUILD_NAME] = Build;
        //        settings.Values[prefix + REVISION_NAME] = Revision;
        //    }

        //    public static AppVersion LoadAppVersion(Windows.Storage.ApplicationDataContainer settings, string prefix)
        //    {
        //        try
        //        {
        //            if (settings.Values.ContainsKey(prefix + MAJOR_NAME))
        //            {
        //                AppVersion version = new AppVersion();
        //                version.Major = (uint)(settings.Values[prefix + MAJOR_NAME]);
        //                version.Minor = (uint)(settings.Values[prefix + MINOR_NAME]);
        //                version.Build = (uint)(settings.Values[prefix + BUILD_NAME]);
        //                version.Revision = (uint)(settings.Values[prefix + REVISION_NAME]);
        //                return version;
        //            }
        //        }
        //        catch (Exception) { }

        //        return AppVersion.DefaultAppVersion;
        //    }

        //    #region Comparing

        //    public int CompareTo(AppVersion other)
        //    { 
        //        if (other == null)
        //        {
        //            return 1;
        //        }
        //        if (Major == other.Major)
        //        {
        //            if (Minor == other.Minor)
        //            {
        //                if (Build == other.Build)
        //                {
        //                    if (Revision == other.Revision)
        //                    {
        //                        return 0;
        //                    }
        //                    else if (Revision < other.Revision)
        //                    {
        //                        return -1;
        //                    }
        //                    else
        //                    {
        //                        return 1;
        //                    }
        //                }
        //                else if (Build < other.Build)
        //                {
        //                    return -1;
        //                }
        //                else
        //                {
        //                    return 1;
        //                }
        //            }
        //            else if (Minor < other.Minor)
        //            {
        //                return -1;
        //            }
        //            else
        //            {
        //                return 1;
        //            }
        //        }
        //        else if (Major < other.Major)
        //        {
        //            return -1;
        //        }
        //        else
        //            return 1;
        //    }

        //    public static bool operator <(AppVersion e1, AppVersion e2)
        //    {
        //        return e1.CompareTo(e2) < 0;
        //    }

        //    public static bool operator >(AppVersion e1, AppVersion e2)
        //    {
        //        return e1.CompareTo(e2) > 0;
        //    }

        //    public static bool operator <=(AppVersion e1, AppVersion e2)
        //    {
        //        return e1.CompareTo(e2) <= 0;
        //    }

        //    public static bool operator >=(AppVersion e1, AppVersion e2)
        //    {
        //        return e1.CompareTo(e2) >= 0;
        //    }

        //    public static bool operator ==(AppVersion e1, AppVersion e2)
        //    {
        //        if (System.Object.ReferenceEquals(e1, e2))
        //        {
        //            return true;
        //        }

        //        if (((object)e1 != null) && ((object)e2 != null))
        //        {
        //            return e1.CompareTo(e2) == 0;
        //        }

        //        // If one is null, but not both, return false.
        //        if (((object)e1 == null) && ((object)e2 == null))
        //        {
        //            return true;
        //        }

        //        return false;
        //    }

        //    public static bool operator !=(AppVersion e1, AppVersion e2)
        //    {
        //        return e1.CompareTo(e2) != 0;
        //    }

        //    public override bool Equals(object obj)
        //    {
        //        AppVersion other = obj as AppVersion;
        //        if (other != null)
        //        {
        //            return other == this;
        //        }
        //        return false;
        //    }

        //    public override int GetHashCode()
        //    {
        //        return ToString().GetHashCode();
        //    }

        //    public override string ToString()
        //    {
        //        return string.Format("{0}.{1}.{2}.{3}", Major, Minor, Build, Revision);
        //    }

        //    #endregion Comparing
        //}

        #endregion App Version


        #region Events

        public delegate void SettingUpdatedDelegate(string settingName);
        public static event SettingUpdatedDelegate SettingUpdated;

        private static void RaiseSettingChangedDelegate([System.Runtime.CompilerServices.CallerMemberName] String settingName = "")
        {
            if (SettingUpdated != null)
            {
                SettingUpdated(settingName);
            }
        }

        #endregion Events

        private const string APP_VERSION_SETTING_NAME = "CompleteReader_AppVersion";

        private static readonly List<string> _PDFFileTypes = new List<string> { ".pdf" };
        private static readonly List<string> _StaticConversionTypes = new List<string> { ".xps", ".oxps", ".txt", ".xml", ".md" };
        private static readonly List<string> _OfficeFileTypes = new List<string> {  ".docx", ".doc", ".ppt", ".pptx", ".xls", ".xlsx"};
        private static readonly List<string> _ImageTypes = new List<string> { ".jpeg", ".jpg", ".png", ".bmp", ".cbz" };
        private static readonly List<string> _UniversalConversionFromString = new List<string> { };

        private static List<string> _AssociatedFileTypes = null;
        public static List<string> AssociatedFileTypes
        {
            get
            {
                if (_AssociatedFileTypes == null)
                {
                    _AssociatedFileTypes = new List<string>();
                    _AssociatedFileTypes.AddRange(_PDFFileTypes);
                    _AssociatedFileTypes.AddRange(_StaticConversionTypes);
                    _AssociatedFileTypes.AddRange(_OfficeFileTypes);
                    _AssociatedFileTypes.AddRange(_ImageTypes);
                    _AssociatedFileTypes.AddRange(_UniversalConversionFromString);
                }
                return _AssociatedFileTypes;
            }
        }

        private static List<string> _FILTER_PDFFileTypes = null;
        public static List<string> FILTER_PDFFileTypes
        {
            get
            {
                if (_FILTER_PDFFileTypes == null)
                {
                    _FILTER_PDFFileTypes = new List<string>();
                    _FILTER_PDFFileTypes.AddRange(_PDFFileTypes);
                }
                return _FILTER_PDFFileTypes;
            }
        }
        private static List<string> _FILTER_OffcieFileTypes = null;
        public static List<string> FILTER_OffcieFileTypes
        {
            get
            {
                if (_FILTER_OffcieFileTypes == null)
                {
                    _FILTER_OffcieFileTypes = new List<string>();
                    _FILTER_OffcieFileTypes.AddRange(_StaticConversionTypes);
                    _FILTER_OffcieFileTypes.AddRange(_OfficeFileTypes);
                }
                return _FILTER_OffcieFileTypes;
            }
        }
        private static List<string> _FILTER_ImageFileTypes = null;
        public static List<string> FILTER_ImageFileTypes
        {
            get
            {
                if (_FILTER_ImageFileTypes == null)
                {
                    _FILTER_ImageFileTypes = new List<string>();
                    _FILTER_ImageFileTypes.AddRange(_ImageTypes);
                }
                return _FILTER_ImageFileTypes;
            }
        }

        public enum FileOpeningType
        {
            PDF,
            StaticConversion,
            UniversalConversion,
            UniversalConversionFromString,
        }

        public static FileOpeningType GetFileOpeningType(string fileTypeWithDot)
        {
            if (_StaticConversionTypes.Contains(fileTypeWithDot, StringComparer.OrdinalIgnoreCase))
            {
                return FileOpeningType.StaticConversion;
            }
            if (_OfficeFileTypes.Contains(fileTypeWithDot, StringComparer.OrdinalIgnoreCase) || _ImageTypes.Contains(fileTypeWithDot, StringComparer.OrdinalIgnoreCase))
            {
                return FileOpeningType.UniversalConversion;
            }
            if (_UniversalConversionFromString.Contains(fileTypeWithDot, StringComparer.OrdinalIgnoreCase))
            {
                return FileOpeningType.UniversalConversionFromString;
            }
            return FileOpeningType.PDF;
        }

        public static FileOpeningType GetFileOpeningType(Windows.Storage.StorageFile file)
        {
            return GetFileOpeningType(file.FileType);
        }

        private static List<string> _FontFileTypes = new List<string> { ".ttf", ".otf" };
        public static List<string> FontFileTypes { get { return _FontFileTypes; } }
        public const string FONT_DIRECTORY = "fonts";

        private static IList<Tuple<Color, Color>> CUSTOM_COLOR_OPTIONS;
        private const int NUM_CUSTOM_COLORS = 15;

        private static Windows.Storage.StorageFolder _DefaultTempPDFLocation = Windows.Storage.ApplicationData.Current.TemporaryFolder;
        public static Windows.Storage.StorageFolder DefaultTempPDFLocation { get { return _DefaultTempPDFLocation; } }
        private static string _DefaultTempPDFName = "Untitled.pdf";
        public static string DefaultTempPDFName { get { return _DefaultTempPDFName; } }

        public static pdftron.PDF.PDFViewCtrlPagePresentationMode PAGE_PRESENTATION_MODE_DEFAULT = pdftron.PDF.PDFViewCtrlPagePresentationMode.e_single_page;
        private static bool REMEMBER_LAST_PAGE_DEFAULT = true;
        private static bool MAINTAIN_ZOOM_DEFAULT = true;
        private static bool ENABLEJAVASCRIPT_DEFAULT = true;
        private static bool BUTTONS_STAY_DOWN_DEFAULT = true;
        private static bool AUTO_SAVE_ON_DEFAULT = false;
        private static ViewModels.Viewer.Helpers.ViewerPageSettingsViewModel.CustomColorModes COLOR_MODE_DEFAULT = 0;
        private static ApplicationTheme THEME_SETTING_DEFAULT = ApplicationTheme.Light;
        private static pdftron.PDF.Tools.ToolManager.InkSmoothingOptions INK_SMOOTHING_DEFAULT = pdftron.PDF.Tools.ToolManager.InkSmoothingOptions.AllButStylus;
        private static string EMAIL_SIGNATURE_DEFAULT = "Sent from ";
        private static bool STYLUS_AS_PEN_DEFAULT = true;
        private static bool SCREEN_SLEEP_LOCK_DEFAULT = false;
        private static string MAINPAGE_PANEL_DEFAULT = "RecentPage";
        private static ViewModels.Document.SubViews.FolderDocumentsViewModel.IconView OPENED_ICON_VIEW_DEFAULT = ViewModels.Document.SubViews.FolderDocumentsViewModel.IconView.Default;
        private static ViewModels.Document.SubViews.FolderDocumentsViewModel.IconView FOLDER_ICON_VIEW_DEFAULT = ViewModels.Document.SubViews.FolderDocumentsViewModel.IconView.Default;
        private static ViewModels.Document.SubViews.RecentDocumentsViewModel.IconView RECENT_ICON_VIEW_DEFAULT = ViewModels.Document.SubViews.RecentDocumentsViewModel.IconView.Default;
        private static bool PIN_COMMAND_BAR_DEFAULT = true;
        private static int CUSTOM_COLOR_ICON_DEFAULT = 0;
        private static int LOG_NATIVE_CODE_DEFAULT = App.LOGGING_ON_BY_DEFAULT; // off
        private static bool OUTLINE_DIALOG_OPEN_DEFAULT = false;
        private static Viewer.Dialogs.OutlineDialog.AnchorSides OUTLINE_DIALOG_ANCHOR_SIDE_DEFAULT = Viewer.Dialogs.OutlineDialog.AnchorSides.Left;
        private static int OUTLINE_DIALOG_THUMBNAIL_NUM_COLUMNS_DEFAULT = 2;
        private static int PHONE_FULL_SCREEN_DEFAULT = 0;
        private static int OUTLINE_DIALOG_WIDTH_DEFAULT = 2;
        private static bool COPY_ANNOTATED_TEXT_TO_NOTE_DEFAULT = false;
        private static int OUTLINE_DEFAULT_VIEW_DEFAULT = 0;
        private static readonly int FILE_TYPE_FILTER_DEFAULT = 0x06;

        private static string CUSTOM_COLOR_OPTIONS_STRING =
            "241,244,245,0,0,0:54,59,61,0,0,0:191,216,194,64,75,64:208,217,227,41,60,88:222,206,172,91,67,46:233,205,213,170,69,92:153,255,204,0,0,0:153,204,255,0,0,0:52,48,46,137,135,132:52,45,44,252,220,220:90,95,55,252,220,220:64,47,33,222,206,172:8,48,69,219,225,227:53,59,61,135,150,153:0,51,0,255,255,255";

        private static string PAGE_PRESENTATION_MODE_SETTING_NAME = "CompleteReader_PagePresentationMode2";

        private static string REMEMBER_LAST_PAGE_SETTING_NAME = "CompleteReader_RememberLastPage";
        private static string MAINTAIN_ZOOM_SETTING_NAME = "CompleteReader_MaintainZoom";
        private static string ENABLEJAVASCRIPT_SETTING_NAME = "CompleteReader_EnableJavaScript";
        private static string BUTTON_STAY_DOWN_SETTING_NAME = "CompleteReader_ButtonsStayDown";
        private static string AUTO_SAVE_ON_SETTING_NAME = "CompleteReader_AutoSaveOn";
        private static string EMAIL_SIGNATURE_SETTING_NAME = "CompleteReader_EmailSignature";
        private static string COLOR_MODE_SETTING_NAME = "CompleteReader_ColorMode";
        private static string THEME_SETTING_NAME = "CompleteReader_Theme";
        private static string INK_SMOOTHING_SETTING_NAME = "CompleteReader_InkSmmothing";
        private static string STYLUS_AS_PEN_NAME = "CompleteReader_StylusAsPen";
        private static string SCREEN_SLEEP_LOCK = "CompleteReader_ScreenSleepLock";
        private static string FOLDER_PATH_LIST_NAME = "CompleteReader_FolderPathList";
        private static string MAINPAGE_PANEL_NAME = "CompleteReader_MainPagePanelName";
        private static string FOLDER_ICON_VIEW_NAME = "CompleteReader_FolderIconViewName";
        private static string RECENT_ICON_VIEW_NAME = "CompleteReader_RecentIconViewName";
        private static string OPENED_ICON_VIEW_NAME = "CompleteReader_OpenedIconViewName";
        private static string CUSTOM_COLOR_LIST_NAME = "CompleteReader_CustomColorListName";
        private static string CUSTOM_COLOR_LIST_INDEX_NAME = "CompleteReader_CustomColorListIndexName";
        private static string PIN_COMMAND_BAR_SETTING_NAME = "CompleteReader_PinCommandBar";
        private static string LOG_NATIVE_CODE_NAME = "CompleteReader_LogNativeCode";
        private static string OUTLINE_DIALOG_OPEN_NAME = "CompleteReader_OutlineDialogOpen";
        private static string OUTLINE_DIALOG_ANCHOR_SIDE_NAME = "CompleteReader_OutlineDialogAnchorSide";
        private static string OUTLINE_DIALOG_THUMBNAIL_NUM_COLUMNS_NAME = "CompleteReader_OutlineDialogNumThumbnailColumns";
        private static string PHONE_FULL_SCREEN_NAME = "CompleteReader_PhoneFullScreen";
        private static string COPY_ANNOTATED_TEXT_TO_NOTE_NAME = "CompleteReader_CopyAnnotatedText";
        private static string OUTLINE_DIALOG_WIDTH_NAME = "CompleteReader_OutlineDialogWidth";
        private static string DISPLAY_NAME = Windows.ApplicationModel.Package.Current.DisplayName;
        private static string SUPPORT_NAME = "developer_support@company.com";
        private static string OUTLINE_DEFAULT_VIEW_NAME = "CompleteReader_OutlineDefaultView";
        private static readonly string FILE_TYPE_FILTER_NAME = "Completereader_FilterPreference";

        // TODO Phone
        // private static string DISPLAY_NAME = Windows.ApplicationModel.Package.Current.Id.Name;

        private static Windows.Storage.ApplicationDataContainer _LocalSettings;
        private static Windows.Storage.ApplicationDataContainer LocalSettings
        {
            get { 
                if (null == _LocalSettings)
                {
                    _LocalSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                }
                return _LocalSettings;
            }
        }

        private static Windows.Storage.ApplicationDataContainer _RoamingSettings;
        private static Windows.Storage.ApplicationDataContainer RoamingSettings
        {
            get {
                if (_RoamingSettings == null)
                {
                    _RoamingSettings = _RoamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                }
                return _RoamingSettings;
            }
        }

        public static void DebugReset()
        {
            RoamingSettings.Values[PAGE_PRESENTATION_MODE_SETTING_NAME] = Defaults.PagePresentationMode;
            RoamingSettings.Values[BUTTON_STAY_DOWN_SETTING_NAME] = BUTTONS_STAY_DOWN_DEFAULT;
            RoamingSettings.Values[EMAIL_SIGNATURE_SETTING_NAME] = EMAIL_SIGNATURE_DEFAULT + Settings.DisplayName;
            RoamingSettings.Values[AUTO_SAVE_ON_SETTING_NAME] = AUTO_SAVE_ON_DEFAULT;

            RoamingSettings.Values[pdftron.PDF.Tools.ToolManager.AnnotationAuthorNameSettingsString] = "";
            RoamingSettings.Values[pdftron.PDF.Tools.ToolManager.DebugAnnotationAuthorHasBeenAskedSettingsString] = false;
        }

        #region Defaults

        public class Defaults
        {
            public const pdftron.PDF.PageRotate PageRotation = pdftron.PDF.PageRotate.e_0;
            public const pdftron.PDF.PDFViewCtrlPagePresentationMode PagePresentationMode = pdftron.PDF.PDFViewCtrlPagePresentationMode.e_single_page;
        }


        #endregion Defaults


        #region Settings

        public static string DisplayName
        {
            get { return DISPLAY_NAME; }
            set { DISPLAY_NAME = value; } // in case you don't want to use the display name in the app.
        }

        public static string SupportName
        {
            get { return SUPPORT_NAME; }
            set { SUPPORT_NAME = value; }
        }

        public static pdftron.PDF.PDFViewCtrlPagePresentationMode PagePresentationMode
        {
            get
            {
                try
                {
                    if (RoamingSettings.Values.ContainsKey(PAGE_PRESENTATION_MODE_SETTING_NAME))
                    {
                        if (RoamingSettings.Values[PAGE_PRESENTATION_MODE_SETTING_NAME] is bool)
                        {
                            bool isContinuous = (bool)RoamingSettings.Values[PAGE_PRESENTATION_MODE_SETTING_NAME];
                            if (isContinuous)
                            {
                                return pdftron.PDF.PDFViewCtrlPagePresentationMode.e_single_continuous;
                            }
                            else
                            {
                                return pdftron.PDF.PDFViewCtrlPagePresentationMode.e_single_page;
                            }
                        }
                        else // it's an int
                        {
                            int presMode = (int)RoamingSettings.Values[PAGE_PRESENTATION_MODE_SETTING_NAME];
                            return (pdftron.PDF.PDFViewCtrlPagePresentationMode)presMode;
                        }
                    }
                }
                catch (Exception) { }
                return Defaults.PagePresentationMode;
            }
            set
            {
                RoamingSettings.Values[PAGE_PRESENTATION_MODE_SETTING_NAME] = (int)value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool RememberLastPage
        {
            get
            {
                try
                {
                    if (RoamingSettings.Values.ContainsKey(REMEMBER_LAST_PAGE_SETTING_NAME))
                    {
                        return (bool)RoamingSettings.Values[REMEMBER_LAST_PAGE_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return REMEMBER_LAST_PAGE_DEFAULT;
            }
            set
            {
                RoamingSettings.Values[REMEMBER_LAST_PAGE_SETTING_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool MaintainZoom
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(MAINTAIN_ZOOM_SETTING_NAME))
                    {
                        return (bool)LocalSettings.Values[MAINTAIN_ZOOM_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return MAINTAIN_ZOOM_DEFAULT;
            }
            set
            {
                LocalSettings.Values[MAINTAIN_ZOOM_SETTING_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool EnableJavaScript
        {
            get
            {
                try
                {
                    if (RoamingSettings.Values.ContainsKey(ENABLEJAVASCRIPT_SETTING_NAME))
                    {
                        return (bool)_RoamingSettings.Values[ENABLEJAVASCRIPT_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return ENABLEJAVASCRIPT_DEFAULT;
            }
            set
            {
                RoamingSettings.Values[ENABLEJAVASCRIPT_SETTING_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool ButtonsStayDown
        {
            get
            {
                try
                {
                    if (RoamingSettings.Values.ContainsKey(BUTTON_STAY_DOWN_SETTING_NAME))
                    {
                        return (bool)RoamingSettings.Values[BUTTON_STAY_DOWN_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return BUTTONS_STAY_DOWN_DEFAULT;
            }
            set
            {
                RoamingSettings.Values[BUTTON_STAY_DOWN_SETTING_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool AutoSaveOn
        {
            get
            {
                try
                {
                    if (RoamingSettings.Values.ContainsKey(AUTO_SAVE_ON_SETTING_NAME))
                    {
                        return (bool)RoamingSettings.Values[AUTO_SAVE_ON_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return AUTO_SAVE_ON_DEFAULT;
            }
            set
            {
                RoamingSettings.Values[AUTO_SAVE_ON_SETTING_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static string EmailSignature
        {
            get
            {
                try
                {
                    if (RoamingSettings.Values.ContainsKey(EMAIL_SIGNATURE_SETTING_NAME))
                    {
                        return RoamingSettings.Values[EMAIL_SIGNATURE_SETTING_NAME] as string;
                    }
                }
                catch (Exception) { }
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                return string.Format(loader.GetString("Settings_Options_EmailSignature_Default"), Settings.DisplayName);
            }
            set
            {
                RoamingSettings.Values[EMAIL_SIGNATURE_SETTING_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static ViewModels.Viewer.Helpers.ViewerPageSettingsViewModel.CustomColorModes ColorMode
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(COLOR_MODE_SETTING_NAME))
                    {
                        return (ViewModels.Viewer.Helpers.ViewerPageSettingsViewModel.CustomColorModes)LocalSettings.Values[COLOR_MODE_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return COLOR_MODE_DEFAULT;
            }
            set
            {
                LocalSettings.Values[COLOR_MODE_SETTING_NAME] = (int)value;
                RaiseSettingChangedDelegate();
            }
        }

        //public static AppVersion LastVersion
        //{
        //    get
        //    {
        //        return AppVersion.LoadAppVersion(LocalSettings, APP_VERSION_SETTING_NAME);
        //    }
        //    set
        //    {
        //        value.SaveAppVersion(LocalSettings, APP_VERSION_SETTING_NAME);
        //    }
        //}

        public static ApplicationTheme ThemeOption
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(THEME_SETTING_NAME))
                    {
                        return (ApplicationTheme)LocalSettings.Values[THEME_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return THEME_SETTING_DEFAULT;
            }
            set
            {
                LocalSettings.Values[THEME_SETTING_NAME] = (int)value;
                RaiseSettingChangedDelegate();
            }
        }

        public static pdftron.PDF.Tools.ToolManager.InkSmoothingOptions InkSmoothingOption
        {
            get 
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(INK_SMOOTHING_SETTING_NAME))
                    {
                        return (pdftron.PDF.Tools.ToolManager.InkSmoothingOptions)LocalSettings.Values[INK_SMOOTHING_SETTING_NAME];
                    }
                }
                catch (Exception) {}
                return INK_SMOOTHING_DEFAULT;
            }
            set
            {
                LocalSettings.Values[INK_SMOOTHING_SETTING_NAME] = (int)value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool StylusAsPen
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(STYLUS_AS_PEN_NAME))
                    {
                        return Convert.ToBoolean((LocalSettings.Values[STYLUS_AS_PEN_NAME]));
                    }
                }
                catch (Exception) { }
                return STYLUS_AS_PEN_DEFAULT;
            }
            set
            {
                LocalSettings.Values[STYLUS_AS_PEN_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool CopyAnnotatedTextToNote
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(COPY_ANNOTATED_TEXT_TO_NOTE_NAME))
                    {
                        return Convert.ToBoolean((LocalSettings.Values[COPY_ANNOTATED_TEXT_TO_NOTE_NAME]));
                    }
                }
                catch (Exception) { }
                return COPY_ANNOTATED_TEXT_TO_NOTE_DEFAULT;
            }
            set
            {
                LocalSettings.Values[COPY_ANNOTATED_TEXT_TO_NOTE_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool ScreenSleepLock
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(SCREEN_SLEEP_LOCK))
                    {
                        return Convert.ToBoolean((LocalSettings.Values[SCREEN_SLEEP_LOCK]));
                    }
                }
                catch (Exception) { }
                return SCREEN_SLEEP_LOCK_DEFAULT;
            }
            set
            {
                LocalSettings.Values[SCREEN_SLEEP_LOCK] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static string FolderPathList
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(FOLDER_PATH_LIST_NAME))
                    {
                        return Convert.ToString(LocalSettings.Values[FOLDER_PATH_LIST_NAME]);
                    }
                }
                catch (Exception) { }
                return "";
            }
            set
            {
                LocalSettings.Values[FOLDER_PATH_LIST_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static string MainPagePanel
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(MAINPAGE_PANEL_NAME))
                    {
                        return Convert.ToString(LocalSettings.Values[MAINPAGE_PANEL_NAME]);
                    }
                }
                catch (Exception) { }
                return MAINPAGE_PANEL_DEFAULT;
            }
            set
            {
                LocalSettings.Values[MAINPAGE_PANEL_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }
        public static int FolderIconView
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(FOLDER_ICON_VIEW_NAME))
                    {
                        return (int)LocalSettings.Values[FOLDER_ICON_VIEW_NAME];
                    }
                }
                catch (Exception) { }
                return (int)FOLDER_ICON_VIEW_DEFAULT;
            }
            set
            {
                LocalSettings.Values[FOLDER_ICON_VIEW_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static int RecentIconView
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(RECENT_ICON_VIEW_NAME))
                    {
                        return (int)LocalSettings.Values[RECENT_ICON_VIEW_NAME];
                    }
                }
                catch (Exception) { }
                return (int)RECENT_ICON_VIEW_DEFAULT;
            }
            set
            {
                LocalSettings.Values[RECENT_ICON_VIEW_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static int OpenedIconView
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(OPENED_ICON_VIEW_NAME))
                        return (int)LocalSettings.Values[OPENED_ICON_VIEW_NAME];
                }
                catch
                {
                    //Swallow exception
                }

                return (int)OPENED_ICON_VIEW_DEFAULT;
            }
            set
            {
                LocalSettings.Values[OPENED_ICON_VIEW_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static bool PinCommandBar
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(PIN_COMMAND_BAR_SETTING_NAME))
                    {
                        return (bool)LocalSettings.Values[PIN_COMMAND_BAR_SETTING_NAME];
                    }
                }
                catch (Exception) { }
                return (bool)PIN_COMMAND_BAR_DEFAULT;
            }
            set
            {
                LocalSettings.Values[PIN_COMMAND_BAR_SETTING_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static IList<Tuple<Color, Color>> GetCustomColors()
        {
            if (CUSTOM_COLOR_OPTIONS == null)
            {
                CUSTOM_COLOR_OPTIONS = ParseColorString(CUSTOM_COLOR_OPTIONS_STRING);
            }
            return CUSTOM_COLOR_OPTIONS;
        }

        public static void SetDefaultCustomColors()
        {
            char[] separateColors = { ':' };
            string[] colorStrings = CUSTOM_COLOR_OPTIONS_STRING.Split(separateColors);
            for (int i = 0; i < NUM_CUSTOM_COLORS; ++i)
            {
                LocalSettings.Values[CUSTOM_COLOR_LIST_NAME + i] = colorStrings[i];
            }

            CUSTOM_COLOR_OPTIONS = ParseColorString(CUSTOM_COLOR_OPTIONS_STRING);
        }

        public static IList<Tuple<Color, Color>> ParseColorString(string colorString)
        {
            try
            {
                List<Tuple<Color, Color>> colors = new List<Tuple<Color, Color>>();
                char[] separateColors = { ':' };
                char[] separateBytes = { ',' };
                string[] colorStrings = colorString.Split(separateColors);
                int index = 0;
                foreach (string color in colorStrings)
                {
                    string[] byteStrings;
                    if (LocalSettings.Values.ContainsKey(CUSTOM_COLOR_LIST_NAME + index))
                    {
                        string str = (string)LocalSettings.Values[CUSTOM_COLOR_LIST_NAME + index];
                        byteStrings = str.Split(separateBytes);
                    }
                    else
                    {
                        byteStrings = color.Split(separateBytes);
                    }
                    Color background = Color.FromArgb(255, Byte.Parse(byteStrings[0]), Byte.Parse(byteStrings[1]), Byte.Parse(byteStrings[2]));
                    Color foreground = Color.FromArgb(255, Byte.Parse(byteStrings[3]), Byte.Parse(byteStrings[4]), Byte.Parse(byteStrings[5]));
                    Tuple<Color, Color> pair = new Tuple<Color, Color>(background, foreground);
                    colors.Add(pair);

                    index++;
                }
                return colors;
            }
            catch (Exception)
            {
                // log
            }
            return null;
        }

        public static int CurrentCustomColorIcon
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(CUSTOM_COLOR_LIST_INDEX_NAME))
                    {
                        int index = (int)LocalSettings.Values[CUSTOM_COLOR_LIST_INDEX_NAME];
                        if (index < 0 || index >= CUSTOM_COLOR_OPTIONS.Count)
                        {
                            index = 0;
                        }
                        return index;
                    }
                }
                catch (Exception) { }
                return CUSTOM_COLOR_ICON_DEFAULT;
            }
            set
            {
                LocalSettings.Values[CUSTOM_COLOR_LIST_INDEX_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static void SetCustomColorIcon(Color background, Color foreground, int index)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(background.R + "," + background.G + "," + background.B + "," + foreground.R + "," + foreground.G + "," + foreground.B);
            LocalSettings.Values[CUSTOM_COLOR_LIST_NAME + index] = sb.ToString();
            if (CUSTOM_COLOR_OPTIONS != null)
            {
                CUSTOM_COLOR_OPTIONS[index] = new Tuple<Color, Color>(background, foreground);
            }
        }

        public static int LogNativeCode
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(LOG_NATIVE_CODE_NAME))
                    {
                        return (int)LocalSettings.Values[LOG_NATIVE_CODE_NAME];
                    }
                }
                catch (Exception) { }
                return LOG_NATIVE_CODE_DEFAULT;
            }
            set
            {
                LocalSettings.Values[LOG_NATIVE_CODE_NAME] = value;
            }
        }

        public static bool OutlineDialogOpen
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(OUTLINE_DIALOG_OPEN_NAME))
                    {
                        return (bool)LocalSettings.Values[OUTLINE_DIALOG_OPEN_NAME];
                    }
                }
                catch (Exception) { }
                return OUTLINE_DIALOG_OPEN_DEFAULT;
            }
            set
            {
                LocalSettings.Values[OUTLINE_DIALOG_OPEN_NAME] = value;
            }
        }

        public static Viewer.Dialogs.OutlineDialog.AnchorSides OutlineDialogAnchorSide
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(OUTLINE_DIALOG_ANCHOR_SIDE_NAME))
                    {
                        return (Viewer.Dialogs.OutlineDialog.AnchorSides)LocalSettings.Values[OUTLINE_DIALOG_ANCHOR_SIDE_NAME];
                    }
                }
                catch (Exception) { }
                return OUTLINE_DIALOG_ANCHOR_SIDE_DEFAULT;
            }
            set
            {
                LocalSettings.Values[OUTLINE_DIALOG_ANCHOR_SIDE_NAME] = (int)value;
                RaiseSettingChangedDelegate();
            }
        }
        public static int OutlineDialogNumThumbnailColumns
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(OUTLINE_DIALOG_THUMBNAIL_NUM_COLUMNS_NAME))
                    {
                        return (int)LocalSettings.Values[OUTLINE_DIALOG_THUMBNAIL_NUM_COLUMNS_NAME];
                    }
                }
                catch (Exception) { }
                return OUTLINE_DIALOG_THUMBNAIL_NUM_COLUMNS_DEFAULT;
            }
            set
            {
                LocalSettings.Values[OUTLINE_DIALOG_THUMBNAIL_NUM_COLUMNS_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static int PhoneFullScreen
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(PHONE_FULL_SCREEN_NAME))
                    {
                        return (int)LocalSettings.Values[PHONE_FULL_SCREEN_NAME];
                    }
                }
                catch (Exception) { }
                return PHONE_FULL_SCREEN_DEFAULT;
            }
            set
            {
                LocalSettings.Values[PHONE_FULL_SCREEN_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static int OutlineDialogWidth
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(OUTLINE_DIALOG_WIDTH_NAME))
                    {
                        return (int)LocalSettings.Values[OUTLINE_DIALOG_WIDTH_NAME];
                    }
                }
                catch (Exception) { }
                return OUTLINE_DIALOG_WIDTH_DEFAULT;
            }
            set
            {
                LocalSettings.Values[OUTLINE_DIALOG_WIDTH_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public static int OutlineDefautlView
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(OUTLINE_DEFAULT_VIEW_NAME))
                    {
                        return (int)LocalSettings.Values[OUTLINE_DEFAULT_VIEW_NAME];
                    }
                }
                catch (Exception) { }
                return OUTLINE_DEFAULT_VIEW_DEFAULT;
            }
            set
            {
                LocalSettings.Values[OUTLINE_DEFAULT_VIEW_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        public enum FilterKeys
        {
            AllFiles = 0x01,
            PDFs = 0x02,
            OfficeFiles = 0x04,
            Images = 0x08,
        }

        public static int FileTypeFilter
        {
            get
            {
                try
                {
                    if (LocalSettings.Values.ContainsKey(FILE_TYPE_FILTER_NAME))
                    {
                        return (int)LocalSettings.Values[FILE_TYPE_FILTER_NAME];
                    }
                }
                catch (Exception) { }
                return FILE_TYPE_FILTER_DEFAULT;
            }
            set
            {
                LocalSettings.Values[FILE_TYPE_FILTER_NAME] = value;
                RaiseSettingChangedDelegate();
            }
        }

        #endregion Settings
    }
}
