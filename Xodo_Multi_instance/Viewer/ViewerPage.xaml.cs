using CompleteReader.ViewModels.Viewer.Helpers;
using pdftron.PDF.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CompleteReader.Viewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ViewerPage : CompleteReader.Pages.Common.NavigablePage
    {
        private AppBar _TopAppBar;
        private AppBar _BottomAppBar;
        private CompleteReader.ViewModels.Viewer.ViewerViewModel _DataContext;

        private bool _SmallScreen = false;
        private bool SmallScreen
        {
            get { return _SmallScreen; }
            set
            {
                if (value != _SmallScreen)
                {
                    _SmallScreen = value;
                    if (_DataContext != null)
                    {
                        _DataContext.OutlineDialogNavigationCloses = _SmallScreen;
                    }
                }
            }
        }

        public ViewerPage()
        {
            this.InitializeComponent();
            this.SizeChanged += ViewerPage_SizeChanged;
            this.RightTapped += ViewerPage_RightTapped;
            BackgroundGrid.Loaded += BackgroundGrid_Loaded;
            CompleteReaderTopAppBar.Opening += CompleteReaderTopAppBar_Opening;
            CompleteReaderTopAppBar.Closing += CompleteReaderTopAppBar_Closing;
            CompeteReaderTopCommandBar.Opening += CompeteReaderTopCommandBar_Opening;
            CompeteReaderTopCommandBar.Closing += CompeteReaderTopCommandBar_Closing;

            if (this.Resources.ContainsKey("AppBarThemeCompactHeight"))
            {
                _CommandbarHeight = (double)this.Resources["AppBarThemeCompactHeight"];
                _CommandBarCompactHeight = _CommandbarHeight;
            }

            _TopAppBar = CompleteReaderTopAppBar;
            _BottomAppBar = CompeteReaderBottomAppBar;
            PlaceOutlineDialog();

            string deviceFamily = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
            if (!string.IsNullOrWhiteSpace(deviceFamily) && deviceFamily.Equals("Windows.Mobile", StringComparison.OrdinalIgnoreCase))
            {
                AppBarButton_Files.Visibility = Visibility.Collapsed;
            }

            double tabButtonHeight = (double)Resources["OutlineTabButtonSize"];
        }

        private double _CommandBarCompactHeight = 48;
        private double _CommandbarHeight = 48;
        private double CommandbarHeight
        {
            get { return _CommandbarHeight; }
            set
            {
                if (_CommandbarHeight != value)
                {
                    _CommandbarHeight = value;
                    TabButtonControl.Margin = new Thickness(0, Math.Max(0, _CommandbarHeight - _CommandBarCompactHeight), 0, 0);
                }
            }
        }

        private void Settings_SettingUpdated(string settingName)
        {
            if (settingName == "OutlineDialogAnchorSide")
            {
                PlaceOutlineDialog();
            }

            if (settingName == "OutlineDialogWidth")
            {
                PlaceOutlineDialog();
            }
        }

        #region Outline Positioning

        private Dialogs.OutlineDialog.AnchorSides _OldOutlineAnchorSide = Dialogs.OutlineDialog.AnchorSides.None;
        private double _OldWidth = 0;
        private void PlaceOutlineDialog()
        {
            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                OutlineDialog.AnchorSide = Dialogs.OutlineDialog.AnchorSides.None;
            }
            else
            {
                OutlineDialog.AnchorSide = Settings.SharedSettings.OutlineDialogAnchorSide;
            }
            double newWidth = Math.Min(this.ActualWidth, 400);
            if (OutlineDialog.AnchorSide == _OldOutlineAnchorSide && newWidth == _OldWidth)
            {
                return;
            }
            _OldWidth = newWidth;
            if (OutlineDialog.AnchorSide == Dialogs.OutlineDialog.AnchorSides.None)
            {
                NonModalDisplayGrid.BorderThickness = new Thickness(0, 1, 0, 0);
                OutlineHostGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                OutlineSpaceHolderGrid.SetValue(Grid.ColumnProperty, 1);
                SnapResizeHost.Visibility = Visibility.Collapsed;
                OutlineHostEntranceControl.EntranceAnimation = FadeInOutline;
                OutlineHostEntranceControl.ExitAnimation = FadeOutOutline;
                if (!OutlineHostEntranceControl.IsOpen)
                {
                    OutlineHostGridTranslation.X = 0;
                    OutlineHostEntranceControl.Opacity = 0;
                }
                UpdateOutlineWidthPosition();
            }
            else if (OutlineDialog.AnchorSide == Dialogs.OutlineDialog.AnchorSides.Left)
            {
                NonModalDisplayGrid.BorderThickness = new Thickness(0, 1, 1, 0);
                OutlineSpaceHolderGrid.SetValue(Grid.ColumnProperty, 0);
                OutlineHostGrid.HorizontalAlignment = HorizontalAlignment.Left;
                SnapResizeHost.HorizontalAlignment = HorizontalAlignment.Left;
                OutlineHostEntranceControl.EntranceAnimation = SlideInOutline;
                OutlineHostEntranceControl.ExitAnimation = SlideOutOutlineToLeft;
                if (!OutlineHostEntranceControl.IsOpen)
                {
                    OutlineHostEntranceControl.Opacity = 1;
                    OutlineHostGridTranslation.X = -OutlineDialog.Width;
                }
                OutlineResizer.Offsets = GetOutlineOffsets();
                OutlineResizer.Position = Settings.Settings.OutlineDialogWidth;
                UpdateOutlineWidthPosition();
            }
            else if (OutlineDialog.AnchorSide == Dialogs.OutlineDialog.AnchorSides.Right)
            {
                NonModalDisplayGrid.BorderThickness = new Thickness(1, 1, 0, 0);
                OutlineSpaceHolderGrid.SetValue(Grid.ColumnProperty, 2);
                OutlineHostGrid.HorizontalAlignment = HorizontalAlignment.Right;
                SnapResizeHost.HorizontalAlignment = HorizontalAlignment.Right;
                OutlineHostEntranceControl.EntranceAnimation = SlideInOutline;
                OutlineHostEntranceControl.ExitAnimation = SlideOutOutlineToRight;
                if (!OutlineHostEntranceControl.IsOpen)
                {
                    OutlineHostEntranceControl.Opacity = 1;
                    OutlineHostGridTranslation.X = OutlineDialog.Width;
                }
                OutlineResizer.Offsets = GetOutlineOffsets();
                OutlineResizer.Position = 2 - Settings.Settings.OutlineDialogWidth;
                UpdateOutlineWidthPosition();
            }
        }

        private RoutedEventHandler _OutlineResizer_PositionUpdated;
        private void OutlineResizer_PositionUpdated(object sender, RoutedEventArgs e)
        {
            int position = OutlineResizer.Position;
            if (OutlineDialog.AnchorSide == Dialogs.OutlineDialog.AnchorSides.Right)
            {
                position = 2 - position;
            }
            Settings.Settings.OutlineDialogWidth = position;
            UpdateOutlineWidthPosition();
        }

        private void UpdateOutlineWidthPosition()
        {
            int position = Settings.Settings.OutlineDialogWidth;
            if (position == 0)
            {
                OutlineDialog.Width = OutlineDialog.LayoutMinWidths[Dialogs.OutlineDialog.WidthLayouts.Narrow];
            }
            else if (position == 1)
            {
                OutlineDialog.Width = OutlineDialog.LayoutMinWidths[Dialogs.OutlineDialog.WidthLayouts.Full];
            }
            else
            {
                OutlineDialog.Width = Math.Min(this.ActualWidth, 400.0);
            }
        }

        private List<double> GetOutlineOffsets()
        {
            List<double> offsets = new List<double>();
            if (OutlineDialog.AnchorSide == Dialogs.OutlineDialog.AnchorSides.Left)
            {
                offsets.Add(OutlineDialog.LayoutMinWidths[Dialogs.OutlineDialog.WidthLayouts.Narrow]);
                offsets.Add(OutlineDialog.LayoutMinWidths[Dialogs.OutlineDialog.WidthLayouts.Full]);
                offsets.Add(Math.Min(this.ActualWidth, 400));
            }
            else if (OutlineDialog.AnchorSide == Dialogs.OutlineDialog.AnchorSides.Right)
            {
                offsets.Add(500 - Math.Min(this.ActualWidth, 400));
                offsets.Add(500 - OutlineDialog.LayoutMinWidths[Dialogs.OutlineDialog.WidthLayouts.Full]);
                offsets.Add(500 - OutlineDialog.LayoutMinWidths[Dialogs.OutlineDialog.WidthLayouts.Narrow]);
            }
            return offsets;
        }

        #endregion Outline Positioning

        private async void SearchFlyout_OnOpened(object sender, object e)
        {
            await Task.Delay(100);
            this.FindTextDialog.SetFocus();
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryShow();
        }

        private void HandleSearchUi(double width)
        {
            if (width < 450)
            {
                //AppBarButton_SearchBox.Width = 150;
                //AppBarButton_SearchBox.Margin = new Thickness(0, 0, 5, 0);
                //AppBarButton_SearchOpen.Width = 40;
                AppBarButton_FindClose.Width = 40;
                AppBarButton_FindClose.Visibility = Visibility.Collapsed;
                FullTextSearchListView.ItemTemplate = (DataTemplate)Resources["SmallFullTextSearchListViewDataTemplate"];
            }
            else 
            {
                //AppBarButton_SearchBox.Width = width < 750 ? 200 : 300;
                //AppBarButton_SearchBox.Margin = new Thickness(0);
                //AppBarButton_SearchOpen.Width = 68;
                AppBarButton_FindClose.Width = 68;
                AppBarButton_FindClose.Visibility = Visibility.Visible;
                FullTextSearchListView.ItemTemplate = (DataTemplate)Resources["FullTextSearchListViewDataTemplate"];
            }

            
        }
        
        private void ViewerPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _DataContext?.ToolManager?.CloseOpenDialog(true);

            SmallScreen = (e.NewSize.Width < 500 || (e.NewSize.Height < 500 && e.NewSize.Width < 700));
            PlaceOutlineDialog();
            HandleSearchUi(e.NewSize.Width);

            if ((e.NewSize.Width < 700))
            {
                Binding binding = AppBarButton_Save.GetBindingExpression(AppBarButton.VisibilityProperty)?.ParentBinding;
                if (binding != null)
                {
                    AppBarButton_Save2.SetBinding(AppBarButton.VisibilityProperty, binding);
                    AppBarButton_Save.Visibility = Visibility.Collapsed;
                }

                Binding binding2 = AppBarButton_SaveAs.GetBindingExpression(AppBarButton.VisibilityProperty)?.ParentBinding;
                if (binding2 != null)
                {
                    AppBarButton_SaveAs2.SetBinding(AppBarButton.VisibilityProperty, binding2);
                    AppBarButton_SaveAs.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Binding binding = AppBarButton_Save2.GetBindingExpression(AppBarButton.VisibilityProperty)?.ParentBinding;
                if (binding != null)
                {
                    AppBarButton_Save.SetBinding(AppBarButton.VisibilityProperty, binding);
                    AppBarButton_Save2.Visibility = Visibility.Collapsed;
                }

                Binding binding2 = AppBarButton_SaveAs2.GetBindingExpression(AppBarButton.VisibilityProperty)?.ParentBinding;
                if (binding != null)
                {
                    AppBarButton_SaveAs.SetBinding(AppBarButton.VisibilityProperty, binding2);
                    AppBarButton_SaveAs2.Visibility = Visibility.Collapsed;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_SettingsUpdatedHandler == null)
            {
                _SettingsUpdatedHandler = new Settings.SharedSettings.SettingUpdatedDelegate(Settings_SettingUpdated);
                Settings.SharedSettings.SettingUpdated += _SettingsUpdatedHandler;
            }

            CompleteReader.ViewModels.Viewer.ViewerViewModel viewModel = null;
            if (e.Parameter != null && e.Parameter is CompleteReader.ViewModels.Viewer.ViewerViewModel)
            {
                viewModel = e.Parameter as CompleteReader.ViewModels.Viewer.ViewerViewModel;
                this.DataContext = viewModel;
                _DataContext = viewModel;
            }
            base.OnNavigatedTo(e);

            if (viewModel == null)
            {
                viewModel = new CompleteReader.ViewModels.Viewer.ViewerViewModel();
                _DataContext = viewModel;
                this.DataContext = _DataContext;
                base.ActivateViewModel(e.Parameter);
            }

            _DataContext.OpenSearch += _DataContext_OpenSearch;

            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                _DataContext.IsUiPinned = false;
                AppBarButton_PinUI.Visibility = Visibility.Collapsed;
                AppBarButton_UnpinUI.Visibility = Visibility.Collapsed;
            }

            viewModel.OutlineDialogNavigationCloses = SmallScreen;
            viewModel.PropertyChanged += viewModel_PropertyChanged;
            viewModel.AnnotationToolbar.AppBarChanged += ViewerPage_AppBarChanged;
            viewModel.FindTextResultFound += ViewModel_FindTextResultFound;
            _OutlineResizer_PositionUpdated = new RoutedEventHandler(OutlineResizer_PositionUpdated);
            OutlineResizer.PositionUpdated += _OutlineResizer_PositionUpdated;
        }

        private Settings.SharedSettings.SettingUpdatedDelegate _SettingsUpdatedHandler;     
        
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_SettingsUpdatedHandler != null)
            {
                Settings.SharedSettings.SettingUpdated -= _SettingsUpdatedHandler;
                _SettingsUpdatedHandler = null;
            }

            if (_OutlineResizer_PositionUpdated != null)
            {
                OutlineResizer.PositionUpdated -= _OutlineResizer_PositionUpdated;
                _OutlineResizer_PositionUpdated = null;
            }

            base.OnNavigatingFrom(e);

            this.DataContext = null;
        }


        protected override void navigableViewModel_NewINavigableAvailable(ViewModels.Common.INavigable sender, ViewModels.Common.INavigable newNavigable)
        {
            if (newNavigable != null && newNavigable != sender)
            {
                if (newNavigable is CompleteReader.ViewModels.Viewer.ViewerViewModel)
                {
                    if (_DataContext != null)
                    {
                        base.DeactivateViewModel(_DataContext);
                        _DataContext.PropertyChanged -= viewModel_PropertyChanged;
                    }
                    _DataContext = (newNavigable as CompleteReader.ViewModels.Viewer.ViewerViewModel);
                    this.DataContext = _DataContext;
                    _DataContext.PropertyChanged += viewModel_PropertyChanged;
                    _DataContext.OpenSearch += _DataContext_OpenSearch;
                    ResolveEditButtonFlyout();
                    base.ActivateViewModel(newNavigable);
                }
                else
                {
                    if (this.Frame.CanGoBack)
                    {
                        this.Frame.GoBack();
                    }
                    else if (newNavigable is CompleteReader.ViewModels.Document.DocumentViewModel)
                    {
                        this.Frame.Navigate(typeof(Documents.DocumentBasePage), newNavigable);
                    }
                }
            }
        }

        private void _DataContext_OpenSearch(object sender, object e)
        {
            AppBarButton_Search.Flyout.ShowAt(AppBarButton_Search);
        }

        async void viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // hide the ViewerPage app bar in thumbnails/crop view
            if (e.PropertyName.Equals("IsThumbnailsViewOpen") || e.PropertyName.Equals("IsCropViewOpen"))
            {
                if (_DataContext.IsThumbnailsViewOpen || _DataContext.IsCropViewOpen)
                {
                    this.TopAppBar = null;
                    this.BottomAppBar = null;
                    AppBarButton_ViewMode.Flyout.Hide();
                    SetViewerPageUp();
                }
                else
                {
                    this.TopAppBar = _TopAppBar;
                    this.BottomAppBar = _BottomAppBar;
                    if (Settings.Settings.PinCommandBar)
                    {
                        SetViewerPageDown();
                    }
                }
            }

            if (e.PropertyName.Equals("IsFullScreen"))
            {
                AppBarButton_ViewMode.Flyout.Hide();
            }
        
            if (e.PropertyName.Equals("MediaElement"))
            {
                // remove any previous text to speech elements placed
                foreach (var child in BackgroundGrid.Children)
                {
                    if (child is MediaElement && (child as MediaElement).Name == "TextToSpeechMedia")
                    {
                        BackgroundGrid.Children.Remove(child);
                    }
                }

                MediaElement media = _DataContext.MediaElement;
                media.Name = "TextToSpeechMedia";

                if (media != null)
                {
                    // need to add MediaElement to UI tree in order to access its events
                    BackgroundGrid.Children.Add(media);
                    media.MediaEnded += (a, b) => { _DataContext.AreAudioButtonsVisible = false; };
                }
            }

            if (e.PropertyName.Equals("IsQuickSettingsOpen"))
            {
                if (!_DataContext.IsQuickSettingsOpen)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        AppBarButton_ViewMode.Flyout.Hide();
                    });
                }
            }

            if (e.PropertyName.Equals("IsAnnotationToolbarOpen"))
            {
                if (_DataContext.IsAnnotationToolbarOpen)
                {
                    SetViewerPageSlightlyDown();
                    TabStoryBoardClose.Begin();
                }
                else
                {
                    if (!Settings.Settings.PinCommandBar)
                    {
                        SetViewerPageUp();
                    }
                    else
                    {
                        SetViewerPageDown();
                    }
                    TabStoryBoardCollapse.Begin();
                }
            }

            if (e.PropertyName.Equals("IsUiPinned"))
            {
                if (_DataContext.IsUiPinned)
                {
                    SetViewerPageDown();
                }
                else
                {
                    SetViewerPageUp();
                }
            }
            if (e.PropertyName.Equals("IsConverting"))
            {
                ResolveEditButtonFlyout();
            }
        }

        private void ResolveEditButtonFlyout()
        {
            if (_DataContext != null && _DataContext.IsConverting)
            {
                if (AppBarButton_Edit.Flyout == null)
                {
                    AppBarButton_Edit.Flyout = AppBarButton_Edit.Resources["EditButtonFlyout"] as Flyout;
                }
            }
            else
            {
                AppBarButton_Edit.Flyout = null;
            }
        }

        private void FindText_ContentIsAvailable(Pages.Common.EntranceAnimationContentControl control)
        {
            this.FindTextDialog.SetFocus();
        }

        public void ActivateWithFile(Windows.Storage.StorageFile file, ViewModels.FileOpening.NewDocumentProperties properties = null)
        {
            CompleteReader.ViewModels.Viewer.ViewerViewModel viewModel = this.DataContext as CompleteReader.ViewModels.Viewer.ViewerViewModel;
            if (viewModel != null)
            {
                viewModel.ActivateWithFile(file, properties);
                viewModel.IsThumbnailsViewOpen = false;
            }
        }

#region Command Bar Expansion

        private void BackgroundGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Settings.PinCommandBar)
            {
                SetViewerPageDown();
            }
        }

        private void ViewerPage_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                HandleCommandBarInteraction();
                e.Handled = true;
            }
        }

        private void HandleCommandBarInteraction()
        {
            var annotationCommandBar = (AnnotationBorder.Child as AnnotationCommandBar);
            if (!Settings.Settings.PinCommandBar)
            {
                if (!_DataContext.IsEntireAppBarOpen)
                {
                    _DataContext.IsEntireAppBarOpen = true;
                }
                else if (!_DataContext.IsAnnotationToolbarOpen)
                {
                    _DataContext.IsAppBarOpen = false;
                    _DataContext.IsEntireAppBarOpen = !_DataContext.IsEntireAppBarOpen;
                    annotationCommandBar.IsAppBarOpen = false;
                }
                else
                {
                    (AnnotationBorder.Child as AnnotationCommandBar).IsAppBarOpen = !(AnnotationBorder.Child as AnnotationCommandBar).IsAppBarOpen;
                    _DataContext.UpdateUIVisiblity();
                }
            }
            else
            {
                if (!_DataContext.IsAnnotationToolbarOpen)
                {
                    _DataContext.IsAppBarOpen = !_DataContext.IsAppBarOpen;
                }
                else
                {
                    if (!annotationCommandBar.IsInkOpen && !annotationCommandBar.IsPolygonSaveOpen)
                    {
                        (AnnotationBorder.Child as AnnotationCommandBar).IsAppBarOpen = !(AnnotationBorder.Child as AnnotationCommandBar).IsAppBarOpen;
                        _DataContext.UpdateUIVisiblity();
                    }
                }
            }
        }

        private void ViewerPage_AppBarChanged(bool isOpen)
        {
            if (isOpen)
            {
                HandleOpeningCommandBar();
            }
            else
            {
                HandleClosingCommandBar();
            }
            _DataContext.UpdateUIVisiblity();
        }

        private void CompleteReaderTopAppBar_Opening(object sender, object e)
        {
        }

        private void CompleteReaderTopAppBar_Closing(object sender, object e)
        {
            if (Settings.Settings.PinCommandBar)
            {
                CompleteReaderTopAppBar.IsOpen = true;
                _DataContext.UpdateUIVisiblity();
            }
        }

        private void CompeteReaderTopCommandBar_Opening(object sender, object e)
        {
            HandleOpeningCommandBar();
        }

        private void CompeteReaderTopCommandBar_Closing(object sender, object e)
        {
            HandleClosingCommandBar();
        }

        private void HandleOpeningCommandBar()
        {
            if (_DataContext != null && _DataContext.IsAnnotationToolbarOpen)
            {
                CommandbarHeight = 60; // Magic number, height AppBar expands to if all buttons have only 1 line of text
            }
            else
            {
                CommandbarHeight = 76; // Magic number, height AppBar expands to if at least one button has 2 lines of text
            }

            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                TabStoryBoardCollapse.Begin();
            }
            else
            {
                TabStoryBoardExpand.Begin();
            }
        }

        private void HandleClosingCommandBar()
        {
            CommandbarHeight = _CommandBarCompactHeight;

            if (_DataContext.IsAnnotationToolbarOpen)
            {
                TabStoryBoardClose.Begin();
            }
            else
            {
                TabStoryBoardCollapse.Begin();
            }
        }

        private void SetViewerPageSlightlyDown()
        {
            this.BackgroundGrid.Margin = new Thickness(0, 48, 0, 0);
        }

        private void SetViewerPageDown()
        {
            this.BackgroundGrid.Margin = new Thickness(0, 88, 0, 0);
        }

        private void SetViewerPageUp()
        {
            this.BackgroundGrid.Margin = new Thickness(0, 0, 0, 0);
        }

#endregion Command Bar Expansion

        private void ViewModel_FindTextResultFound()
        {
            if (this.FindTextDialog.DoesSearchBoxHaveFocus)
            {
                //AppBarButton_SearchOpen.Focus(FocusState.Keyboard);
            }
        }

        /// <summary>
        /// Waits until the data context containing the appropriate full search item is ready and 
        /// then positions the highlight rectangle according to the information 
        /// </summary>
        private void Highlight_Text(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Rectangle rect = sender as Rectangle;
            FullSearchItem dataContext = rect.DataContext as FullSearchItem;
            // Only care when data context becomes non-null 
            if (dataContext == null)
                return;

            // Use a textblock to hold text to measure where highlight should be positioned 
            TextBlock textBlock = new TextBlock();
            string contextText = dataContext.ContextText;

            string leftText = contextText.Substring(0, contextText.IndexOf(dataContext.SearchText));
            textBlock.Text = leftText;
            // Trailing empty spaces won't count for width, so use a placeholder character
            if (textBlock.Text.EndsWith(" "))
            {
                textBlock.Text = textBlock.Text.Substring(0, textBlock.Text.Count() - 1) + "i";
            }
            textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            textBlock.Arrange(new Rect(new Point(0, 0), textBlock.DesiredSize));
            double leftOffset = textBlock.DesiredSize.Width;

            string rightText = leftText + dataContext.SearchText;
            if (textBlock.Text.EndsWith(" "))
            {
                textBlock.Text = textBlock.Text.Substring(0, textBlock.Text.Count() - 1) + "i";
            }
            textBlock.Text = rightText;
            textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            textBlock.Arrange(new Rect(new Point(0, 0), textBlock.DesiredSize));
            double rightOffset = textBlock.DesiredSize.Width;

            // Apply calculations to position highlight for text
            rect.Margin = new Thickness(leftOffset, 0, 0, 0);
            rect.Height = 17;
            rect.Width = rightOffset - leftOffset;
        }
    }
}
