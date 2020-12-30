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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

// TODO: Flyout binding not working, need to check and try to change it from code-behind to MVVM

namespace CompleteReader.Documents.SubPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecentItemsPage 
    {
        private RecentDocumentsViewModel _ViewModel;
        private ItemsWrapGrid _ItemsWrapGrid;

        public RecentItemsPage()
        {
            this.InitializeComponent();
            this.SizeChanged += RecentItemsPage_SizeChanged;
        }

        private void RecentItemsPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ViewModel.UpdateUI();
            if (e.NewSize.Width < Constants.PhoneWidthThreshold)
            {
                CommandBarContentGrid.Visibility = Visibility.Collapsed;
                UtilityFunctions.HideElement(CancelSelectionButton);
                UtilityFunctions.HideElement(AppBarSeparatorBar);
            }
            else
            {
                CommandBarContentGrid.Visibility = Visibility.Visible;
                UtilityFunctions.ShowElement(CancelSelectionButton, DeleteAppBarButton.Width);
                UtilityFunctions.ShowElement(AppBarSeparatorBar, DeleteAppBarButton.Width);
            }
        }

        #region Icon View 

        private void ViewModel_IconViewChanged(RecentDocumentsViewModel.IconView status)
        {
            ResolveIconView(status);
            IconViewAppBarButton.Flyout.Hide();
        }

        private void ResolveIconView(RecentDocumentsViewModel.IconView status)
        {
            if (_ItemsWrapGrid == null)
            {
                return;
            }

            switch (status)
            {
                case RecentDocumentsViewModel.IconView.Default:
                    RecentDocumentsView.Margin = new Thickness(0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = -1;
                    RecentDocumentsView.ItemTemplate = Resources["DefaultRecentListDataTemplate"] as DataTemplate;
                    Style style = new Style();
                    style.TargetType = typeof(ListViewItem);
                    style.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelection"] as Style;
                    RecentDocumentsView.ItemContainerStyle = style;
                    break;
                case RecentDocumentsViewModel.IconView.Small:
                    RecentDocumentsView.Margin = new Thickness(0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = -1;
                    RecentDocumentsView.ItemTemplate = Resources["SmallRecentListDataTemplate"] as DataTemplate;
                    Style style2 = new Style();
                    style2.TargetType = typeof(ListViewItem);
                    style2.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelectionSmall"] as Style;
                    RecentDocumentsView.ItemContainerStyle = style2;
                    break;
                case RecentDocumentsViewModel.IconView.List:
                    RecentDocumentsView.Margin = new Thickness(-15, 0, -15, 0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = 1;
                    RecentDocumentsView.ItemTemplate = Resources["ListRecentListDataTemplate"] as DataTemplate;
                    Style style3 = new Style();
                    style3.TargetType = typeof(ListViewItem);
                    style3.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelectionList"] as Style;
                    RecentDocumentsView.ItemContainerStyle = style3;
                    break;
                case RecentDocumentsViewModel.IconView.Cover:
                    RecentDocumentsView.Margin = new Thickness(0);
                    _ItemsWrapGrid.MaximumRowsOrColumns = -1;
                    RecentDocumentsView.ItemTemplate = Resources["CoverRecentListDataTemplate"] as DataTemplate;
                    Style style4 = new Style();
                    style4.TargetType = typeof(ListViewItem);
                    style4.BasedOn = App.Current.Resources["ListViewItemWithBorderBrushSelection"] as Style;
                    RecentDocumentsView.ItemContainerStyle = style4;
                    break;
            }
        }

        /// <summary>
        ///  Using load event to save the ItemsWrapGrid for changing layout of the ListView, since there's no way to access it otherwise.
        /// </summary>
        private void ItemsWrapGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _ItemsWrapGrid = (ItemsWrapGrid)sender;
            var iconView = Constants.IsPhoneWidth() ? RecentDocumentsViewModel.IconView.List : (RecentDocumentsViewModel.IconView)Settings.Settings.RecentIconView;
            ResolveIconView(iconView);
        }

        #endregion Icon View

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is CompleteReader.ViewModels.Common.INavigable)
            {
                _ViewModel = e.Parameter as RecentDocumentsViewModel;
                this.DataContext = _ViewModel;
                _ViewModel.SelectedFileInfoFetched += RecentItemsPage_SelectedFileInfoFetched;
                _ViewModel.UserKeyboardTyping += ViewModel_UserKeyboardTyping;
                _ViewModel.IconViewChanged += ViewModel_IconViewChanged;
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            }
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;

            base.OnNavigatingFrom(e);
        }

        private void ClearSelectionAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            RecentDocumentsView.SelectedItems.Clear();
        }

        #region File Info

        /// <summary>
        /// Once the ViewModel has fetched the File Info for the RecentItem, display it as a flyout. 
        /// </summary>
        private void RecentItemsPage_SelectedFileInfoFetched(object sender, EventArgs e)
        {
            SelectedFileInfo fileInfo = (this.DataContext as RecentDocumentsViewModel).SelectedFileInfo;
            UIElementCollection stackPanels = FlyoutStackPanel.Children;

            // Flyout bindings don't seem to work. Set them programatically.
            ((stackPanels[0] as StackPanel).Children[1] as TextBlock).Text = fileInfo.Title;
            ((stackPanels[1] as StackPanel).Children[1] as TextBlock).Text = !String.IsNullOrEmpty(fileInfo.Author) ? fileInfo.Author : "N/A";
            ((stackPanels[2] as StackPanel).Children[1] as TextBlock).Text = fileInfo.PageCount;
            ((stackPanels[3] as StackPanel).Children[1] as TextBlock).Text = fileInfo.Path;
            ((stackPanels[4] as StackPanel).Children[1] as TextBlock).Text = fileInfo.FileSize;
            ((stackPanels[5] as StackPanel).Children[1] as TextBlock).Text = fileInfo.LastModified;

            ListViewItem item = RecentDocumentsView.ContainerFromItem(sender) as ListViewItem;

            // Align the flyout 
            var ttv = item.TransformToVisual(this);
            Point screenCoords = ttv.TransformPoint(new Point(0, 0));
            double centerX = _ViewModel.CurrentIconView != RecentDocumentsViewModel.IconView.List ? item.ActualWidth / 2 : 150;
            double centerY = item.ActualHeight / 2;
            MarginTextBlock.Margin = new Thickness(screenCoords.X + centerX,  screenCoords.Y + centerY, 0, 0);

            FlyoutFileInfo.ShowAt(MarginTextBlock);
        }

        /// <summary>
        /// Show the flyout options when a RecentItem is right tapped
        /// </summary>
        private void RecentItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            RecentItem selectedItem = (sender as FrameworkElement).DataContext as RecentItem;

            ListViewItem item = RecentDocumentsView.ContainerFromItem(selectedItem) as ListViewItem;
            var pos = e.GetPosition(this);
            MarginTextBlock.Margin = new Thickness(pos.X, pos.Y, 0, 0);
            FlyoutOptions.ShowAt(MarginTextBlock);

            IList<MenuFlyoutItemBase> buttons = FlyoutOptions.Items;
                        
            // Need to add CommandParameter programatically since the MenuFlyout is is not part of the RecentItem 
            (buttons[0] as MenuFlyoutItem).CommandParameter = selectedItem;
            (buttons[1] as MenuFlyoutItem).CommandParameter = selectedItem;
            (buttons[2] as MenuFlyoutItem).CommandParameter = selectedItem;
        }

        #endregion File Info

        #region Keyboard Typing Navigation

        /// <summary>
        /// Once the ViewModel has parsed the pressed key, scroll into the matched item
        /// </summary>
        private void ViewModel_UserKeyboardTyping(RecentItem item)
        {
            if (_ViewModel.IsModal)
                return;

            RecentDocumentsView.ScrollIntoView(item);
            FocusBox.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// This event is used to detect user keyboard preses without needing to be focused on any UI Element
        /// </summary>
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (_ViewModel == null || _ViewModel.IsModal || RecentDocumentsView.Items.Count == 0)
                return;

            Windows.Devices.Input.TouchCapabilities touchPresent = new Windows.Devices.Input.TouchCapabilities();
            if (touchPresent.TouchPresent == 1)
                return;

            // Allow the TextBox to be focused so that it can capture the keystroke as well in a succinct manner
            // without need to parse shift, num keys, etc 
            FocusBox.Focus(FocusState.Programmatic);

            // Handle certain keys through the view model
            if (args.VirtualKey == Windows.System.VirtualKey.Enter || args.VirtualKey == Windows.System.VirtualKey.Left
                || args.VirtualKey == Windows.System.VirtualKey.Right)
            {
                _ViewModel.ParseHotKeyPress(args);
            }

            // Only want to handle down and up at this point
            if (args.VirtualKey != Windows.System.VirtualKey.Down && args.VirtualKey != Windows.System.VirtualKey.Up)
                return;

            // At this point, down and up need to be handled through the view first to calculate the number of items per row
            bool isDown = args.VirtualKey == Windows.System.VirtualKey.Down ? true : false;

            RecentDocumentsViewModel.IconView iconView = (RecentDocumentsViewModel.IconView)Settings.Settings.RecentIconView;

            // For list, simply move up/down by 1
            if (iconView == RecentDocumentsViewModel.IconView.List)
            {
                _ViewModel.MoveMatchedItemByRow((int)(1), isDown);
            }
            // For other view modes, calculate number of items per row and move up/down by that amount
            else
            {
                RecentItem defaultRecentItem = (RecentDocumentsView.Items[0] as RecentItem);
                ListViewItem defaultListViewItem = RecentDocumentsView.ContainerFromItem(defaultRecentItem) as ListViewItem;
                if (defaultRecentItem != null && defaultListViewItem != null)
                {
                    _ViewModel.MoveMatchedItemByRow((int)(RecentDocumentsView.ActualWidth / defaultListViewItem.DesiredSize.Width), isDown);
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
