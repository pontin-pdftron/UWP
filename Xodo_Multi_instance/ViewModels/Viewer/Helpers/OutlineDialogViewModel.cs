using CompleteReader.ViewModels.Common;
using pdftron.PDF;
using pdftron.PDF.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;

namespace CompleteReader.ViewModels.Viewer.Helpers
{
    public class OutlineDialogViewModel : ViewModelBase
    {
        public class OutlineDialogStateBundle
        {
            private double _UserBookmarksState = 0;
            public double UserBookmarksState
            {
                get { return _UserBookmarksState; }
                internal set { _UserBookmarksState = value; }
            }
            private List<int> _OutlineState = null;
            public List<int> OutlineState
            {
                get { return _OutlineState; }
                internal set { _OutlineState = value; }
            }
            private double _AnnotationListState = 0;
            public double AnnotationListState
            {
                get { return _AnnotationListState; }
                internal set { _AnnotationListState = value; }
            }

            private SubViews _VisibleState = SubViews.UserBookmarks;
            public SubViews VisibleState { get { return _VisibleState; } }

            public OutlineDialogStateBundle(SubViews subView, double userBookmarksState,
                List<int> outlineState, double annotationListState)
            {
                _VisibleState = subView;
                _UserBookmarksState = userBookmarksState;
                _OutlineState = outlineState;
                _AnnotationListState = annotationListState;
            }
        }

        private OutlineDialogStateBundle _RestoreState = null;
        private bool _HasRestoredUserBookmarks = true;
        private bool _HasRestoredOutline = true;
        private bool _HasRestoredAnnotationList = true;


        private PDFViewCtrl _PDFViewCtrl;
        public PDFViewCtrl PDFViewCtrl
        {
            get { return _PDFViewCtrl; }
            set
            {
                _PDFViewCtrl = value;
            }
        }


        private pdftron.PDF.Tools.NavigationStack _NavigationStack = null;
        public pdftron.PDF.Tools.NavigationStack NavigationStack
        {
            get { return _NavigationStack; }
            set
            {
                _NavigationStack = value;
                if (AnnotationList != null)
                {
                    AnnotationList.NavigationStack = value;
                }
                if (Outline != null)
                {
                    Outline.NavigationStack = value;
                }
                if (UserBookmarks != null)
                {
                    UserBookmarks.NavigationStack = value;
                }
                if (Thumbnails != null)
                {
                    Thumbnails.NavigationStack = value;
                }
            }
        }

        private bool _CloseDialogOnItemClick = false;
        /// <summary>
        /// Gets or sets whether clicking on an item should collapse  the outline.
        /// </summary>
        public bool CloseDialogOnItemClick
        {
            get { return _CloseDialogOnItemClick; }
            set
            {
                if (Set(ref _CloseDialogOnItemClick, value))
                {
                    if (UserBookmarks != null)
                    {
                        UserBookmarks.ShowOverflow = _CloseDialogOnItemClick;
                    }
                }
            }
        }

        public string DocumentTag { get; set; }
        public event EventHandler RequestClosing;
        public event EventHandler RequestSwitchSide;

        public delegate void DocumentModifiedEventHandler(PDFDoc doc);
        public DocumentModifiedEventHandler DocumentModified;

        public delegate void UserBookmarksEditedEventHandler(PDFDoc doc);
        public event UserBookmarksEditedEventHandler UserBookmarksEdited;

        public delegate void PageChangedDelegate(int prevPage, int newPage);
        public event PageChangedDelegate PageChanged;

        private void RaiseDocumentModified(PDFDoc doc)
        {
            if (DocumentModified != null)
            {
                DocumentModified(doc);
            }
        }

        public OutlineDialogViewModel()
        {
            InitCommands();
        }

        public OutlineDialogViewModel(PDFViewCtrl ctrl)
        {
            InitCommands();
            PDFViewCtrl = ctrl;
        }

        bool _ActivatedSubViews = false;
        public void ActivateSubView()
        {
            DocumentTag = "NULL";
            if (_ActivatedSubViews)
            {
                return;
            }
            _ActivatedSubViews = true;
            Thumbnails = new ThumbnailViewer(PDFViewCtrl, DocumentTag);
            Thumbnails.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
            Thumbnails.BlankPageDefaultColor = pdftron.PDF.Tools.UtilityFunctions.GetPostProcessedColor(Windows.UI.Colors.White, PDFViewCtrl);
            Thumbnails.FitItemsToWidth = true;
            Thumbnails.NavigationOnly = true;
            Thumbnails.NumberOfColumns = 2;
            Thumbnails.NavigationStack = NavigationStack;
            Thumbnails.ItemPadding = new Windows.UI.Xaml.Thickness(1, 2, 6, 2);
            Thumbnails.ViewModel.PageClicked += _ViewModel_PageChanged;
            UserBookmarks = new UserBookmarkControl(_PDFViewCtrl, DocumentTag);
            UserBookmarks.ShowOverflow = _CloseDialogOnItemClick;
            UserBookmarks.CanAwalysEdit = false;
            UserBookmarks.AddButtonVisibility = Windows.UI.Xaml.Visibility.Collapsed;
            UserBookmarks.NavigationStack = NavigationStack;
            UserBookmarks._ViewModel.PageChanged += _ViewModel_PageChanged;
            UserBookmarks.BookmarksEdited += UserBookmarks_BookmarksEdited;
            Outline = new Outline(_PDFViewCtrl);
            Outline.NavigationStack = NavigationStack;
            Outline.PageChanged += _ViewModel_PageChanged;
            AnnotationList = new AnnotationList(_PDFViewCtrl);
            AnnotationList.NavigationStack = NavigationStack;
            AnnotationList.ItemClicked += AnnotationList_ItemClicked;
            AnnotationList._ViewModel.PageChanged += _ViewModel_PageChanged;
            UserBookmarks.ItemClicked += ItemClickedHandler;
            Outline.ItemClicked += ItemClickedHandler;
            AnnotationList.ItemClicked += ItemClickedHandler;
            Thumbnails.ItemClicked += ItemClickedHandler;

            UpdateColumnNumber();
        }

        public OutlineDialogStateBundle GetState()
        {
            double userBookmarksState = 0;
            List<int> outlineState = null;
            double annotationListState = 0;
            if (_RestoreState != null && !_HasRestoredUserBookmarks)
            {
                userBookmarksState = _RestoreState.UserBookmarksState;
            }
            else if (UserBookmarks != null)
            {
                userBookmarksState = UserBookmarks.GetState();
            }
            if (_RestoreState != null && !_HasRestoredOutline)
            {
                outlineState = _RestoreState.OutlineState;
            }
            else
            {
                outlineState = Outline.GetState();
            }
            if (_RestoreState != null && !_HasRestoredAnnotationList)
            {
                annotationListState = _RestoreState.AnnotationListState;
            }
            else
            {
                annotationListState = AnnotationList.GetState();
            }
            return new OutlineDialogStateBundle(SubView, userBookmarksState, outlineState, annotationListState);
        }

        public void RestoreState(OutlineDialogStateBundle stateBundle)
        {
            _RestoreState = stateBundle;
            _HasRestoredUserBookmarks = false;
            _HasRestoredOutline = false;
            _HasRestoredAnnotationList = false;
            ActivateSubView();
            SubView = stateBundle.VisibleState;
            AnnotationList.ViewIsVisible = _SubView == SubViews.AnnotationList;
            Thumbnails.ViewIsVisible = _SubView == SubViews.Thumbnails;
            RestoreCurrentSubView();
        }

        private void RestoreCurrentSubView()
        {
            if (_RestoreState != null)
            {
                if (SubView == SubViews.UserBookmarks && !_HasRestoredUserBookmarks)
                {
                    if (_RestoreState.UserBookmarksState != 0)
                    {
                        UserBookmarks.RestoreState(_RestoreState.UserBookmarksState);
                    }
                    _HasRestoredUserBookmarks = true;
                }
                else if (SubView == SubViews.Outline && !_HasRestoredOutline)
                {
                    if (_RestoreState.OutlineState != null)
                    {
                        Outline.RestoreState(_RestoreState.OutlineState);
                    }
                    _HasRestoredOutline = true;
                }
                else if (SubView == SubViews.AnnotationList && !_HasRestoredAnnotationList)
                {
                    if (_RestoreState.AnnotationListState != 0)
                    {
                        AnnotationList.RestoreState(_RestoreState.AnnotationListState);
                    }
                    _HasRestoredAnnotationList = true;
                }
                if (_HasRestoredUserBookmarks && _HasRestoredOutline && _HasRestoredAnnotationList)
                {
                    _RestoreState = null;
                }
            }
        }

        public async void CancelAllViewModels()
        {
            await CleanUpSubViewsAsync();
        }

        public async Task CleanUpSubViewsAsync()
        {
            if (this.AnnotationList != null)
            {
                await this.AnnotationList.WaitForAnnotationListToFinish();
            }
            if (this.UserBookmarks != null)
            {
                await this.UserBookmarks.WaitForBookmarkSavingAsync();
            }
            if (this.Outline != null)
            {
                await Outline.WaitForOutlineToLoadAsync();
            }
        }

        /// <summary>
        /// Call this function to indicate that the user has interacted outside the Outline Dialog
        /// </summary>
        public void InteractionOutsideDialog()
        {
            SaveBookmarks();
            UserBookmarks?._ViewModel?.DeselectAll();
        }

        public void SaveBookmarks()
        {
            if (UserBookmarks != null)
            {
                UserBookmarks.SaveBookmarks();
            }
        }

        private bool _Isconverting = false;
        public bool Isconverting
        {
            get { return _Isconverting; }
            set { Set(ref _Isconverting, value); }
        }

        public enum SubViews
        {
            Thumbnails = 0,
            UserBookmarks,
            Outline,
            AnnotationList,
        }

        private SubViews _SubView = SubViews.Thumbnails;
        public SubViews SubView
        {
            get { return _SubView; }
            set 
            {
                if (Set(ref _SubView, value))
                {
                    SubViewIndex = (int)_SubView;
                    AnnotationList.ViewIsVisible = _SubView == SubViews.AnnotationList;
                    Thumbnails.ViewIsVisible = _SubView == SubViews.Thumbnails;
                    RestoreCurrentSubView();
                }
            }
        }

        public bool HasUnsavedUserbookmarks
        {
            get
            {
                if (UserBookmarks != null && UserBookmarks._ViewModel != null)
                {
                    return UserBookmarks._ViewModel.HasUnsavedBookmarks;
                }
                return false;
            }
        }

        private int _SubviewIndex = 0;
        public int SubViewIndex
        {
            get { return _SubviewIndex; }
            set
            {
                if (Set(ref _SubviewIndex, value))
                {
                    if (SubView != (SubViews)_SubviewIndex)
                    {
                        SubView = (SubViews)_SubviewIndex;
                        Settings.Settings.OutlineDefautlView = _SubviewIndex;
                    }
                }
            }
        }

        private ThumbnailViewer _Thumbnails;
        public ThumbnailViewer Thumbnails
        {
            get { return _Thumbnails; }
            set { Set(ref _Thumbnails, value); }
        }

        private Outline _Outline;
        public Outline Outline
        {
            get { return _Outline; }
            set { Set(ref _Outline, value); }
        }

        private UserBookmarkControl _UserBookmarks;
        public UserBookmarkControl UserBookmarks
        {
            get { return _UserBookmarks; }
            set 
            {
                UserBookmarkControl _oldBookmarks = _UserBookmarks;
                if (Set(ref _UserBookmarks, value))
                {
                    if (_oldBookmarks != null)
                    {
                        _UserBookmarks.DocumentModified -= UserBookmarksDocumentModified;
                    }
                    if (_UserBookmarks != null)
                    {
                        _UserBookmarks.DocumentModified += UserBookmarksDocumentModified;
                    }

                }
            }
        }

        private void UserBookmarksDocumentModified(PDFDoc doc)
        {
            RaiseDocumentModified(doc);
        }

        private AnnotationList _AnnotationList;
        public AnnotationList AnnotationList
        {
            get { return _AnnotationList; }
            set { Set(ref _AnnotationList, value); }
        }

        private Storyboard _FadeAnimation = null;
        public Storyboard FadeAnimation
        {
            get { return _FadeAnimation; }
            set { Set(ref _FadeAnimation, value); }
        }

        public int NumberOfColumns
        {
            get
            {
                if (Thumbnails != null)
                {
                    return Thumbnails.NumberOfColumns;
                }
                return 0;
            }
        }

        private void AnnotationList_ItemClicked(object sender, Windows.UI.Xaml.Controls.ItemClickEventArgs e)
        {
            if (_FadeAnimation != null)
            {
                _FadeAnimation.Begin();
            }
        }

        private void _ViewModel_PageChanged(int prevPage, int newPage)
        {
            PageChanged?.Invoke(prevPage, newPage);
        }

        private void UserBookmarks_BookmarksEdited(PDFDoc doc)
        {
            UserBookmarksEdited?.Invoke(doc);
        }

        private void ItemClickedHandler(object sender, Windows.UI.Xaml.Controls.ItemClickEventArgs e)
        {
            if (CloseDialogOnItemClick)
            {
                RequestClosing?.Invoke(this, new EventArgs());
            }
        }

        private void UpdateColumnNumber()
        {
            if (Thumbnails != null)
            {
                Thumbnails.NumberOfColumns = Settings.Settings.OutlineDialogNumThumbnailColumns;
                RaisePropertyChanged("NumberOfColumns");
            }
        }

        #region Commands

        private void InitCommands()
        {
            SubViewSelectionCommand = new RelayCommand(SubViewSelectionCommandImpl);
            SelectionChangedCommand = new RelayCommand(SelectionChangedCommandImpl);
            CloseCommand = new RelayCommand(CloseCommandImpl);
            SwitchSidesCommand = new RelayCommand(SwitchSidesCommandImpl);
            AddBookmarkCommand = new RelayCommand(AddBookmarkCommandImpl);
            SetNumColumnsCommand = new RelayCommand(SetNumColumnsCommandImpl);
        }

        public RelayCommand SubViewSelectionCommand { get; private set; }
        public RelayCommand SelectionChangedCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        public RelayCommand SwitchSidesCommand { get; private set; }
        public RelayCommand AddBookmarkCommand { get; private set; }
        public RelayCommand SetNumColumnsCommand { get; private set; }

        private void SubViewSelectionCommandImpl(object commandParam)
        {
            string commandName = commandParam as string;
            if (!string.IsNullOrWhiteSpace(commandName))
            {
                if (commandName.Equals("UserBookmarks", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViews.UserBookmarks;
                }
                else if (commandName.Equals("Outline", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViews.Outline;
                }
                else if (commandName.Equals("AnnotationList", StringComparison.OrdinalIgnoreCase))
                {
                    SubView = SubViews.AnnotationList;
                }
            }
        }

        private void SelectionChangedCommandImpl(object parameters)
        {
            if (UserBookmarks != null)
            {
                // We just want to make sure it's in it's default state when opened
                UserBookmarks.GoBack();
            }
        }

        private void CloseCommandImpl(object parameters)
        {
            RequestClosing?.Invoke(this, new EventArgs());
        }

        private void SwitchSidesCommandImpl(object parameters)
        {
            RequestSwitchSide?.Invoke(this, new EventArgs());
        }

        private void AddBookmarkCommandImpl(object parameter)
        {
            if (!Isconverting)
            {
                UserBookmarks?._ViewModel?.AddBookmarkCommand?.Execute(parameter);
            }
        }

        private void SetNumColumnsCommandImpl(object parameter)
        {
            string numString = parameter as string;
            if (!string.IsNullOrEmpty(numString))
            {
                int columnNumber = 1;
                if (int.TryParse(numString, out columnNumber))
                {
                    Settings.Settings.OutlineDialogNumThumbnailColumns = columnNumber;
                    UpdateColumnNumber();
                }
            }
        }

        #endregion Commands

        #region Back Key
        public override bool GoBack()
        {
            if(UserBookmarks != null)
            {
                if (UserBookmarks.GoBack())
                {
                    return true;
                }
            }
            if (Outline != null)
            {
                //if (Outline.GoBack())
                //{
                //    return true;
                //}
            }
            if (AnnotationList != null)
            {
                if (AnnotationList.GoBack())
                {
                    return true;
                }
            }
            SaveBookmarks();
            return false;
        }

        #endregion Back Key

    }
}
