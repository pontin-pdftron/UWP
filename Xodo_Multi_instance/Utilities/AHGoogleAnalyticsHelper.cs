using GoogleAnalytics.Core;
using pdftron.PDF.Tools.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompleteReader.Utilities
{
    class AHGoogleAnalyticsHelper : AnalyticsHandlerBase
    {
        private Tracker _Tracker;
        
        public AHGoogleAnalyticsHelper()
            : base()
        {
            _Tracker = GoogleAnalytics.EasyTracker.GetTracker();
            GrabUnhandledException();
        }

        public AHGoogleAnalyticsHelper(string trackerID)
            : base()
        {
            _Tracker = GoogleAnalytics.AnalyticsEngine.Current.GetTracker(trackerID);
            GrabUnhandledException();
        }

        private void GrabUnhandledException()
        {
            // This line here interferes with application insights for crash stack traces
            //Windows.UI.Xaml.Application.Current.UnhandledException += Current_UnhandledException;
        }

        void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            _Tracker.SendException(e.Exception.ToString(), true);
        }

        public override void SendEvent(string category, string action)
        {
            _Tracker.SendEvent(category, action, null, 0);
        }

        public override void SendEvent(string category, string action, string label)
        {
            _Tracker.SendEvent(category, action, label, 0);
        }

        public override void SendException(Exception ex)
        {
            _Tracker.SendException(ex.ToString(), false);
        }

        public override void SendException(Exception ex, string key, string value)
        {
            SendException(ex);
        }

        public override void SendException(Exception ex, IDictionary<string, string> map)
        {
            SendException(ex);
        }
    }
}
