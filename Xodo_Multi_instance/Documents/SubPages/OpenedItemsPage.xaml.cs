using CompleteReader.ViewModels.Document.SubViews;
using System;
using System.Collections.Generic;
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
using static CompleteReader.ViewModels.Document.SubViews.FolderDocumentsViewModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CompleteReader.Documents.SubPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OpenedItemsPage : Page
    {
        private OpenedDocumentsViewModel _viewModel;

        public OpenedDocumentsViewModel ViewModel => _viewModel;

        public OpenedItemsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is OpenedDocumentsViewModel viewModel)
            {
                _viewModel = viewModel;
                DataContext = _viewModel;

                _viewModel.IconViewChanged += ViewModel_IconViewChanged;
            }

            base.OnNavigatedTo(e);
        }

        private void ViewModel_IconViewChanged(IconView status)
        {
            ResolveIconView();
        }

        private void ResolveIconView()
        {
            switch (_viewModel.CurrentIconView)
            {
                case IconView.Default:
                    _viewModel.MaximumRowsOrColumns = -1;
                    _viewModel.OpenedItemTemplate = Resources["DefaultOpenedDocsDataTemplate"] as DataTemplate;
                    _viewModel.OpenedItemContainerStyle = App.Current.Resources["ListViewItemWithBorderBrushSelection"] as Style;
                    break;
                case IconView.Small:
                    _viewModel.MaximumRowsOrColumns = -1;
                    _viewModel.OpenedItemTemplate = Resources["SmallOpenedDocsDataTemplate"] as DataTemplate;
                    _viewModel.OpenedItemContainerStyle = App.Current.Resources["ListViewItemWithBorderBrushSelectionSmall"] as Style;
                    break;
                case IconView.List:
                    _viewModel.MaximumRowsOrColumns = 1;
                    _viewModel.OpenedItemTemplate = Resources["ListOpenedDocsDataTemplate"] as DataTemplate;
                    _viewModel.OpenedItemContainerStyle = App.Current.Resources["ListViewItemWithBorderBrushSelectionList"] as Style;
                    break;
                case IconView.Cover:
                    _viewModel.MaximumRowsOrColumns = -1;
                    _viewModel.OpenedItemTemplate = Resources["CoverOpenedDocsDataTemplate"] as DataTemplate;
                    _viewModel.OpenedItemContainerStyle = App.Current.Resources["ListViewItemWithBorderBrushSelection"] as Style;
                    break;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ResolveIconView();
        }
    }
}
