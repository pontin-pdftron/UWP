using CompleteReader.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CompleteReader.ViewModels.Document.SubViews;
using System.Threading.Tasks;
using Windows.UI.Core;
using CompleteReader.Utilities;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CompleteReader.Documents.SubPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FoldersPage
    {
        private FolderDocumentsViewModel _ViewModel;
        private ItemsWrapGrid _ItemsWrapGrid;
        private ScrollViewer _LWScroller = null;
        private EventHandler<ScrollViewerViewChangedEventArgs> _LWViewChanged = null;

        public FoldersPage()
        {
            this.InitializeComponent();
            this.FolderNavigationView.SizeChanged += FolderNavigationView_SizeChanged;
        }

        private void FolderNavigationView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FolderNavigationViewScrollViewer.ScrollToHorizontalOffset(FolderNavigationViewScrollViewer.ExtentWidth);
        }

        private void FoldersPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ViewModel.UpdateUI();
            var loader = ResourceLoader.GetForCurrentView(); 
            if (e.NewSize.Width < Constants.PhoneWidthThreshold)
            {
                CommandBarContentGrid.Visibility = Visibility.Collapsed;
                UtilityFunctions.HideElement(CancelSelectionButton);
                UtilityFunctions.HideElement(AppBarSeparatorBar);
                if (_ViewModel.PinItem != null)
                {
                    _ViewModel.PinItem.DocumentName = loader.GetString("DocumentsPage_FoldersPage_SmallPinItem_Text");
                }
            }
            else
            {
                CommandBarContentGrid.Visibility = Visibility.Visible;
                UtilityFunctions.ShowElement(CancelSelectionButton, DeleteAppBarButton.Width);
                UtilityFunctions.ShowElement(AppBarSeparatorBar, DeleteAppBarButton.Width);
                if (_ViewModel.PinItem != null)
                {
                    _ViewModel.PinItem.DocumentName = loader.GetString("DocumentsPage_FoldersPage_PinItem_Text");
                }
            }
        }

        #region Icon View 

        /// <summary>
        ///  Using load event to save the ItemsWrapGrid for changing layout of the ListView, since there's no way to access it otherwise.
        /// </summary>
        private void _ViewModel_IconViewChanged(FolderDocumentsViewModel.IconView status)
        {
            ResolveIconView(status);
            IconViewAppBarButton.Flyout.Hide();
        }

        private void ResolveIconView(FolderDocumentsViewModel.IconView status)
        {
            if (_ItemsWrapGrid == null)
            {
                return;
            }

            switch (status)
            {
                case FolderDocumentsViewModel.IconView.Default:
                    FolderDocumentsView.Margin = new Thickness(0,30,0,0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = -1;
                    FolderDocumentsView.ItemTemplate = Resources["DefaultRecentListDataTemplate"] as DataTemplate;
                    Style style = new Style();
                    style.TargetType = typeof(ListViewItem);
                    style.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelection"] as Style;
                    FolderDocumentsView.ItemContainerStyle = style;
                    break;
                case FolderDocumentsViewModel.IconView.Small:
                    FolderDocumentsView.Margin = new Thickness(0,30,0,0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = -1;
                    FolderDocumentsView.ItemTemplate = Resources["SmallRecentListDataTemplate"] as DataTemplate;
                    Style style2 = new Style();
                    style2.TargetType = typeof(ListViewItem);
                    style2.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelectionSmall"] as Style;
                    FolderDocumentsView.ItemContainerStyle = style2;
                    break;
                case FolderDocumentsViewModel.IconView.List:
                    FolderDocumentsView.Margin = new Thickness(-15,30,-15,0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = 1;
                    FolderDocumentsView.ItemTemplate = Resources["ListRecentListDataTemplate"] as DataTemplate;
                    Style style3 = new Style();
                    style3.TargetType = typeof(ListViewItem);
                    style3.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelectionList"] as Style;
                    FolderDocumentsView.ItemContainerStyle = style3;
                    break;
                case FolderDocumentsViewModel.IconView.Cover:
                    FolderDocumentsView.Margin = new Thickness(0, 30, 0, 0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = -1;
                    FolderDocumentsView.ItemTemplate = Resources["CoverRecentListDataTemplate"] as DataTemplate;
                    Style style4 = new Style();
                    style4.TargetType = typeof(ListViewItem);
                    style4.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelection"] as Style;
                    FolderDocumentsView.ItemContainerStyle = style4;
                    break;
            }
        }

        private void ItemsWrapGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _ItemsWrapGrid = (ItemsWrapGrid)sender;
            var iconView = Constants.IsPhoneWidth() ? FolderDocumentsViewModel.IconView.List : (FolderDocumentsViewModel.IconView)Settings.Settings.FolderIconView;
            ResolveIconView(iconView);
        }

        #endregion Icon View


        #region Animating Thumbnails


        private void ViewModel_ThumbnailReceived(PinnedItem item, int index)
        {
            if (_ViewModel.FilteredPinnedItems.Contains(item))
            {
                if (index >= _ViewModel.FilteredPinnedItems.FirstVisibleIndex && index <= _ViewModel.FilteredPinnedItems.LastVisibleIndex)
                {
                    FrameworkElement container = (FrameworkElement)FolderDocumentsView.ContainerFromItem(item);
                    FrameworkElement imageHost = UtilityFunctions.FindVisualChildByName(container, "ImageHost");
                    if (imageHost != null)
                    {
                        imageHost.Opacity = 0;
                        Storyboard opacityAnim = OpacityAnimation(imageHost, index);
                        opacityAnim.Begin();
                    }
                }
            }
        }

        private Storyboard OpacityAnimation(FrameworkElement target, int index)
        {
            Storyboard sb = new Storyboard();
            string ID = "anim" + index + "OpacityAnimation";
            if (target.Resources.ContainsKey(ID))
            {
                return target.Resources[ID] as Storyboard;
            }
            double from = 0;
            double to = 1;
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                RepeatBehavior = new RepeatBehavior { Count = 1 },
                EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn },
            };

            target.Resources.Add(ID, sb);
            Storyboard.SetTarget(fadeIn, target);
            Storyboard.SetTargetName(fadeIn, target.Name);
            Storyboard.SetTargetProperty(fadeIn, "(UIElement.Opacity)");
            sb.Children.Add(fadeIn);

            return sb;
        }

        #endregion Animating Thumbnails

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is CompleteReader.ViewModels.Common.INavigable)
            {
                _ViewModel = e.Parameter as FolderDocumentsViewModel;
                this.DataContext = _ViewModel;
                _ViewModel.SelectedFileInfoFetched += ViewModel_SelectedFileInfoFetched;
                _ViewModel.UserKeyboardTyping += ViewModel_UserKeyboardTyping;
                _ViewModel.CurrentListViewBase = FolderDocumentsView;
                _ViewModel.IconViewChanged += _ViewModel_IconViewChanged;
                _ViewModel.ThumbnailReceived += ViewModel_ThumbnailReceived;
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_LWViewChanged != null)
            {
                _LWScroller.ViewChanged -= _LWViewChanged;
            }

            _ViewModel.SelectedFileInfoFetched -= ViewModel_SelectedFileInfoFetched;
            _ViewModel.UserKeyboardTyping -= ViewModel_UserKeyboardTyping;
            _ViewModel.IconViewChanged -= _ViewModel_IconViewChanged;
            _ViewModel.ThumbnailReceived -= ViewModel_ThumbnailReceived;

            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            base.OnNavigatingFrom(e);
        }

        private void ClearSelectionAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            FolderDocumentsView.SelectedItems.Clear(); 
        }


        #region File Info

        /// <summary>
        /// Once the ViewModel has fetched the File Info for the RecentItem, display it as a flyout. 
        /// </summary>
        private void ViewModel_SelectedFileInfoFetched(object sender, EventArgs e)
        {
            SelectedFileInfo fileInfo = _ViewModel.SelectedFileInfo;
            UIElementCollection stackPanels = FlyoutStackPanel.Children;

            // Flyout bindings don't seem to work. Set them programatically.

            // Title, author, pagecount is not relevant for a folder. Can't calculate filesize easily.
            if (!fileInfo.IsFolder)
            {
                ((stackPanels[0] as StackPanel).Children[1] as TextBlock).Text = fileInfo.Title;
                ((stackPanels[1] as StackPanel).Children[1] as TextBlock).Text = !String.IsNullOrEmpty(fileInfo.Author) ? fileInfo.Author : "N/A";
                ((stackPanels[2] as StackPanel).Children[1] as TextBlock).Text = fileInfo.PageCount;
                ((stackPanels[4] as StackPanel).Children[1] as TextBlock).Text = fileInfo.FileSize;
            }
            // Number of folders and PDFs is only applicable for storage folders
            else
            {
                ((stackPanels[5] as StackPanel).Children[1] as TextBlock).Text = fileInfo.NumFolders.ToString();
                ((stackPanels[6] as StackPanel).Children[1] as TextBlock).Text = fileInfo.NumPDFs.ToString();
            }

            // Collapse the visibility according to what is applicable to a folder or file
            (stackPanels[0] as StackPanel).Visibility = fileInfo.IsFolder ? Visibility.Collapsed : Visibility.Visible;
            (stackPanels[1] as StackPanel).Visibility = fileInfo.IsFolder ? Visibility.Collapsed : Visibility.Visible;
            (stackPanels[2] as StackPanel).Visibility = fileInfo.IsFolder ? Visibility.Collapsed : Visibility.Visible;
            (stackPanels[4] as StackPanel).Visibility = fileInfo.IsFolder ? Visibility.Collapsed : Visibility.Visible;
            (stackPanels[5] as StackPanel).Visibility = fileInfo.IsFolder ? Visibility.Visible : Visibility.Collapsed;
            (stackPanels[6] as StackPanel).Visibility = fileInfo.IsFolder ? Visibility.Visible : Visibility.Collapsed;

            // Both folders and files will show path and last modified info
            ((stackPanels[3] as StackPanel).Children[1] as TextBlock).Text = fileInfo.Path;
            ((stackPanels[7] as StackPanel).Children[1] as TextBlock).Text = fileInfo.LastModified;

            ListViewItem item = FolderDocumentsView.ContainerFromItem(sender) as ListViewItem;
            FlyoutFileInfo.ShowAt(item);

            // Align the flyout 
            var ttv = item.TransformToVisual(this);
            Point screenCoords = ttv.TransformPoint(new Point(0, 0));
            double centerX = _ViewModel.CurrentIconView != FolderDocumentsViewModel.IconView.List ? item.ActualWidth / 2 : 150;
            double centerY = item.ActualHeight / 2;
            MarginTextBlock.Margin = new Thickness(screenCoords.X + centerX, screenCoords.Y + centerY, 0, 0);

            FlyoutFileInfo.ShowAt(MarginTextBlock);
        }

        /// <summary>
        /// Show the flyout options when a FolderItem is right tapped
        /// </summary>
        private async void FolderItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            PinnedItem selectedItem = (sender as FrameworkElement).DataContext as PinnedItem;

            if (selectedItem.PinnedItemType == PinnedItem.PinnedType.Pin)
                return;

            // Since non-root folders only have one option, just use it without showing the flyout
            if (selectedItem.IsFolder && !_ViewModel.IsRootPath)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    _ViewModel.FileInfoCommand.Execute(selectedItem);
                });
                return;
            }

            ListViewItem item = FolderDocumentsView.ContainerFromItem(selectedItem) as ListViewItem;
            Point pos = e.GetPosition(this);
            MarginTextBlock.Margin = new Thickness(pos.X, pos.Y, 0, 0);
            FlyoutOptions.ShowAt(MarginTextBlock);

            IList<MenuFlyoutItemBase> buttons = FlyoutOptions.Items;

            // Need to add CommandParameter programatically since the MenuFlyout is is not part of the FolderItem 

            // Unpin
            (buttons[0] as MenuFlyoutItem).CommandParameter = selectedItem;
            (buttons[0] as MenuFlyoutItem).Visibility = _ViewModel.IsRootPath ? Visibility.Visible : Visibility.Collapsed;

            // Share
            (buttons[1] as MenuFlyoutItem).CommandParameter = selectedItem;
            (buttons[1] as MenuFlyoutItem).Visibility = selectedItem.PinnedItemType == PinnedItem.PinnedType.File ? Visibility.Visible : Visibility.Collapsed;

            // File Info
            (buttons[2] as MenuFlyoutItem).CommandParameter = selectedItem;
            (buttons[2] as MenuFlyoutItem).Visibility = selectedItem.PinnedItemType != PinnedItem.PinnedType.Pin ? Visibility.Visible : Visibility.Collapsed;
        }

#endregion File Info


#region Keyboard Typing Navigation

        /// <summary>
        /// Once the ViewModel has parsed the pressed key, scroll into the matched item
        /// </summary>
        private void ViewModel_UserKeyboardTyping(PinnedItem item)
        {
            if (_ViewModel.IsModal)
                return;

            FolderDocumentsView.ScrollIntoView(item);
            FocusBox.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// This event is used to detect user keyboard preses without needing to be focused on any UI Element
        /// </summary>
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (_ViewModel == null || _ViewModel.IsModal || FolderDocumentsView.Items.Count == 0)
                return;

            Windows.Devices.Input.TouchCapabilities touchPresent = new Windows.Devices.Input.TouchCapabilities();
            if (touchPresent.TouchPresent == 1)
                return;

            // Allow the TextBox to be focused so that it can capture the keystroke as well in a succinct manner
            // without need to parse shift, num keys, etc 
            FocusBox.Focus(FocusState.Programmatic);

            // Handle certain keys through the view model
            if (args.VirtualKey == Windows.System.VirtualKey.Enter || args.VirtualKey == Windows.System.VirtualKey.Left 
                || args.VirtualKey == Windows.System.VirtualKey.Right  || args.VirtualKey == Windows.System.VirtualKey.Back
                || args.VirtualKey == Windows.System.VirtualKey.Up || args.VirtualKey == Windows.System.VirtualKey.Down)
            {
                if (_ViewModel.ParseHotKeyPress(args))
                    return;
            }

            // Only want to handle down and up at this point
            if (args.VirtualKey != Windows.System.VirtualKey.Down && args.VirtualKey != Windows.System.VirtualKey.Up)
                return;
            
            // At this point, down and up need to be handled through the view first to calculate the number of items per row
            bool isDown = args.VirtualKey == Windows.System.VirtualKey.Down ? true : false;

            FolderDocumentsViewModel.IconView iconView = (FolderDocumentsViewModel.IconView)Settings.Settings.FolderIconView;

            // For list, simply move up/down by 1
            if (iconView == FolderDocumentsViewModel.IconView.List)
            {
                _ViewModel.MoveMatchedItemByRow((int)(1), isDown);
            }
            // For other view modes, calculate number of items per row and move up/down by that amount
            else
            {
                PinnedItem defaultPinItem = (FolderDocumentsView.Items[0] as PinnedItem);
                ListViewItem defaultListViewItem = FolderDocumentsView.ContainerFromItem(defaultPinItem) as ListViewItem;
                if (defaultPinItem != null && defaultListViewItem != null)
                {
                    _ViewModel.MoveMatchedItemByRow((int)(FolderDocumentsView.ActualWidth / defaultListViewItem.DesiredSize.Width), isDown);
                }
            }
        }

        /// <summary>
        /// Uses a textbox that captures text input to parse in the ViewModel
        /// </summary>
        private void FocusBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(FocusBox.Text))
                return;

            Windows.Devices.Input.TouchCapabilities touchPresent = new Windows.Devices.Input.TouchCapabilities();
            // Only allow desktops with keyboards to use this search feature
            if (touchPresent.TouchPresent == 1)
                return;

            // Parse the pressed key, and reset the textbox
            _ViewModel.ParseKeyPress(FocusBox.Text);
            FocusBox.Text = "";
        }

#endregion Keyboard Typing Navigation
    }
}
