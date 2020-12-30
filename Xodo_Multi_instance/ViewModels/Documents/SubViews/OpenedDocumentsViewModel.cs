using Common.Commands;
using CompleteReader.Settings;
using CompleteReader.ViewModels.Common;
using CompleteReader.ViewModels.Viewer.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static CompleteReader.ViewModels.Document.SubViews.FolderDocumentsViewModel;
using RelayCommand = Common.Commands.RelayCommand;

namespace CompleteReader.ViewModels.Document.SubViews
{
    public partial class OpenedDocumentsViewModel : ViewModelBase, INavigable
    {
        public event NewINavigableAvailableDelegate NewINavigableAvailable;
        public delegate void OpenedFileSelectedDelegate(StorageFile file);
        public event OpenedFileSelectedDelegate OpenedFileSelected;
        public delegate void IconViewChangedHandler(IconView status);
        public event IconViewChangedHandler IconViewChanged;

        private ObservableCollection<CompleteReaderPDFViewCtrlTabInfo> _openedDocuments;
        private DataTemplate _openedItemTemplate;
        private int _maximumRowsOrColumns = -1;
        private Style _openedItemContainerStyle;
        private IconView _currentIconView;

        public RelayCommand<ItemClickEventArgs> OpenedItemClickCommand { get; }

        public RelayCommand OpenedIconViewCommand { get; }

        public OpenedDocumentsViewModel()
        {
            OpenedItemClickCommand = new RelayCommand<ItemClickEventArgs>(OpenedItemClick);
            OpenedIconViewCommand = new RelayCommand(OpenedIconView);
        }

        public ObservableCollection<CompleteReaderPDFViewCtrlTabInfo> OpenedDocuments
        {
            get => _openedDocuments;
            set => Set(ref _openedDocuments, value);
        }

        public DataTemplate OpenedItemTemplate
        {
            get => _openedItemTemplate;
            set => Set(ref _openedItemTemplate, value);
        }

        public int MaximumRowsOrColumns
        {
            get => _maximumRowsOrColumns;
            set => Set(ref _maximumRowsOrColumns, value);
        }

        public Style OpenedItemContainerStyle
        {
            get => _openedItemContainerStyle;
            set => Set(ref _openedItemContainerStyle, value);
        }

        private bool _IsModal = false;
        public bool IsModal
        {
            get { return _IsModal; }
            set { Set(ref _IsModal, value); }
        }

        public IconView CurrentIconView
        {
            get => _currentIconView;
            set
            {
                if (Set(ref _currentIconView, value))
                {
                    SharedSettings.OpenedIconView = (int)_currentIconView;
                    IconViewChanged?.Invoke(_currentIconView);
                    RaisePropertyChanged(nameof(CurrVisibleIconView));
                }
            }
        }

        public string CurrVisibleIconView
        {
            get
            {
                switch (CurrentIconView)
                {
                    case IconView.Default:
                        return "";
                    case IconView.Small:
                        return "";
                    case IconView.List:
                        return "";
                    default:
                        return "";
                }
            }
        }

        public async Task InitializeAsync()
        {
            await CompleteReaderTabControlViewModel.GetInstanceAsync();
            OpenedDocuments = new ObservableCollection<CompleteReaderPDFViewCtrlTabInfo>(
                CompleteReaderTabControlViewModel.Instance.Tabs.Where(x => !string.IsNullOrEmpty(x.PreviewSource)).ToList());

            //CurrentIconView = (IconView)SharedSettings.OpenedIconView;
        }

        public async void Activate(object parameter)
        {
            // Method intentionally left empty.
            await InitializeAsync();
        }

        public void Deactivate(object parameter)
        {
            // Method intentionally left empty.
        }
    }
}
