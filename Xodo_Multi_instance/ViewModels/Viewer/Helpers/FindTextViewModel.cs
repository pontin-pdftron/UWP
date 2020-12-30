using CompleteReader.ViewModels.Common;
using pdftron.Common;
using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using UIRect = Windows.Foundation.Rect;

// TODO: check it due some FindText related crashes
namespace CompleteReader.ViewModels.Viewer.Helpers
{
    public class FullSearchItem
    {
        public FullSearchItem(int pageNumber, string searchText, string contextText, string chapter, Highlights highlights, double[] quads)
        {
            this.PageNumber = pageNumber;
            this.SearchText = searchText;
            this.ContextText = contextText;
            this.Chapter = chapter;
            this.Highlights = highlights;
            this.Quads = quads;
        }

        public int PageNumber { get; private set; }
        public string SearchText { get; private set; }
        public string ContextText { get; private set; }
        public string Chapter { get; private set; }
        public Highlights Highlights { get; private set; }
        public double[] Quads { get; private set; }

    }

    public class FindTextViewModel : CompleteReader.ViewModels.Common.ViewModelBase
    {
        private PDFViewCtrl _PDFViewCtrl;
        private pdftron.PDF.Tools.Controls.TextHighlighter _TextHighlighter;
        public FindTextViewModel(PDFViewCtrl ctrl)
        {
            _PDFViewCtrl = ctrl;
            _PDFViewCtrl.OnScale += PDFViewCtrl_OnScale;
            _PDFViewCtrl.OnPageNumberChanged += PDFViewCtrl_OnPageNumberChanged;
            InitCommands();

            if (Settings.Settings.ColorMode == ViewerPageSettingsViewModel.CustomColorModes.Night)
            {
                _CurrentColor = _NightModecolor;
            }
            else
            {
                _CurrentColor = _NormalColor;
            }

            FullSearchItems = new ObservableCollection<FullSearchItem>();
        }

        public delegate void FindTextClosedDelegate();
        public event FindTextClosedDelegate FindTextClosed;
        public delegate void PageChangedDelegate(int prevPage, int newPage);
        public event PageChangedDelegate PageChanged;
        public delegate void FindTextResultFoundDelegate();
        public event FindTextResultFoundDelegate FindTextResultFound;
        public event EventHandler<object> FocusRequested;

        private SolidColorBrush _NightModecolor = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 200, 0, 0));
        private SolidColorBrush _NormalColor = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 100, 0, 255));
        private SolidColorBrush _CurrentColor;


        #region Interaction

        public void ClearViewModel()
        {
            _CancelSearch = true;
            CancelCurrentSearch();
            _PDFViewCtrl.OnScale -= PDFViewCtrl_OnScale;
            _PDFViewCtrl.OnPageNumberChanged -= PDFViewCtrl_OnPageNumberChanged;
        }

        /// <summary>
        /// Wait for this before disposing the PDFDoc, after you call ClearViewModel().
        /// This guarantees that no searching is active.
        /// </summary>
        /// <returns></returns>
        public async Task WaitForTextSearchToCancel()
        {
            await TextSearchSemaphore.WaitAsync();
            TextSearchSemaphore.Release();
        }

        public void FocusTextSearch()
        {
            FocusRequested?.Invoke(this, this);
        }

        public bool HandleKeyboardEvent(Windows.System.VirtualKey key, bool isCtrlDown, bool isAltDown, bool isShiftDown)
        {
            if (IsPrevNextAvailable && key == Windows.System.VirtualKey.F3)
            {
                if (!isCtrlDown && !isAltDown && !isShiftDown)
                {
                    PrevNextImpl(false);
                    return true;
                }
                else if (!isCtrlDown && !isAltDown && isShiftDown)
                {
                    PrevNextImpl(true);
                    return true;
                }
            }

            return false;
        }

        #endregion Interaction


        #region Commands

        private void InitCommands()
        {
            SearchCommand = new RelayCommand(SearchCommandImpl);
            SearchFullCommand = new RelayCommand(SearchFullCommandImpl);
            FullSearchItemClickedCommand = new RelayCommand(FullSearchItemClickedCommandImpl);
            PrevNextCommand = new RelayCommand(PrevNextCommandImpl);
            CloseCommand = new RelayCommand(CloseCommandImpl);

            SearchTermChangedCommand = new RelayCommand(SearchTermChangedCommandImpl);
            SearchTermKeyUpCommand = new RelayCommand(SearchTermKeyUpCommandImpl);
        }

        public RelayCommand SearchCommand { get; private set; }
        public RelayCommand SearchFullCommand { get; private set; }
        public RelayCommand FullSearchItemClickedCommand { get; private set; }
        public RelayCommand PrevNextCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }

        public RelayCommand SearchTermChangedCommand { get; private set; }
        public RelayCommand SearchTermKeyUpCommand { get; private set; }

        private void SearchCommandImpl(object ignore)
        {
            BeginSearch();
        }

        private void SearchFullCommandImpl(object ignore)
        {
            CreateSearchList();
        }

        private void FullSearchItemClickedCommandImpl(object param)
        {
            if (CurrentFullSearchItem != param)
            {
                CurrentFullSearchItem = param as FullSearchItem;
                _PDFViewCtrl.ClearSelection();
            }
            HighlightFullSearchItem();
        }
        private void PrevNextCommandImpl(object direction)
        {
            string dir = direction as string;
            if (!string.IsNullOrWhiteSpace(dir)) {
                bool backwards = dir.Equals("prev", StringComparison.OrdinalIgnoreCase);
                bool forwards = dir.Equals("next", StringComparison.OrdinalIgnoreCase);
                if (backwards || forwards)
                {
                    PrevNextImpl(backwards);
                }
            }
        }

        private void PrevNextImpl(bool goBackwards)
        {
            if (CurrentFullSearchItem != null)
            {
                if (goBackwards)
                {
                    int index = FullSearchItems.IndexOf(CurrentFullSearchItem) - 1;
                    // Check if at first search item and moving back (need to wrap around to end page)
                    if (index < 0)
                    {
                        if (FoundFullSearchItems)
                        {
                            index = FullSearchItems.Count - 1;
                        }
                        // If full search is not completed, use FindTextAsync to get the proper search item in last page 
                        else
                        {
                            CurrentFullSearchItem = null;
                            FindSameTerm(true, _PDFViewCtrl.GetPageCount());
                            return;
                        }
                    }

                    CurrentFullSearchItem = FullSearchItems[index];

                }
                else
                {
                    // Check if at last full search item and trying to move to first page (wrap around)
                    int index = FullSearchItems.IndexOf(CurrentFullSearchItem) + 1;
                    if (index > FullSearchItems.Count - 1)
                    {
                        if (FoundFullSearchItems)
                        {
                            index = 0;
                        }
                        // If full search is not completed, use FindTextAsync incase we are not yet truly at the last item yet
                        else
                        {
                            FindSameTerm(false, CurrentFullSearchItem.PageNumber);
                            CurrentFullSearchItem = null;
                            return;
                        }
                    }
                    CurrentFullSearchItem = FullSearchItems[index];
                }

                HighlightFullSearchItem();
            }
            // Handle case when using prev/next from normal search
            else
            {
                if (goBackwards)
                {
                    FindSameTerm(true);
                }
                else
                {
                    FindSameTerm(false);
                }
            }
        }

        private void CloseCommandImpl(object ignore)
        {
            CloseDialog();
        }

        private void SearchTermChangedCommandImpl(object newText)
        {
            IsTermNotFound = false;
            string text = newText as string;
            IsSearchProgessVisible = false;
            SearchTextTerm = text;
            CancelCurrentSearch();
            IsPrevNextAvailable = false;
        }

        private void SearchTermKeyUpCommandImpl(object keyArgs)
        {
            KeyRoutedEventArgs args = keyArgs as KeyRoutedEventArgs;
            if (args.Key == Windows.System.VirtualKey.Enter)
            {
                BeginSearch();
            }
            else if (args.Key == Windows.System.VirtualKey.Escape)
            {
                CloseDialog();
            }
        }
        private void PDFViewCtrl_OnScale()
        {
            HighlightSelection(_CurrentSearchSelection);
        }

        private void PDFViewCtrl_OnPageNumberChanged(int current_page, int num_pages)
        {
            HighlightSelection(_CurrentSearchSelection);
        }

        #endregion Commands


        #region Visual Properties

        private bool _IsPrevNextAvailable = false;
        public bool IsPrevNextAvailable
        {
            get { return _IsPrevNextAvailable; }
            set { Set(ref _IsPrevNextAvailable, value); }
        }

        private double _SearchProgress = 0;
        public double SearchProgress
        {
            get { return _SearchProgress; }
            set { Set(ref _SearchProgress, value); }
        }

        private bool _IsSearchProgessVisible = false;
        public bool IsSearchProgessVisible
        {
            get { return _IsSearchProgessVisible; }
            set { 
                Set(ref _IsSearchProgessVisible, value);
            }
        }

        private string _SearchTextTerm = string.Empty;
        public string SearchTextTerm
        {
            get { return _SearchTextTerm; }
            set
            {
                if (Set(ref _SearchTextTerm, value))
                {
                    RaisePropertyChanged("IsFullSearchEnabled");
                }
            }
        }

        private string _PrevSearchTextTerm = string.Empty;
        public string PrevSearchTextTerm
        {
            get { return _PrevSearchTextTerm; }
            set { Set(ref _PrevSearchTextTerm, value); }
        }

        private bool _IsInSearchMode = false;
        
        public bool IsInSearchMode
        {
            get { return _IsInSearchMode; }
            set { Set(ref _IsInSearchMode, value); }
        }

        private bool _IsTermNotFound = false;
        public bool IsTermNotFound
        {
            get { return _IsTermNotFound; }
            set { Set(ref _IsTermNotFound, value); }
        }

        private bool _IsSearchBoxEnabled = true;
        public bool IsSearchBoxEnabled
        {
            get { return _IsSearchBoxEnabled; }
            set { Set(ref _IsSearchBoxEnabled, value); }
        }

        public bool IsFullSearchEnabled
        {
            get
            {
                return !string.IsNullOrEmpty(SearchTextTerm);
            }
        }

        #endregion Visual Properties


        #region Searching
        public ObservableCollection<FullSearchItem> FullSearchItems { get; private set; }
        private FullSearchItem _CurrentFullSearchItem;
        public FullSearchItem CurrentFullSearchItem
        {
            get { return _CurrentFullSearchItem; }
            set { Set(ref _CurrentFullSearchItem, value); }
        }

        private IAsyncOperation<pdftron.PDF.PDFViewCtrlSelection> _SearchResult = null;
        private List<Rectangle> _OnScreenSelection = new List<Rectangle>();
        private string _SearchString;
        private bool _CancelSearch = false;
        private bool _issearching = false;
        private bool _IsSearching
        {
            get { return _issearching; }
            set
            {
                DateTime time = DateTime.Now;
                _issearching = value;
            }
        }
        private bool _FoundFullSearchItems = false;
        public bool FoundFullSearchItems
        {
            get { return _FoundFullSearchItems; }
            set { Set(ref _FoundFullSearchItems, value); }
        }

        private int _CurrentFullSearchPage = 1;

        public string CurrentFullSearchStatusText
        {
            get
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                return string.Format(loader.GetString("FindTextDialog_FullSearch_CurrentPageText"), _CurrentFullSearchPage, _PDFViewCtrl.GetPageCount());
            }
        }

        public string FullSearchResultText
        {
            get
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                return string.Format(loader.GetString("FindTextDialog_FullSearch_Results"), FullSearchItems.Count);
            }
        }

        private pdftron.PDF.PDFViewCtrlSelection _CurrentSearchSelection;

        // search progress
        private DispatcherTimer _SearchProgressTimer;
        private int _TicksBeforeProgress = 0;

        private class FullTextSearchCanceller
        {
            public FullTextSearchCanceller()
            {
                Cancel = false;
            }
            public bool Cancel { get; set; }
        }
        private FullTextSearchCanceller _CurrentTextSearchCanceller = null;
        public SemaphoreSlim _TextSearchSemaphore;
        public SemaphoreSlim TextSearchSemaphore
        {
            get
            {
                if (_TextSearchSemaphore == null)
                {
                    _TextSearchSemaphore = new SemaphoreSlim(1);
                }
                return _TextSearchSemaphore;
            }
        }


        private async void CreateSearchList()
        {
            // Check if searching same text to prevent searching again
            if (PrevSearchTextTerm != SearchTextTerm)
            {
                FullSearchItems.Clear();
                PrevSearchTextTerm = SearchTextTerm;
                FoundFullSearchItems = false;
            }
            else
            {
                return;
            }

            FoundFullSearchItems = false;
            PDFDoc doc = _PDFViewCtrl.GetDoc();
            if (doc != null)
            {
                if (_CurrentTextSearchCanceller != null)
                {
                    _CurrentTextSearchCanceller.Cancel = true;
                    _CurrentTextSearchCanceller = null;
                }

                await WaitForTextSearchToCancel();

                FullTextSearchCanceller textSearchCanceller = new FullTextSearchCanceller();
                _CurrentTextSearchCanceller = textSearchCanceller;
                PageIterator pageIter = null;

                bool isDocLocked = false;
                try
                {
                    _PDFViewCtrl.DocLockRead();
                    isDocLocked = true;
                    pageIter = doc.GetPageIterator();
                }
                catch (Exception) { }
                finally
                {
                    if (isDocLocked)
                    {
                        _PDFViewCtrl.DocUnlockRead();
                    }
                }

                _CurrentFullSearchPage = 1;
                IList<FullSearchItem> items = await GetNextPageWithSearchItemsAsync(pageIter, doc, textSearchCanceller);
                while (items != null)
                {
                    if (textSearchCanceller.Cancel)
                    {
                        break;
                    }

                    foreach (var item in items)
                    {
                        FullSearchItems.Add(item);
                    }

                    _CurrentFullSearchPage++;
                    RaisePropertyChanged("CurrentFullSearchStatusText");
                    items = await GetNextPageWithSearchItemsAsync(pageIter, doc, textSearchCanceller);
                }

                RaisePropertyChanged("FullSearchResultText");
                FoundFullSearchItems = true;
            }
        }

        private IAsyncOperation<IList<FullSearchItem>> GetNextPageWithSearchItemsAsync(PageIterator pageIter, PDFDoc doc, FullTextSearchCanceller canceller)
        {
            Task<IList<FullSearchItem>> t = new Task<IList<FullSearchItem>>(() =>
            {
                return GetNextPageWithSearchItems(pageIter, doc, canceller);
            });
            t.Start();
            return t.AsAsyncOperation<IList<FullSearchItem>>();
        }


        private IList<FullSearchItem> GetNextPageWithSearchItems(PageIterator pageIter, PDFDoc doc, FullTextSearchCanceller canceller)
        {
            bool gotSema = false;
            bool gotDocLock = false;
            try
            {
                TextSearchSemaphore.Wait();
                gotSema = true;
                doc.LockRead();
                gotDocLock = true;

                IList<FullSearchItem> items = new List<FullSearchItem>();
                while (pageIter.HasNext() && !canceller.Cancel)
                {
                    TextSearch textSearch = new TextSearch();
                    textSearch.SetMode((int)(TextSearchSearchMode.e_page_stop | TextSearchSearchMode.e_highlight | TextSearchSearchMode.e_ambient_string));

                    Int32Ref pageNum = new Int32Ref(0);
                    StringRef resultStr = new StringRef();
                    StringRef ambientStr = new StringRef();
                    Highlights hLts = new Highlights();

                    textSearch.Begin(_PDFViewCtrl.GetDoc(), SearchTextTerm, textSearch.GetMode(), pageIter.GetPageNumber(), pageIter.GetPageNumber());
                    TextSearchResultCode result = textSearch.Run(pageNum, resultStr, ambientStr, hLts);

                    while (result == TextSearchResultCode.e_found)
                    {
                        hLts.Begin(_PDFViewCtrl.GetDoc());
                        var test = hLts.GetCurrentQuads();
                        FullSearchItem item = new FullSearchItem(pageIter.GetPageNumber(), resultStr.Value, ambientStr.Value, "", hLts, test);
                        items.Add(item);
                        hLts = new Highlights();
                        result = textSearch.Run(pageNum, resultStr, ambientStr, hLts);
                    }
                    pageIter.Next();
                    return items;
                }
            }
            catch (Exception) { }
            finally
            {
                if (gotDocLock)
                {
                    doc.UnlockRead();
                }
                if (gotSema)
                {
                    TextSearchSemaphore.Release();
                }
            }
            return null;
        }

        private void BeginSearch()
        {
            // Don't search again if text has not changed
            if (_IsSearching && (_SearchString == SearchTextTerm))
                return;

            _CancelSearch = false;
            _SearchString = SearchTextTerm;
            if (_CurrentFullSearchItem != null)
            {
                PrevNextCommandImpl("next");
                return;
            }

            if (CurrentFullSearchItem != null)
            {
                CurrentFullSearchItem = null;  // Not in full search mode, prev/next command needs to account for this
            }

            if (_SearchString.Length > 0)
            {
                _SearchResult = StartSearchAsync(_SearchString, false, false, false, false);

                if (_SearchResult != null)
                {
                    _SearchResult.Completed = SearchDelegate;
                }

                if (_TextHighlighter == null)
                {
                    _TextHighlighter = new pdftron.PDF.Tools.Controls.TextHighlighter(_PDFViewCtrl, _SearchString);
                }
            }
        }

        private void FindSameTerm(bool goBackwards, int pageNum = -1)
        {
            if (_IsSearching)
            {
                return;
            }
            _SearchResult = StartSearchAsync(_SearchString, false, false, goBackwards, false, pageNum);
            if (_SearchResult != null)
            {
                _SearchResult.Completed = SearchDelegate;
            }
        }


        IAsyncOperation<pdftron.PDF.PDFViewCtrlSelection> StartSearchAsync(String searchString, bool matchCase, bool matchWholeWord, bool searchUp, bool regExp, int pageNum = -1)
        {
            _IsSearching = true;
            _TicksBeforeProgress = 20; // will be 1 second
            if (_SearchProgressTimer != null)
            {
                _SearchProgressTimer.Stop();
            }
            _SearchProgressTimer = new DispatcherTimer();
            _SearchProgressTimer.Tick += SearchProgressTimer_Tick;
            _SearchProgressTimer.Interval = TimeSpan.FromMilliseconds(50);
            _SearchProgressTimer.Start();
            IsTermNotFound = false;

            return _PDFViewCtrl.FindTextAsync(searchString, matchCase, matchWholeWord, searchUp, regExp, pageNum);
        }

        void SearchProgressTimer_Tick(object sender, object e)
        {
            if (_TicksBeforeProgress > 0)
            {
                _TicksBeforeProgress--;
            }
            else
            {
                IsSearchProgessVisible = true;
                SearchProgress = 100.0 * _PDFViewCtrl.GetFindTextProgressAsFactor();
            }
        }

        public async void SearchDelegate(IAsyncOperation<pdftron.PDF.PDFViewCtrlSelection> asyncAction, AsyncStatus asyncStatus)
        {
            double searchProgress = SearchProgress;
            SearchProgress = 0;
            await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
            {
                _IsSearching = false;
                if (_CancelSearch)
                {
                    _CancelSearch = false;
                    return;
                }
                ResetSearchProgress();
                switch (asyncStatus)
                {
                    case AsyncStatus.Error:
                        //SearchResultOutput.Text = "Error";
                        break;
                    case AsyncStatus.Completed:
                        if (_CancelSearch)
                        {
                            return;
                        }
                        _CurrentSearchSelection = asyncAction.GetResults();
                        if (HighlightSelection(_CurrentSearchSelection))
                        {
                            IsPrevNextAvailable = true;
                            FindTextResultFound?.Invoke();
                        }
                        else
                        {
                            IsPrevNextAvailable = false;
                            IsTermNotFound = true;
                            if (_PDFViewCtrl.GetFindTextProgressAsFactor() == 1)
                                ShowTextNotFoundMessage();
                        }
                        break;
                }
            });
        }

        private void HighlightFullSearchItem()
        {
            _SearchString = SearchTextTerm;

            PageChanged?.Invoke(_PDFViewCtrl.GetCurrentPage(), CurrentFullSearchItem.PageNumber);
            _PDFViewCtrl.SetCurrentPage(CurrentFullSearchItem.PageNumber);
            _PDFViewCtrl.Select(CurrentFullSearchItem.Highlights);

            if (_TextHighlighter == null)
            {
                _TextHighlighter = new pdftron.PDF.Tools.Controls.TextHighlighter(_PDFViewCtrl, _SearchString);
            }

            if (HighlightSelection(CurrentFullSearchItem.Quads, CurrentFullSearchItem.Quads.Length / 8, CurrentFullSearchItem.PageNumber))
            {
                _PDFViewCtrl.ClearSelection();
                IsPrevNextAvailable = true;
            }
        }

        private void ClearSelection()
        {
            if (_OnScreenSelection == null)
            {
                return;
            }
            foreach (Rectangle rect in _OnScreenSelection)
            {
                Canvas parent = rect.Tag as Canvas;
                if (parent != null)
                {
                    parent.Children.Remove(rect);
                }
            }
            _OnScreenSelection.Clear();
        }

        private bool HighlightSelection(double[] quads, int numQuads, int page)
        {
            int quadNumber = 0;
            List<UIRect> rects = new List<UIRect>();

            PDFViewCtrlPagePresentationMode mode = _PDFViewCtrl.GetPagePresentationMode();
            int currentPage = _PDFViewCtrl.GetCurrentPage();
            if ((mode == PDFViewCtrlPagePresentationMode.e_single_page && page != currentPage) ||
                (mode == PDFViewCtrlPagePresentationMode.e_facing && (((page + 1) / 2) != ((currentPage + 1) / 2)))  ||
                (mode == PDFViewCtrlPagePresentationMode.e_facing_cover && ((page / 2) != (currentPage / 2))) )
            {
                return false;
            }

            // get highlights in control (screen) space
            for (int i = 0; i < numQuads; i++)
            {
                quadNumber = i * 8;

                pdftron.Common.DoubleRef x1 = new pdftron.Common.DoubleRef(quads[quadNumber + 0]);
                pdftron.Common.DoubleRef y1 = new pdftron.Common.DoubleRef(quads[quadNumber + 1]);

                pdftron.Common.DoubleRef x2 = new pdftron.Common.DoubleRef(quads[quadNumber + 2]);
                pdftron.Common.DoubleRef y2 = new pdftron.Common.DoubleRef(quads[quadNumber + 3]);

                pdftron.Common.DoubleRef x3 = new pdftron.Common.DoubleRef(quads[quadNumber + 4]);
                pdftron.Common.DoubleRef y3 = new pdftron.Common.DoubleRef(quads[quadNumber + 5]);

                pdftron.Common.DoubleRef x4 = new pdftron.Common.DoubleRef(quads[quadNumber + 6]);
                pdftron.Common.DoubleRef y4 = new pdftron.Common.DoubleRef(quads[quadNumber + 7]);

                _PDFViewCtrl.ConvPagePtToScreenPt(x1, y1, page);
                _PDFViewCtrl.ConvPagePtToScreenPt(x2, y2, page);
                _PDFViewCtrl.ConvPagePtToScreenPt(x3, y3, page);
                _PDFViewCtrl.ConvPagePtToScreenPt(x4, y4, page);

                double left, right, top, bottom;

                left = Math.Min(x1.Value, Math.Min(x2.Value, Math.Min(x3.Value, x4.Value)));
                right = Math.Max(x1.Value, Math.Max(x2.Value, Math.Max(x3.Value, x4.Value)));
                top = Math.Min(y1.Value, Math.Min(y2.Value, Math.Min(y3.Value, y4.Value)));
                bottom = Math.Max(y1.Value, Math.Max(y2.Value, Math.Max(y3.Value, y4.Value)));

                rects.Add(new UIRect(left, top, right - left, bottom - top));
            }

            Canvas annotCanvas = _PDFViewCtrl.GetAnnotationCanvas();

            ClearSelection();

            // add highlight(s) to annotation canvas
            foreach (UIRect rect in rects)
            {
                Rectangle highlight = new Rectangle();
                highlight.Fill = _CurrentColor;
                highlight.Width = rect.Width;
                highlight.Height = rect.Height;

                var rLeft = new pdftron.Common.DoubleRef(rect.Left);
                var rTop = new pdftron.Common.DoubleRef(rect.Top);
                _PDFViewCtrl.ConvScreenPtToAnnotationCanvasPt(rLeft, rTop);
                Canvas.SetLeft(highlight, rLeft.Value);
                Canvas.SetTop(highlight, rTop.Value);
                annotCanvas.Children.Add(highlight);
                _OnScreenSelection.Add(highlight);
                highlight.Tag = annotCanvas;
            }

            return numQuads > 0;
        }

        /// <summary>
        /// Highlights the search result if any
        /// </summary>
        /// <param name="result">A text selection acquired by mPDFView.FindText</param>
        /// <returns>true if and only if the selections contains at least one highlight</returns>
        private bool HighlightSelection(pdftron.PDF.PDFViewCtrlSelection result)
        {
            if (result == null)
            {
                return false;
            }

            double[] quads = result.GetQuads();
            int numQuads = result.GetQuadArrayLength() / 8;
            return HighlightSelection(quads, numQuads, result.GetPageNum());
        }

        void ResetSearchProgress()
        {
            IsSearchProgessVisible = false;
            SearchProgress = 0;
            if (_SearchProgressTimer != null)
            {
                _SearchProgressTimer.Stop();
            }
        }

        #endregion Searching


        #region Utilities

        private void CloseDialog()
        {
            if (FindTextClosed != null)
            {
                FindTextClosed();
            }
            CancelCurrentSearch();
        }

        private void CancelCurrentSearch()
        {
            _PDFViewCtrl.CancelFindText();
            IsPrevNextAvailable = false;
            ClearSelection();
            ResetSearchProgress();
            if (_TextHighlighter != null)
            {
                _TextHighlighter.Detach();
                _TextHighlighter = null;
            }
            if (_CurrentTextSearchCanceller != null)
            {
                _CurrentTextSearchCanceller.Cancel = true;
            }
        }

        private async void ShowTextNotFoundMessage()
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog dialog = new MessageDialog(loader.GetString("FindTextDialog_NotFoundText"));
            await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(dialog);
        }
        #endregion Utilities
    }
}
