using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace CompleteReader.Settings
{
    /// <summary>
    /// Class where all the logic for when and what to display in a What's new type dialog should be shown.
    /// </summary>
    //public class WhatsNewManager
    //{
    //    private static WhatsNewManager _Current;
    //    public static WhatsNewManager Current
    //    {
    //        get 
    //        {
    //            if (_Current == null)
    //            {
    //                _Current = new WhatsNewManager();
    //            }
    //            return _Current;
    //        }
    //    }

    //    private WhatsNewManager() 
    //    {
    //        // TODO Enable before release
    //        ShownWhatsNewFinished();
    //    }

    //    public event Windows.UI.Xaml.RoutedEventHandler ShowWhatsNew;

    //    public bool ShouldShowWhatsNew
    //    {
    //        get 
    //        {
    //            Settings.AppVersion a1 = Settings.AppVersion.CurrentVersion;
    //            Settings.AppVersion a2 = Settings.LastVersion;
    //            bool shouldShow = a1 > a2; 
    //            return shouldShow;
    //        }
    //    }

    //    public bool ShouldShowChanged
    //    {
    //        get 
    //        {
    //            // Make this return true if there is a feature that has changed, and you need to tell existing users, but not new ones.
    //            return false;
    //        }
    //    }

    //    public bool ShouldShowWelcome
    //    {
    //        get 
    //        { 
    //            // Until the first version with this has been released, we will have to return false here
    //            // else it will show up for everyone.
    //            //return Settings.LastVersion == Settings.AppVersion.DefaultAppVersion;

    //            return false;
    //        }
    //    }

    //    public void ShownWhatsNewFinished()
    //    {
    //        Settings.LastVersion = Settings.AppVersion.CurrentVersion;
    //        _HasShownWhatsNew = true;
    //    }


    //    #region Impl

    //    private bool _HasShownWhatsNew = false;
    //    private bool _IsReadyToShowWhatsNew = false;
    //    public bool IsReadyToShowWhatsNew
    //    {
    //        get { return _IsReadyToShowWhatsNew; }
    //        set
    //        {
    //            if (value != _IsReadyToShowWhatsNew)
    //            {
    //                _IsReadyToShowWhatsNew = value;
    //                if (!_HasShownWhatsNew)
    //                {
    //                    if (_IsReadyToShowWhatsNew)
    //                    {
    //                        if (ShouldShowWelcome || ShouldShowChanged || ShouldShowWhatsNew)
    //                        {
    //                            ShowWhatsNewTimer.Start();
    //                        }
    //                        else
    //                        {
    //                            _HasShownWhatsNew = true;
    //                        }
    //                    }
    //                    else
    //                    {
    //                        ShowWhatsNewTimer.Stop();
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    private DispatcherTimer _ShowWhatsNewTimer;
    //    private DispatcherTimer ShowWhatsNewTimer
    //    {
    //        get
    //        {
    //            if (_ShowWhatsNewTimer == null)
    //            {
    //                _ShowWhatsNewTimer = new DispatcherTimer();
    //                _ShowWhatsNewTimer.Interval = TimeSpan.FromSeconds(2);
    //                _ShowWhatsNewTimer.Tick += ShowWhatsNewTimer_Tick;
    //            }
    //            return _ShowWhatsNewTimer;
    //        }
    //    }

    //    void ShowWhatsNewTimer_Tick(object sender, object e)
    //    {
    //        _ShowWhatsNewTimer.Stop();

    //        if (ShowWhatsNew != null)
    //        {
    //            ShowWhatsNew(this, new RoutedEventArgs());
    //        }
    //    }

    //    #endregion Impl
    //}
}
