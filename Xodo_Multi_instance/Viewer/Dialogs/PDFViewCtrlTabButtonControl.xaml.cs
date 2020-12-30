using CompleteReader.ViewModels.Viewer.Helpers;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CompleteReader.Viewer.Dialogs
{
    public sealed partial class PDFViewCtrlTabButtonControl : UserControl
    {
        public PDFViewCtrlTabButtonControl()
        {
            this.InitializeComponent();
            if (Utilities.UtilityFunctions.GetDeviceFormFactorType() == Utilities.UtilityFunctions.DeviceFormFactorType.Phone)
            {
                HorizontalItemsControl.ItemTemplate = Resources["PhoneTabItemTemplate"] as DataTemplate;
            }

            CopyPathButton.Click += CopyPathButton_Click;
        }

        private void CopyPathButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.ApplicationModel.DataTransfer.DataPackage dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            dataPackage.SetText(FlyoutPathTextBlock.Text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            FadeInAndOutCopyToClipboard.Begin();
        }

        private void TabButtonItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            CompleteReaderPDFViewCtrlTabInfo selectedItem = (sender as FrameworkElement).DataContext as CompleteReaderPDFViewCtrlTabInfo;

            if (selectedItem.IsFixedItem)
            {
                return;
            }


            DependencyObject item = HorizontalItemsControl.ContainerFromItem(selectedItem);
            Point pos = e.GetPosition(this);
            MarginTextBlock.Margin = new Thickness(pos.X, pos.Y, 0, 0);
            FlyoutFileInfo.ShowAt(MarginTextBlock);

            FlyoutTitleTextBlock.Text = selectedItem.MetaData.TabTitle;
            FlyoutPathTextBlock.Text = selectedItem.OriginalFile.Path;
            e.Handled = true;
        }

        private Storyboard GetFadeInAnimation(FrameworkElement target)
        {
            Storyboard sb = new Storyboard();
            String ID = target.Name.ToString();
            TimeSpan delayTime = TimeSpan.FromSeconds(0.5);
            TimeSpan fadeTime = TimeSpan.FromSeconds(0.2);
            DoubleAnimationUsingKeyFrames fade = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(delayTime + fadeTime),
                RepeatBehavior = new RepeatBehavior { Count = 1 },
                //EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut },
            };

            DiscreteDoubleKeyFrame startFrame = new DiscreteDoubleKeyFrame();
            startFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0));
            startFrame.Value = 0.0;
            fade.KeyFrames.Add(startFrame);

            DiscreteDoubleKeyFrame fadeStartFrame = new DiscreteDoubleKeyFrame();
            fadeStartFrame.KeyTime = KeyTime.FromTimeSpan(delayTime);
            fadeStartFrame.Value = 0.0;
            fade.KeyFrames.Add(fadeStartFrame);

            EasingDoubleKeyFrame fadeFrame = new EasingDoubleKeyFrame();
            fadeFrame.KeyTime = KeyTime.FromTimeSpan(delayTime + fadeTime);
            fadeFrame.Value = 1.0;
            QuadraticEase ease = new QuadraticEase();
            ease.EasingMode = EasingMode.EaseIn;
            fadeFrame.EasingFunction = ease;
            fade.KeyFrames.Add(fadeFrame);

            target.Resources.Add(ID + "FadInAnimation", sb);
            Storyboard.SetTarget(fade, target);
            Storyboard.SetTargetName(fade, target.Name);
            Storyboard.SetTargetProperty(fade, "(UIElement.Opacity)");
            sb.Children.Add(fade);

            return sb;
        }

        private Dictionary<ProgressRing, Storyboard> _AnimationsPlaying = new Dictionary<ProgressRing, Storyboard>();

        private void ProgressRing_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressRing ringy = sender as ProgressRing;
            if (ringy.Visibility == Visibility.Visible && !_AnimationsPlaying.ContainsKey(ringy))
            {
                Storyboard sb = GetFadeInAnimation(ringy);
                _AnimationsPlaying[ringy] = sb;
                sb.Begin();
            }
        }
    }
}
