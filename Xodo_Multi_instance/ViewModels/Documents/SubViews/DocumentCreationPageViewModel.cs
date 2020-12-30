using CompleteReader.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CompleteReader.ViewModels.Document.SubViews
{
    class DocumentCreationPageViewModel : ViewModelBase, INavigable
    {
        private static DocumentCreationPageViewModel _Current;
        public DocumentCreationPageViewModel()
        {
            _Current = this;
            NavigationCommand = new RelayCommand(NavigationCommandImpl);
            BrowseFilesCommand = new RelayCommand(BrowseFilesCommandImpl);
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
        }

        /// <summary>
        /// Gets the current instance of the DocumentCreationPageViewModel
        /// </summary>
        public static DocumentCreationPageViewModel Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new DocumentCreationPageViewModel();
                }
                return _Current;
            }
        }

        // Need this to satisfy INavigable, but don't need to use it.
        public event NewINavigableAvailableDelegate NewINavigableAvailable;

        public void Activate(object parameter)
        {

        }

        public void Deactivate(object parameter)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
        }

        public void Reset()
        {
            CurrentView = PageCreationViews.BlankDoc;
        }
        private void OnBackRequested(object sender, BackRequestedEventArgs backRequestedEventArgs)
        {
            DocumentViewModel.Current.SubViewSelectionCommand.Execute(Settings.Settings.MainPagePanel);
            backRequestedEventArgs.Handled = true;
        }

        public override bool GoBack()
        {
            if (CurrentView != PageCreationViews.Main)
            {
                CurrentView = PageCreationViews.Main;
                return true;
            }
            return false;
        }

        #region Events

        public delegate void NewDocumentCreatedDelegate(Windows.Storage.StorageFile file);

        public event NewDocumentCreatedDelegate NewDocumentCreated;

        #endregion Events

        #region Commands

        public RelayCommand NavigationCommand { get; private set; }
        public RelayCommand BrowseFilesCommand { get; private set; }

        private void NavigationCommandImpl(object nextViewStringAsObject)
        {
            string nextView = nextViewStringAsObject as string;
            if (!string.IsNullOrWhiteSpace(nextView))
            {
                if (nextView.Equals("BlankDoc", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentView = PageCreationViews.BlankDoc;
                }
                else if (nextView.Equals("ImageFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentView = PageCreationViews.ImageFromFile;
                }
                else if (nextView.Equals("ImageFromCamera", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentView = PageCreationViews.ImageFromCamera;
                }
                else
                {
                    DocumentViewModel.Current.SubViewSelectionCommand.Execute(Settings.Settings.MainPagePanel);
                }
            }
            else
            {
                DocumentViewModel.Current.SubViewSelectionCommand.Execute(Settings.Settings.MainPagePanel);
            }

        }

        private void BrowseFilesCommandImpl(object parameter)
        {
            DocumentViewModel.Current.BrowseFilesCommand.Execute(null);
        }


        #endregion Commands

        #region SubViews

        private DocumentsPage.BlankDocumentCreationViewModel _BlankDocumentCreationViewModel;
        public DocumentsPage.BlankDocumentCreationViewModel BlankDocumentCreationViewModel
        {
            get
            {
                if (_BlankDocumentCreationViewModel == null)
                {
                    _BlankDocumentCreationViewModel = new DocumentsPage.BlankDocumentCreationViewModel();
                    _BlankDocumentCreationViewModel.NewDocumentCreated += NewDocumentCreatedHandler;
                }
                return _BlankDocumentCreationViewModel;
            }
        }

        private DocumentsPage.ImageDocumentCreationViewModel _ImageDocumentCreationViewModel;
        public DocumentsPage.ImageDocumentCreationViewModel ImageDocumentCreationViewModel
        {
            get
            {
                if (_ImageDocumentCreationViewModel == null)
                {
                    _ImageDocumentCreationViewModel = new DocumentsPage.ImageDocumentCreationViewModel();
                    _ImageDocumentCreationViewModel.NewDocumentCreated += NewDocumentCreatedHandler;

                }
                return _ImageDocumentCreationViewModel;
            }
        }

        private DocumentsPage.ImageDocumentCreationViewModel _ImageDocumentCreationFromCameraViewModel;
        public DocumentsPage.ImageDocumentCreationViewModel ImageDocumentCreationFromCameraViewModel
        {
            get
            {
                if (_ImageDocumentCreationFromCameraViewModel == null)
                {
                    _ImageDocumentCreationFromCameraViewModel = new DocumentsPage.ImageDocumentCreationViewModel();
                    _ImageDocumentCreationFromCameraViewModel.NewDocumentCreated += NewDocumentCreatedHandler;
                }
                return _ImageDocumentCreationFromCameraViewModel;
            }
        }

        private void NewDocumentCreatedHandler(Windows.Storage.StorageFile file)
        {
            if (NewDocumentCreated != null)
            {
                NewDocumentCreated(file);
            }
        }

        #endregion SubViews

        #region Properties

        public enum PageCreationViews
        {
            Main,
            BlankDoc,
            ImageFromFile,
            ImageFromCamera,
        }

        private PageCreationViews _CurrentView = PageCreationViews.BlankDoc;
        public PageCreationViews CurrentView
        {
            get {return _CurrentView; }
            set
            {
                if (value != _CurrentView)
                {
                    _CurrentView = value;
                    bool delayPropertyChange = false;

                    switch (_CurrentView)
                    {
                        case PageCreationViews.ImageFromFile:
                            if (!ImageDocumentCreationViewModel.IsImageSelected)
                            {
                                ImageDocumentCreationViewModel.PickDocumentCommand.Execute(this);
                                delayPropertyChange = true;
                            }
                            break;
                        case PageCreationViews.ImageFromCamera:
                            if (!ImageDocumentCreationFromCameraViewModel.IsImageSelected)
                            {
                                ImageDocumentCreationFromCameraViewModel.GetPictureFromCameraCommand.Execute(this);
                                delayPropertyChange = true;
                            }
                            break;
                    }
                    if (delayPropertyChange)
                    {
                        DelayedCurrentViewPropertyChange();
                    }
                    else
                    {
                        RaisePropertyChanged();
                        RaisePropertyChanged("CurrentViewIndex");
                    }
                }
            }
        }

        private async void DelayedCurrentViewPropertyChange()
        {
            await Task.Delay(500);
            RaisePropertyChanged("CurrentView");
            RaisePropertyChanged("CurrentViewIndex");
        }

        // TODO Phone
        //private async void DelayedCurrentViewPropertyChange()
        //{
        //    RaisePropertyChanged("CurrentView");
        //    RaisePropertyChanged("CurrentViewIndex");
        //}


        public int CurrentViewIndex
        {
            get { return (int)CurrentView; }
        }

        #endregion Properties
    }
}
