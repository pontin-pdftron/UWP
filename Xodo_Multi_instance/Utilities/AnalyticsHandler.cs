using System;

using Windows.Storage;
using System.Diagnostics;
using pdftron.PDF.Tools.Utilities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CompleteReader.Utilities
{
    /// <summary>
    /// A utility class for app analytics.
    /// </summary>
    public class AnalyticsHandler : AnalyticsHandlerBase
    {
        public const string EXCEPTION_SEVERE_EVENT_CATEGORY = "APP_Severe_Exception";
        public const string EXCEPTION_APP = "APP_Eception";


        private static AnalyticsHandler _CURRENT;
        public static new AnalyticsHandler CURRENT
        {
            get
            {
                if (_CURRENT == null)
                {
                    _CURRENT = new AnalyticsHandler();
                }
                return _CURRENT;
            }
            set
            {
                _CURRENT = value;
            }
        }

        private IList<AnalyticsHandlerBase> _Handlers = new List<AnalyticsHandlerBase>();
        public void AddHandler(AnalyticsHandlerBase handler)
        {
            if (!_Handlers.Contains(handler))
            {
                _Handlers.Add(handler);
            }
        }

        public bool RemoveHandler(AnalyticsHandlerBase handler)
        {
            return _Handlers.Remove(handler);
        }

        public enum Category
        {
            VIEWER,
            FILEBROWSER,
            GENERAL,
            ANNOTATIONTOOLBAR,
            BOOKMARK,
            THUMBSLIDER,
            THUMBVIEW,
            QUICKTOOL,
            HARDWARE,
        }

        public enum Label
        {
            SHARE,
            MANAGE,
        }


        /// <summary>
        /// Add global custom data
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void AddCrashExtraData(string key, string value)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.AddCrashExtraData(key, value);
            }
        }

        /// <summary>
        /// Clear global custom data
        /// </summary>
        public override void ClearCrashExtraData()
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.ClearCrashExtraData();
            }
        }

        /// <summary>
        /// Remove a custom extra data instance with key
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public override bool RemoveCrashExtraData(string keyName)
        {
            bool removed = false;
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                removed |= handler.RemoveCrashExtraData(keyName);
            }
            return removed;
        }

        /// <summary>
        /// Log handled exceptions and send custom data Asynchronous
        /// </summary>
        /// <param name="ex"></param>
        public override void LogException(Exception ex)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.LogException(ex);
            }
        }

        /// <summary>
        /// Log handled exceptions and send custom data Asynchronous
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void LogException(Exception ex, string key, string value)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.LogException(ex, key, value);
            }
        }

        /// <summary>
        /// Send an immediate exception with custom data Asynchronous
        /// </summary>
        /// <param name="ex">Exception</param>
        public override void SendException(Exception ex)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.SendException(ex);
            }
        }

        /// <summary>
        /// Send an immediate exception with custom data Asynchronous
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="key">custom data key</param>
        /// <param name="value">custom data value</param>
        public override void SendException(Exception ex, string key, string value)
        {
            IDictionary<String, String> map = new Dictionary<string, string>();
            map.Add(key, value);
            SendException(ex, map);
        }

        public override void SendException(Exception ex, IDictionary<String, String> map)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.SendException(ex, map);
            }
        }

        /// <summary>
        /// Add breadcrumb to the global breadcrumb list
        /// </summary>
        /// <param name="evt">breadcrumb</param>
        public override void LeaveBreadcrumb(string evt)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.LeaveBreadcrumb(evt);
            }
        }

        /// <summary>
        /// Clear global breadcrumb list
        /// </summary>
        public override void ClearBreadCrumbs()
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.ClearBreadCrumbs();
            }
        }

        /// <summary>
        /// Set an action to be executed before the app crashes (save state)
        /// </summary>
        /// <param name="lastAction">An action that is to be executed just before an app crashes</param>
        public override void LastActionBeforeTerminate(System.Action lastAction)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.LastActionBeforeTerminate(lastAction);
            }
        }

        /*/// <summary>
        /// Log an event using the following method Synchronous
        /// </summary>
        /// <param name="tag"></param>
        public void LogEvent(string tag)
        {
            if (IsBugSenseActive)
            {
                BugSenseLogResult result = BugSenseHandler.Instance.LogEvent(tag);

                // Examine the ResultState to determine whether it was successful.
                Debug.WriteLine("Result: {0}", result.ResultState.ToString());
            }
        }*/

        /// <summary>
        /// Log an event using the following method Asynchronous
        /// </summary>
        /// <param name="tag"></param>
        public override void LogEvent(string tag)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.LogEvent(tag);
            }
        }

        /// <summary>
        /// Try to send an event immediately Asynchronous
        /// </summary>
        /// <param name="tag"></param>
        public override void SendEvent(string tag)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.SendEvent(tag);
            }
        }

        public void SendEvent(Category category, string action)
        {
            SendEvent(GetCategoryString(category), action);
        }

        public override void SendEvent(string category, string action)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.SendEvent(category, action);
            }
        }

        public void SendEvent(Category category, string action, Label label)
        {
            SendEvent(GetCategoryString(category), action, GetLabelString(label));
        }

        public override void SendEvent(string category, string action, string label)
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.SendEvent(category, action, label);
            }
        } 

        public override void Flush()
        {
            foreach (AnalyticsHandlerBase handler in _Handlers)
            {
                handler.Flush();
            }
        }

        public async Task<ulong> GetFileSizeAsync(IStorageFile file)
        {
            var properties = await file.GetBasicPropertiesAsync();
            if (properties != null)
            {
                return properties.Size;
            }
            return 0;
        }


        #region Utility Functions

        public static String GetCategoryString(Category category)
        {
            switch (category)
            {
                case Category.VIEWER:
                    return "Viewer";
                case Category.FILEBROWSER:
                    return "File Browser";
                case Category.GENERAL:
                    return "General";
                case Category.ANNOTATIONTOOLBAR:
                    return "Annotation Toolbar";
                case Category.BOOKMARK:
                    return "Bookmark";
                case Category.QUICKTOOL:
                    return "QuickMenu Tool";
                case Category.THUMBSLIDER:
                    return "ThumbSlider";
                case Category.THUMBVIEW:
                    return "ThumbnailsView";
                case Category.HARDWARE:
                    return "Hardware";
                default:
                    return "General";
            }
        }

        public static string GetLabelString(Label label)
        {
            switch (label)
            {
                case Label.MANAGE:
                    return "Manage";
                case Label.SHARE:
                    return "Share";
                default:
                    return "General";
            }
        }

        #endregion Utility Functions
    }
}
