using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Security.Cryptography;

namespace Xodo_Windows10
{
    public static class Program
    {
        const int MAX_NUMBER_INSTANCES = 2;
        const string INSTANCE_ONE_NAME = "instance1";
        const string INSTANCE_TWO_NAME = "instance2";

        static void Main(string[] args)
        {
            // Init PDFTron stuff
            pdftron.PDFNet.Initialize();

            // First, we'll get our activation event args, which are typically richer
            // than the incoming command-line args. We can use these in our app-defined
            // logic for generating the key for this instance.
            IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();

            // If the Windows shell indicates a recommended instance, then
            // the app can choose to redirect this activation to that instance instead.
            if (AppInstance.RecommendedInstance != null)
            {
                AppInstance.RecommendedInstance.RedirectActivationTo();
            }
            else
            {
                // TODO: 
                /*
                 * - Ensure LocalState folders are properly created
                 * - Limit the number of instances to 2 for now
                 * - Exceptions? DocumentPreview ?
                 */
                var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                string res_path = localFolder.Path;
                
                var instances = AppInstance.GetInstances();
                if (instances.Count >= MAX_NUMBER_INSTANCES)
                {
                    // TODO: Message saying Max number fo instances reached
                    return;
                }
                else
                {
                    // Check if Instance folders are already created
                    if (!File.Exists(Path.Combine(res_path, INSTANCE_ONE_NAME)) && !File.Exists(Path.Combine(res_path, INSTANCE_TWO_NAME)))
                    {                        
                        _ = localFolder.CreateFolderAsync("instance1");
                        _ = localFolder.CreateFolderAsync("instance2");
                    }
                }

                // TODO: find a better way to get the current instance
                string instancePath = Path.Combine(res_path, "instance" + (instances.Count + 1).ToString());

                pdftron.PDFNet.AddResourceSearchPath(Path.Combine(instancePath, CompleteReader.Settings.Settings.FONT_DIRECTORY));
                pdftron.PDFNet.SetResourcesPath(instancePath);
                pdftron.PDFNet.SetPersistentCachePath(instancePath);

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
                pdftron.PDF.DocumentPreviewCache.ClearCache(); // attempt to prevent startup crash 


                // Define a key for this instance, based on some app-specific logic.
                // If the key is always unique, then the app will never redirect.
                // If the key is always non-unique, then the app will always redirect
                // to the first instance. In practice, the app should produce a key
                // that is sometimes unique and sometimes not, depending on its own needs.
                string key = Guid.NewGuid().ToString(); // always unique.
                                                        //string key = "Some-App-Defined-Key"; // never unique.
                var instance = AppInstance.FindOrRegisterInstanceForKey(key);

                


                if (instance.IsCurrentInstance)
                {
                    // If we successfully registered this instance, we can now just
                    // go ahead and do normal XAML initialization.
                    global::Windows.UI.Xaml.Application.Start((p) => new CompleteReader.App());
                }
                else
                {
                    // Some other instance has registered for this key, so we'll 
                    // redirect this activation to that instance instead.
                    instance.RedirectActivationTo();
                }
            }
        }
    }
}
