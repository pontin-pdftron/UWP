using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace CompleteReader.Utilities
{
    class AutoSaveHelper
    {
        PDFDoc _Doc;
        private TimeSpan _Interval;
        private DispatcherTimer _SaveTimer;

        public delegate void DocumentWasSavedHandler(DateTimeOffset newModifiedDate, ulong newFilesize);
        public event DocumentWasSavedHandler DocumentWasSaved;

        public delegate Task CommitChangesAsyncDelegate();
        public event CommitChangesAsyncDelegate CommitChangesAsync;

        /// <summary>
        /// Sets the document to save to
        /// </summary>
        public PDFDoc PDFDoc { 
            set 
            {
                _HasSavedSinceGettingDoc = false;
                _Doc = value;
                Stop();
                Start();
            } 
            get
            {
                return _Doc;
            }
        }

        /// <summary>
        /// Sets the interval in seconds between auto-saves.
        /// </summary>
        public double Interval { 
            set 
            {
                _Interval = TimeSpan.FromSeconds(value);
                if (_SaveTimer != null)
                {
                    _SaveTimer.Interval = _Interval;
                }
            } 
        }

        public bool IsPaused { get; set; }

        private bool _HasSavedSinceGettingDoc = false;
        /// <summary>
        /// This variable lets you know if the auto-saver has saved the document, in case you want to display a notification.
        /// </summary>
        public bool HasSavedSinceGettingDoc { get { return _HasSavedSinceGettingDoc; } }

        /// <summary>
        /// The current file.
        /// </summary>
        public StorageFile CurrentFile { get; set; }
        /// <summary>
        /// The current temporary file.
        /// </summary>
        public StorageFile TemporaryFile { get; set; }

        /// <summary>
        /// Creates an AutoSaveHelper that will save every 30 seconds.
        /// </summary>
        public AutoSaveHelper()
        {
            Init(30);
        }

        /// <summary>
        /// Creates an AutoSaveHelper that will save every interval seconds.
        /// </summary>
        public AutoSaveHelper(double intervalInSeconds)
        {
            Init(intervalInSeconds);
        }

        /// <summary>
        /// Stops the AutoSaveHelper
        /// </summary>
        public void Stop()
        {
            if (_SaveTimer != null)
            {
                _SaveTimer.Stop();
            }
        }

        /// <summary>
        /// Starts the AutoSaveHelper
        /// </summary>
        public void Start()
        {
            if (_SaveTimer != null)
            {
                _SaveTimer.Start();
            }
        }

        private void Init(double interval)
        {
            _SaveTimer = new DispatcherTimer();
            _SaveTimer.Tick += SaveTimer_Tick;
            _Interval = TimeSpan.FromSeconds(interval);
            _SaveTimer.Interval = _Interval;
            IsPaused = false;
        }

        private void SaveTimer_Tick(object sender, object e)
        {
            if (!IsPaused)
            {
                Save();
            }
        }

        private async void Save()
        {
            if (_Doc != null && CurrentFile != null && TemporaryFile != null)
            {
                bool locked = false;
                try
                {
                    locked = _Doc.TryLock();
                    if (locked)
                    {
                        if (_Doc.IsModified())
                        {
                            _Doc.Unlock();
                            locked = false;
                            DateTime preSave = DateTime.Now;
                            if (CommitChangesAsync != null)
                            {
                                Task commitTask = CommitChangesAsync();
                                if (commitTask != null)
                                {
                                    await commitTask;
                                }
                            }
                            await _Doc.SaveAsync(pdftron.SDF.SDFDocSaveOptions.e_incremental);
                            DocumentManager manager = await DocumentManager.GetInstanceAsync();
                            await manager.AddChangesToOriginal(CurrentFile, TemporaryFile);
                            Windows.Storage.FileProperties.BasicProperties props = await TemporaryFile.GetBasicPropertiesAsync();
                            
                            TimeSpan saveTime = DateTime.Now.Subtract(preSave);
                            _HasSavedSinceGettingDoc = true;
                            if (DocumentWasSaved != null)
                            {
                                DocumentWasSaved(props.DateModified, props.Size);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    pdftron.Common.PDFNetException pdfnetE = new pdftron.Common.PDFNetException(ex.HResult);
                    if (pdfnetE.IsPDFNetException)
                    {
                        System.Diagnostics.Debug.WriteLine("Error: " + pdfnetE.ToString());
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Error: " + ex.ToString());
                    }
                }
                finally
                {
                    if (locked)
                    {
                        _Doc.Unlock();
                    }
                }

            }
        }
    }
}
