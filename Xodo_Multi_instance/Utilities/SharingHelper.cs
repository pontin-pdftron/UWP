using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Foundation;

using Windows.ApplicationModel.DataTransfer;
using CompleteReader.Collections;

namespace CompleteReader.Utilities
{
    class SharingHelper
    {
        private static SharingHelper _Instance;
        
        // We will share either one of these.
        string _StringToShare;
        StorageFile _FileToShare;
        IList<RecentItem> _RecentItems;

        // Get a StorageFile to share. This is used by the viewer to share the current document
        public delegate StorageFile RetrieveSharingStorageFileHandler(ref string errorMessage);
        public event RetrieveSharingStorageFileHandler RetrieveSharingStorageFile;

        // Used to get a string to share. For example when text is selected
        public delegate string RetrieveSharingStringHandler();
        public event RetrieveSharingStringHandler RetrieveSharingString;

        // Used to get a list of RecentItems. We have to do this because we can't perform async operations where this is used.
        // With the RecentItems, we can perform the async operation later.
        public delegate IList<RecentItem> RetrieveSharingRecentItemsHandler(ref string errorMessage);
        public event RetrieveSharingRecentItemsHandler RetrieveSharingRecentItems;

        public delegate Task SaveDocumentDelegate();
        public SaveDocumentDelegate _Saver = null;
        public SaveDocumentDelegate DocumentSaver
        {
            set { _Saver = value; }
        }

        public static SharingHelper GetSharingHelper()
        {
            if (_Instance == null)
            {
                _Instance = new SharingHelper();
            }
            return _Instance;
        }

        private SharingHelper()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareTextHandler);
        }

        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            AnalyticsHandler.CURRENT.SendEvent("[Viewer] Share clicked from Viewer");

            _FileToShare = null;
            _RecentItems = null;

            string errorMessage = "An error occured";

            // Ask for a string first. For example, if text is selected, we will share that.
            if (RetrieveSharingString != null)
            {
                _StringToShare = RetrieveSharingString();
            }
            
            // if no string was received, we ask for a StorageFile
            if (string.IsNullOrWhiteSpace(_StringToShare))
            {
                if (RetrieveSharingStorageFile != null)
                {
                    _FileToShare = RetrieveSharingStorageFile(ref errorMessage);
                }
                if (RetrieveSharingRecentItems != null)
                {
                    _RecentItems = RetrieveSharingRecentItems(ref errorMessage);
                }
            }

            DataRequest request = e.Request;
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            if (!string.IsNullOrWhiteSpace(_StringToShare))
            {
                request.Data.Properties.Title = Settings.Settings.DisplayName;
                request.Data.SetText(_StringToShare);
            }
            else if (_FileToShare != null || _RecentItems != null)
            {
                if (_FileToShare != null)
                {
                    request.Data.Properties.Title = string.Format(loader.GetString("Sharing_SignleDocument_Title"), Settings.Settings.DisplayName, _FileToShare.Name);
                }
                else if (_RecentItems != null)
                {
                    if (_RecentItems.Count == 1)
                    {
                        request.Data.Properties.Title = string.Format(loader.GetString("Sharing_SignleDocument_Title"), Settings.Settings.DisplayName, _RecentItems[0].DocumentName);
                    }
                    else
                    {
                        request.Data.Properties.Title = string.Format(loader.GetString("Sharing_MultipleDocuments_Title"), Settings.Settings.DisplayName);
                    }
                }
                request.Data.Properties.ApplicationName = Settings.Settings.DisplayName;
                string emailSignature = Settings.Settings.EmailSignature;
                if (emailSignature.Length > 0)
                {
                    request.Data.SetText(emailSignature);
                }
                request.Data.Properties.FileTypes.Add("*.pdf");
                request.Data.SetDataProvider(StandardDataFormats.StorageItems, new DataProviderHandler(this.OnDeferredFileRequestedHandler));
            }
            else
            {
                e.Request.FailWithDisplayText(errorMessage);
            }
        }

        private async void OnDeferredFileRequestedHandler(DataProviderRequest request)
        {
            DataProviderDeferral deferral = request.GetDeferral();

            if (_FileToShare == null && _RecentItems == null)
            {
                return;
            }

            // Make sure to always call Complete when finished with the deferral.
            try
            {
                if (_FileToShare != null)
                {
                    // pause auto saver
                    if (Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                    {
                        //ViewModels.ViewerViewModel.Current.PauseAutoSaving();
                    }
                    else
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () =>
                            {
                                //ViewModels.ViewerViewModel.Current.PauseAutoSaving();
                            });
                    }
                    IReadOnlyList<StorageFile> storageFileList = new List<StorageFile>() { _FileToShare };
                    if (_Saver != null)
                    {
                        await _Saver();
                    }

                    request.SetData(storageFileList);
                    EnableSavingAfterDelay();

                }
                else if (_RecentItems != null)
                {
                    RecentItemsData recentItemsData = RecentItemsData.Instance;
                    if (recentItemsData == null)
                    {
                        recentItemsData = await RecentItemsData.GetItemSourceAsync();
                    }
                    List<StorageFile> storageItems = new List<StorageFile>();
                    foreach (RecentItem recentItem in _RecentItems)
                    {
                        StorageFile file = recentItem.Properties.File as StorageFile;
                        if (file == null)
                        {
                            RecentDocumentProperties properties = await recentItemsData.GetRecentFileAsync(recentItem);
                            file = properties.File as StorageFile;
                        }

                        storageItems.Add(file);
                    }
                    IReadOnlyList<StorageFile> rol = storageItems as IReadOnlyList<StorageFile>;
                    request.SetData(rol);
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void EnableSavingAfterDelay()
        {
            try
            {
                await Task.Delay(3000);
                if (Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                {
                    //ViewModels.ViewerViewModel.Current.ResumeAutoSaving();
                }
                else
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            //ViewModels.ViewerViewModel.Current.ResumeAutoSaving();
                        });
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
