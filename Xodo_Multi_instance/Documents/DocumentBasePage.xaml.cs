using CompleteReader.Utilities;
using CompleteReader.ViewModels.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CompleteReader.Documents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DocumentBasePage : CompleteReader.Pages.Common.NavigablePage
    {
        public DocumentBasePage()
        {
            this.InitializeComponent();
            this.ShellSplitView.Content = new Frame();
            this.DataContext = CompleteReader.ViewModels.Document.DocumentViewModel.Current;
            NavigateSubView(CompleteReader.ViewModels.Document.DocumentViewModel.Current.CurrentSubView);
            this.SizeChanged += DocumentBasePage_SizeChanged;
        }

        void DocumentBasePage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 700)
            {
                if (CompleteReader.ViewModels.Document.DocumentViewModel.Current.SubView == ViewModels.Document.DocumentViewModelBase.SubViewState.Settings ||
                    CompleteReader.ViewModels.Document.DocumentViewModel.Current.SubView == ViewModels.Document.DocumentViewModelBase.SubViewState.CreateDocument ||
                    CompleteReader.ViewModels.Document.DocumentViewModel.Current.SubView == ViewModels.Document.DocumentViewModelBase.SubViewState.ImageFromFile ||
                    CompleteReader.ViewModels.Document.DocumentViewModel.Current.SubView == ViewModels.Document.DocumentViewModelBase.SubViewState.ImageFromCamera)
                {
                    CompleteReader.ViewModels.Document.DocumentViewModel.Current.SplitViewDisplayMode = SplitViewDisplayMode.CompactInline;
                    // Don't open splitview if user intentionally closed it
                    if (e.PreviousSize.Width <= 700)
                    {
                        CompleteReader.ViewModels.Document.DocumentViewModel.Current.IsSplitViewOpen = true;
                    }
                }
                else
                {
                    CompleteReader.ViewModels.Document.DocumentViewModel.Current.SplitViewDisplayMode = SplitViewDisplayMode.CompactOverlay;
                }
            }
            else
            {
                CompleteReader.ViewModels.Document.DocumentViewModel.Current.SplitViewDisplayMode = SplitViewDisplayMode.Overlay;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        protected override void navigableViewModel_NewINavigableAvailable(ViewModels.Common.INavigable sender, ViewModels.Common.INavigable newNavigable)
        {
            if (newNavigable != null && newNavigable != sender)
            {
                if (newNavigable is CompleteReader.ViewModels.Viewer.ViewerViewModel)
                {
                    NavigateToViewer(newNavigable as CompleteReader.ViewModels.Viewer.ViewerViewModel);
                }
                else
                {
                    NavigateSubView(newNavigable);
                }
            }
        }

        private void NavigateSubView(ViewModels.Common.INavigable subView)
        {
            if (subView != null && ShellSplitView.Content != null)
            {
                if (subView is ViewModels.Document.SubViews.OpenedDocumentsViewModel)
                {
                    DocumentViewModel.Current.MainNavigationEnabled = true;
                    DocumentViewModel.Current.CreateNavigationEnabled = false;
                    DocumentViewModel.Current.SettingNavigationEnabled = false;
                    ((Frame)ShellSplitView.Content).Navigate(typeof(SubPages.OpenedItemsPage), subView);
                }

                if (subView is ViewModels.Document.SubViews.RecentDocumentsViewModel)
                {
                    ((Frame)ShellSplitView.Content).Navigate(typeof(SubPages.RecentItemsPage), subView);
                }
                else if (subView is ViewModels.Document.SubViews.FolderDocumentsViewModel)
                {
                    ((Frame)ShellSplitView.Content).Navigate(typeof(SubPages.FoldersPage), subView);
                }
                else if (subView is ViewModels.Document.SubViews.DocumentCreationPageViewModel)
                {
                    ((Frame)ShellSplitView.Content).Navigate(typeof(SubPages.DocumentCreationPage), subView);
                }
                else if (subView is Settings.SettingsViewModel)
                {
                    ((Frame)ShellSplitView.Content).Navigate(typeof(Settings.SettingsPage), subView);
                }
            }
        }

        private void NavigateToViewer(CompleteReader.ViewModels.Viewer.ViewerViewModel viewerVM)
        {
            // Deactivate the Document subpage View Model, and ensure OnNavigatedFrom gets called by navigating to a dummy page
            CompleteReader.ViewModels.Document.DocumentViewModel.Current.CurrentSubView = null;
            ((Frame)ShellSplitView.Content).Navigate(typeof(SubPages.StandbyPage));
            this.Frame.Navigate(typeof(CompleteReader.Viewer.ViewerPage), viewerVM);
        }


        // Code-behind work around for binding to isChecked not working for nested splitview radio buttons
        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            BlankDocButton.IsChecked = true;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            OptionsButton.IsChecked = true;
        }
    }
}
