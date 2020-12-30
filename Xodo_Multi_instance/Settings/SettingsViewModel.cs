using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.Document;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using CompleteReaderSettings = CompleteReader.Settings.Settings;

namespace CompleteReader.Settings
{
    public class SettingsViewModel : ViewModelBase, INavigable
    {
        private static SettingsViewModel _Current;
        public SettingsViewModel()
        {
            PrepareInkSmoothingOptions();
            PrepareBackgroundOptions();
            ShowAboutCommand = new RelayCommand(ShowAboutCommandImpl);
            NavigationCommand = new RelayCommand(NavigationCommandImpl);
            _Current = this;
        }
        public static SettingsViewModel Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new SettingsViewModel();
                }
                return _Current;
            }
        }

        // Need this to satisfy INavigable, but don't need to use it.
        public event NewINavigableAvailableDelegate NewINavigableAvailable;

#region Settings
        public enum SettingsViews
        {
            Options,
            About,
            Fonts,
            TextAnnotationFonts,
        }

        private SettingsViews _CurrentView = SettingsViews.Options;
        public SettingsViews CurrentView
        {
            get { return _CurrentView; }
            set { Set(ref _CurrentView, value); }
        }

        public bool RememberLastPageSetting
        {
            get { return CompleteReaderSettings.RememberLastPage; }
            set { CompleteReaderSettings.RememberLastPage = value; }
        }

        public bool MaintainZoomSetting
        {
            get { return CompleteReaderSettings.MaintainZoom; }
            set { CompleteReaderSettings.MaintainZoom = value; }
        }

        public bool EnableJavaScriptSetting
        {
            get { return CompleteReaderSettings.EnableJavaScript; }
            set { CompleteReaderSettings.EnableJavaScript = value; }
        }

        public bool EnableLoggingSettings
        {
            get { return CompleteReaderSettings.LogNativeCode > 0; }
            set
            {
                int val = 0;
                if (value)
                {
                    val = 1;
                }
                CompleteReaderSettings.LogNativeCode = val;
                Utilities.UtilityFunctions.SetNativeLoggingState(CompleteReaderSettings.LogNativeCode);
            }
        }

        public bool ContinuousAnnotationEditOption
        {
            get { return CompleteReaderSettings.ButtonsStayDown; }
            set 
            { 
                CompleteReaderSettings.ButtonsStayDown = value;
                
            }
        }
        public bool StylusAsPenOption
        {
            get { return CompleteReaderSettings.StylusAsPen; }
            set
            {
                CompleteReaderSettings.StylusAsPen = value;
            }
        }

        public bool CopyAnnotatedTextToNote
        {
            get { return CompleteReaderSettings.CopyAnnotatedTextToNote; }
            set { CompleteReaderSettings.CopyAnnotatedTextToNote = value; }
        }

        public bool ScreenSleepLockOption
        {
            get { return CompleteReaderSettings.ScreenSleepLock; }
            set
            {
                CompleteReaderSettings.ScreenSleepLock = value;
            }
        }

        public bool AutoSaveSetting
        {
            get { return CompleteReaderSettings.AutoSaveOn; }
            set { CompleteReaderSettings.AutoSaveOn = value; }
        }

        public string AuthorSetting
        {
            get { return pdftron.PDF.Tools.Settings.AnnotationAuthor; }
            set { pdftron.PDF.Tools.Settings.AnnotationAuthor = value; }
        }

        public string EmailSignatureSetting
        {
            get { return CompleteReaderSettings.EmailSignature; }
            set { CompleteReaderSettings.EmailSignature = value; }
        }

        private bool _IsAboutDialogVisible = false;
        public bool IsAboutDialogVisible
        {
            get { return _IsAboutDialogVisible; }
            set { Set(ref _IsAboutDialogVisible, value); }
        }

        public RelayCommand ShowAboutCommand { get; private set; }
        public RelayCommand NavigationCommand { get; private set; }
        public RelayCommand BackgroundChangedCommand { get; private set; }

        private void ShowAboutCommandImpl(object param)
        {
            IsAboutDialogVisible = true;
        }

        private void NavigationCommandImpl(object param) {
            string nextView = param as string;
            if (!string.IsNullOrWhiteSpace(nextView))
            {
                if (nextView.Equals("Options", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentView = SettingsViews.Options;
                }
                else if (nextView.Equals("About", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentView = SettingsViews.About;
                }
                else if (nextView.Equals("Fonts", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentView = SettingsViews.Fonts;
                }
                else if (nextView.Equals("TextAnnotationFonts", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentView = SettingsViews.TextAnnotationFonts;
                }
                else
                {
                    CurrentView = SettingsViews.Options;
                    DocumentViewModel.Current.SubViewSelectionCommand.Execute(Settings.MainPagePanel);
                }
            }
            else
            {
                // Back button
                SettingsViews oldView = CurrentView;

                CurrentView = SettingsViews.Options;
                if (oldView != SettingsViews.Fonts && oldView != SettingsViews.TextAnnotationFonts)
                {
                    DocumentViewModel.Current.SubViewSelectionCommand.Execute(Settings.MainPagePanel);
                }
            }
        }

#region Background Setting
        private ObservableCollection<BackgroundOption> _BackgroundSettings;

        public ObservableCollection<BackgroundOption> BackgroundSettings { get { return _BackgroundSettings; } }

        private BackgroundOption _SelectedBackgroundSettings;
        public BackgroundOption SelectedBackgroundSettings
        {
            get { return _SelectedBackgroundSettings; }
            set
            {
                if (_SelectedBackgroundSettings == null)
                   Set(ref _SelectedBackgroundSettings, value);

                if (_SelectedBackgroundSettings != value)
                {
                    string strContent = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("Settings_Options_ThemeMessage_Content");
                    string strTitle = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("Settings_Options_ThemeMessage_Title");

                    var message = new MessageDialog(strContent, strTitle);

                    string strLabel = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("ViewerPage_ViewModeControl_FullScreen_Okay");

                    message.Commands.Add(new Windows.UI.Popups.UICommand(strLabel, (command) =>
                    {
                        Set(ref _SelectedBackgroundSettings, value);
                        Settings.ThemeOption = _SelectedBackgroundSettings.ThemeOption;

                        Application.Current.Exit();
                    }));

                    // TODO: Add "Cancel" to resources
                    message.Commands.Add(new Windows.UI.Popups.UICommand("Cancel", (command) =>
                    {
                        RaisePropertyChanged(); // Notify view to set the selection back to original
                        MessageDialogHelper.CancelLastMessageDialog();
                    }));

                    _ = Utilities.MessageDialogHelper.ShowMessageDialogAsync(message);
                }
            }
        }
        private void PrepareBackgroundOptions()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            _BackgroundSettings = new ObservableCollection<BackgroundOption>();
            _BackgroundSettings.Add(new BackgroundOption(ApplicationTheme.Dark));
            _BackgroundSettings.Add(new BackgroundOption(ApplicationTheme.Light));
            foreach (BackgroundOption option in _BackgroundSettings)
            {
                if (option.ThemeOption == Settings.ThemeOption)
                {
                    SelectedBackgroundSettings = option;
                }
            }
        }

        public static string BackgroundOptionToString(ApplicationTheme option)
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            switch (option)
            {
                case ApplicationTheme.Dark:
                    return loader.GetString("Settings_Options_Background_Dark");
                case ApplicationTheme.Light:
                    return loader.GetString("Settings_Options_Background_Light");
            }
            return loader.GetString("Settings_Options_Background_Dark");
        }

        public class BackgroundOption
        {
            public string OptionName { get; private set; }

            public ApplicationTheme ThemeOption { get; private set; }

            public BackgroundOption(ApplicationTheme themeOption)
            {
                ThemeOption = themeOption;
                OptionName = BackgroundOptionToString(themeOption);
            }
        }

#endregion

#region Ink Smoothing

        private void PrepareInkSmoothingOptions()
        {
            _InkSmoothingItems = new ObservableCollection<InkSmoothingOption>();
            _InkSmoothingItems.Add(new InkSmoothingOption(pdftron.PDF.Tools.ToolManager.InkSmoothingOptions.SmoothAll));
            _InkSmoothingItems.Add(new InkSmoothingOption(pdftron.PDF.Tools.ToolManager.InkSmoothingOptions.AllButStylus));
            _InkSmoothingItems.Add(new InkSmoothingOption(pdftron.PDF.Tools.ToolManager.InkSmoothingOptions.NoSmoothing));
            foreach (InkSmoothingOption option in _InkSmoothingItems)
            {
                if (option.SmoothingOption == Settings.InkSmoothingOption)
                {
                    _SelectedInkSmoothingOption = option;
                }
            }
        }

        public static string InkSmoothingOptionToString(pdftron.PDF.Tools.ToolManager.InkSmoothingOptions option)
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            switch(option)
            {
                case pdftron.PDF.Tools.ToolManager.InkSmoothingOptions.NoSmoothing:
                    return loader.GetString("Settings_Options_InkSmoothing_NoSmoothing");
                case pdftron.PDF.Tools.ToolManager.InkSmoothingOptions.SmoothAll:
                    return loader.GetString("Settings_Options_InkSmoothing_SmoothAll");
                case pdftron.PDF.Tools.ToolManager.InkSmoothingOptions.AllButStylus:
                    return loader.GetString("Settings_Options_InkSmoothing_AllButStylus");
            }
            return loader.GetString("Settings_Options_InkSmoothing_AllButStylus");
        }

        public class InkSmoothingOption
        {
            public string OptionName { get; private set; }

            public pdftron.PDF.Tools.ToolManager.InkSmoothingOptions SmoothingOption { get; private set; }

            public InkSmoothingOption(pdftron.PDF.Tools.ToolManager.InkSmoothingOptions smoothingOption)
            {
                SmoothingOption = smoothingOption;
                OptionName = InkSmoothingOptionToString(smoothingOption);
            }
        }

        private ObservableCollection<InkSmoothingOption> _InkSmoothingItems;
        public ObservableCollection<InkSmoothingOption> InkSmoothingItems { get { return _InkSmoothingItems; } }

        private InkSmoothingOption _SelectedInkSmoothingOption;
        public InkSmoothingOption SelectedInkSmoothingOption
        {
            get { return _SelectedInkSmoothingOption; }
            set 
            { 
                if (Set(ref _SelectedInkSmoothingOption, value))
                {
                    Settings.InkSmoothingOption = _SelectedInkSmoothingOption.SmoothingOption;
                }
            }
        }

        #endregion Ink Smoothing

#endregion Settings

        public override bool GoBack()
        {
            if (IsAboutDialogVisible)
            {
                IsAboutDialogVisible = false;
                return true;
            }
            return false;
        }

        private void BackButtonHandler_BackPressed(object sender, BackRequestedEventArgs e)
        {
            CurrentView = SettingsViews.Options;
            DocumentViewModel.Current.SubViewSelectionCommand.Execute(Settings.MainPagePanel);
            e.Handled = true;
        }

        public void Activate(object parameter)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += BackButtonHandler_BackPressed;
        }

        public void Deactivate(object parameter)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= BackButtonHandler_BackPressed;
        }

    }
}
