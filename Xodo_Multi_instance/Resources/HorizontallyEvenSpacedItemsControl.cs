using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CompleteReader.Resources
{
    /// <summary>
    /// This class should be used with a StackPanel as the ItemTemplate.
    /// The StackPanel should have a horizontal orientation.
    /// E.g.
    /// <local:HorizontallyEvenSpacedItemsControl.ItemsPanel>
    ///     <ItemsPanelTemplate>
    ///         <StackPanel Orientation="Horizontal"/>
    ///     </ItemsPanelTemplate>
    /// </local:HorizontallyEvenSpacedItemsControl.ItemsPanel>
    /// 
    /// Note that there is no limit to how small an item can be.
    /// </summary>
    public class HorizontallyEvenSpacedItemsControl : ItemsControl
    {
        public HorizontallyEvenSpacedItemsControl()
        {
            this.SizeChanged += HorizontallyEvenSpacedItemsControl_SizeChanged;
        }

        // Workaround to reschedule attempting to measure items if the UI tree isn't ready
        private bool _AwaitingDispatcher = false;
        private DateTime? _FirstDispatchTime = null;
        private int _TimeoutDurationMS = 30000;

        private double _LastResizeWidth = 0;
        private int _LastResizeItemCount = 0;
        private double _LastUsedMaxItemWidth = 0;

        private double _ActualWidth = 0;

        private double _MaxItemWidth = 0;
        public double MaxItemWidth
        {
            get { return _MaxItemWidth; }
            set
            {
                if (value != _MaxItemWidth)
                {
                    _MaxItemWidth = value;
                    ResizeIfNecessary(false);
                }
            }
        }

        public static readonly DependencyProperty MaxItemWidthProperty =
        DependencyProperty.RegisterAttached("MaxItemWidth", typeof(double), typeof(HorizontallyEvenSpacedItemsControl),
        new PropertyMetadata(0));

        void HorizontallyEvenSpacedItemsControl_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            _ActualWidth = e.NewSize.Width;
            ResizeIfNecessary(false);
        }

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
            ResizeIfNecessary(true);
        }

        private bool _IsLastItemFixedSize = false;
        public bool IsLastItemFixedSize
        {
            get { return _IsLastItemFixedSize; }
            set
            {
                if (value != _IsLastItemFixedSize)
                {
                    _IsLastItemFixedSize = value;
                    ResizeIfNecessary(true);
                }
            }
        }

        public static readonly DependencyProperty IsLastItemFixedSizeProperty =
        DependencyProperty.RegisterAttached("IsLastItemFixedSize", typeof(bool), typeof(HorizontallyEvenSpacedItemsControl),
        new PropertyMetadata(false, OnPropChanged));

        private static void OnPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HorizontallyEvenSpacedItemsControl control = d as HorizontallyEvenSpacedItemsControl;
            control.IsLastItemFixedSize = (bool)e.NewValue;
        }

        private void ResizeIfNecessary(bool itemChange)
        {
            if (!itemChange && _LastResizeWidth == _ActualWidth && _LastUsedMaxItemWidth == MaxItemWidth)
            {
                return;
            }

            _LastResizeWidth = _ActualWidth;
            _LastResizeItemCount = this.Items.Count;
            _LastUsedMaxItemWidth = MaxItemWidth;
            double availableWidth = _LastResizeWidth;
            int itemsToSize = _LastResizeItemCount;

            if (IsLastItemFixedSize && this.Items.Count > 0)
            {
                ContentPresenter container = this.ContainerFromItem(this.Items[this.Items.Count - 1]) as ContentPresenter;
                if (container != null)
                {
                    container.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    double lastItemWidth = container.DesiredSize.Width;
                    availableWidth = _ActualWidth - lastItemWidth;
                    itemsToSize--;
                } 
                else
                {
                    RescheduleMeasurement();
                }
            }

            double newWidth = (availableWidth) / itemsToSize;
            if (_LastUsedMaxItemWidth > 0 && newWidth > _LastUsedMaxItemWidth)
            {
                newWidth = _LastUsedMaxItemWidth;
            }

            double nextWidth = newWidth;
            foreach (object item in this.Items)
            {
                ContentPresenter container = this.ContainerFromItem(item) as ContentPresenter;
                if (container != null)
                {

                    if (IsLastItemFixedSize && item == this.Items[Items.Count - 1])
                    {
                        container.Width = double.NaN;
                    }
                    else
                    {
                        container.Width = nextWidth;
                    }

                }
                else if (!_AwaitingDispatcher)
                {
                    RescheduleMeasurement();
                }
            }
        }

        private async void RescheduleMeasurement()
        {
            // The container was null because the UI tree wasn't ready. So we can't measure the items at this point
            // Therefore, reschedule it with the dispatcher. We stop doing this after 30 seconds, 
            // just in case container was null for some other reason.
            bool allowedDispatch = true;
            if (_FirstDispatchTime == null)
            {
                _FirstDispatchTime = DateTime.Now;
            }
            else
            {
                DateTime now = DateTime.Now;
                TimeSpan duration = (now - _FirstDispatchTime).Value;
                if (duration.TotalMilliseconds > _TimeoutDurationMS)
                {
                    allowedDispatch = false;
                }
            }
            if (allowedDispatch)
            {
                _AwaitingDispatcher = true;

                try
                {
                    await Task.Delay(50);
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        _AwaitingDispatcher = false;
                        ResizeIfNecessary(true);
                    });
                }
                catch (Exception) { }
            }


        }
    }
}
