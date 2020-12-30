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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CompleteReader.Documents.SubPages
{
    public sealed partial class CreateImageDocumentControl : UserControl
    {
        ViewModels.DocumentsPage.ImageDocumentCreationViewModel _ViewModel;

        public static readonly DependencyProperty IsForCameraProperty = DependencyProperty.Register(
          "IsForCamera",
          typeof(bool),
          typeof(CreateImageDocumentControl),
          new PropertyMetadata(false, new PropertyChangedCallback(OnIsForCameraChanged))
        );

        public bool IsForCamera
        {
            get
            {
                return (bool)GetValue(IsForCameraProperty);
            }
            set
            {
                SetValue(IsForCameraProperty, value);
                SetButtonVisibility((bool)GetValue(IsForCameraProperty));
            }
        }

        private static void OnIsForCameraChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CreateImageDocumentControl ctrl = d as CreateImageDocumentControl; //null checks omitted
            bool s = (bool)e.NewValue; //null checks omitted
            ctrl.SetButtonVisibility(s);
        }

        private void SetButtonVisibility(bool isCamera)
        {
            if (isCamera)
            {
                FromFileButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                FromCameraButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                _ViewModel = ViewModels.Document.SubViews.DocumentCreationPageViewModel.Current.ImageDocumentCreationFromCameraViewModel;
                this.DataContext = _ViewModel;
            }
            else
            {
                FromFileButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                FromCameraButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                _ViewModel = ViewModels.Document.SubViews.DocumentCreationPageViewModel.Current.ImageDocumentCreationViewModel;
                this.DataContext = _ViewModel;
            }
        }

        public CreateImageDocumentControl()
        {
            this.InitializeComponent();
        }
    }
}
