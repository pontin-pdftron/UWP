using CompleteReader.Settings;
using CompleteReader.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace CompleteReader.Settings
{
    class UserVoiceFeedbackFormViewModel : ViewModelBase
    {
        private static UserVoiceFeedbackFormViewModel _Current;
        public static UserVoiceFeedbackFormViewModel Current
        {
            get
            {
                if (_Current == null)
                    Launch();

                return _Current;
            }
        }
        public static void Launch()
        {
            new UserVoiceFeedbackFormViewModel();
        }

        private WebView _Viewer;
        public WebView Viewer
        {
            set
            {
                _Viewer = value;
                if (_Viewer != null)
                {
                    _Viewer.NavigationCompleted += _Viewer_NavigationCompleted;
                    _Viewer.FrameNavigationCompleted += _Viewer_FrameNavigationCompleted;
                    _Viewer.Navigate(SupportSource);
                }
            }
        }

        private UserVoiceFeedbackFormViewModel()
        {
            BackButtonCommand = new RelayCommand(BackButtonCommandImpl);
            _Current = this;
            //LaunchPopup();
        }

        private bool _IsLoading = true;
        public bool IsLoading
        {
            get { return _IsLoading; }
            private set
            {
                if (_IsLoading != value)
                {
                    _IsLoading = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _HasInternet = true;
        public bool HasInternet
        {
            get { return _HasInternet; }
            private set
            {
                if (_HasInternet != value)
                {
                    _HasInternet = value;
                    RaisePropertyChanged();
                }
            }
        }

        bool _loadSuccessfull = false;
        async void _Viewer_FrameNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
#if XODO
            _loadSuccessfull = false;
            await Task.Delay(1500);
            if (!_loadSuccessfull)
            {
                if (!CompleteReader.Utilities.UtilityFunctions.HasInternet())
                {
                    IsLoading = false;
                    HasInternet = false;
                }
            }
#endif
        }

        void _Viewer_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            _loadSuccessfull = true;
            IsLoading = false;
            HasInternet = true;
        }

        private Uri _UserVouceSupportSource = new Uri("http://xodo.com/winrt/support.html", UriKind.Absolute);
        public Uri SupportSource
        {
            get
            {
                return _UserVouceSupportSource;
            }
        }

        public RelayCommand BackButtonCommand { get; private set; }

        private void BackButtonCommandImpl(object sender)
        {
            Close();
        }

#region Popup

        private Popup _Popup;
        private Windows.UI.Xaml.Controls.Grid _ConentGrid;
        WindowSizeChangedEventHandler _SizeChangedHandler = null;

        private void LaunchPopup()
        {
            _Popup = new Popup();
            _ConentGrid = new Windows.UI.Xaml.Controls.Grid();
            _ConentGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            _ConentGrid.VerticalAlignment = VerticalAlignment.Stretch;
            _ConentGrid.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);

            _Popup.Child = _ConentGrid;
            _ConentGrid.Children.Add(new UserVoiceFeedbackForm());

            _SizeChangedHandler = CurrentWindow_SizeChanged;
            Window.Current.SizeChanged += _SizeChangedHandler;
            _Popup.Width = Window.Current.Bounds.Width;
            _Popup.Height = Window.Current.Bounds.Height;
            _ConentGrid.Width = Window.Current.Bounds.Width;
            _ConentGrid.Height = Window.Current.Bounds.Height;

            _Popup.IsOpen = true;
        }

        void CurrentWindow_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            _Popup.Width = e.Size.Width;
            _Popup.Height = e.Size.Height;
            _ConentGrid.Width = e.Size.Width;
            _ConentGrid.Height = e.Size.Height;
        }

        public void Close()
        {
            _Current = null;
            _Popup.IsOpen = false;
            if (_SizeChangedHandler != null)
            {
                Window.Current.SizeChanged -= _SizeChangedHandler;
            }
        }


#endregion Popup
    }
}
