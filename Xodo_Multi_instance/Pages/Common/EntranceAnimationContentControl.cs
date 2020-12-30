using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace CompleteReader.Pages.Common
{
    class EntranceAnimationContentControl : Windows.UI.Xaml.Controls.ContentControl
    {
        #region dependency property

        public EntranceAnimationContentControl()
        {
            this.HorizontalContentAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            this.VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
        }

        /// <summary>
        /// Bind this boolean to a property in your view model that indicates when this control and it's content should be shown.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
          "IsOpen",
          typeof(bool),
          typeof(EntranceAnimationContentControl),
          new PropertyMetadata(false, new PropertyChangedCallback(OnIsOpenChanged))
        );

        /// <summary>
        /// Bind this boolean to a property in your view model that indicates when this control and it's content should be shown.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return (bool)GetValue(IsOpenProperty);
            }
            set
            {
                SetValue(IsOpenProperty, value);
            }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EntranceAnimationContentControl ctrl = d as EntranceAnimationContentControl; //null checks omitted
            bool s = (bool)e.NewValue; //null checks omitted
            if (s) // opening
            {
                ctrl.Open();
            }
            else // closing
            {
                ctrl.Close();
            }
        }


        /// <summary>
        /// You can give this control an animation to play when the content is to be shown. You can bind to an element in 
        /// your XAML, by using EntranceAnimation="{Binding ElementName=[A named StoryBoard], Mode=TwoWay}"
        /// </summary>
        public static readonly DependencyProperty EntranceAnimationProperty = DependencyProperty.Register(
          "EntranceAnimation",
          typeof(Storyboard),
          typeof(EntranceAnimationContentControl),
          new PropertyMetadata(null)
        );


        /// <summary>
        /// You can give this control an animation to play when the content is to be shown. You can bind to an element in 
        /// your XAML, by using EntranceAnimation="{Binding ElementName=[A named StoryBoard], Mode=TwoWay}"
        /// </summary>
        public Storyboard EntranceAnimation
        {
            get
            {
                try
                {
                    object storyBoardObject = GetValue(EntranceAnimationProperty);

                    if (storyBoardObject != null && storyBoardObject is Storyboard)
                    {
                        return (Storyboard)storyBoardObject;
                    }
                    return null;
                }
                catch (InvalidCastException)
                {
                    System.Diagnostics.Debug.WriteLine("Entrance animation failed for: " + this.Name);
                }
                return null;
            }
            set
            {
                SetValue(EntranceAnimationProperty, value);
            }
        }

        /// <summary>
        /// You can give this control an animation to play when the content is to be hidden. You can bind to an element in 
        /// your XAML, by using ExitAnimation="{Binding ElementName=[A named StoryBoard], Mode=TwoWay}"
        /// </summary>
        public static readonly DependencyProperty ExitAnimationProperty = DependencyProperty.Register(
          "ExitAnimation",
          typeof(Storyboard),
          typeof(EntranceAnimationContentControl),
          new PropertyMetadata(null, OnExitAnimationChanged)
        );

        private static void OnExitAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EntranceAnimationContentControl ctrl = d as EntranceAnimationContentControl;
            Storyboard sb = e.NewValue as Storyboard;
            if (ctrl != null)
            {
                if (ctrl.ExitAnimation != null && ctrl._ExitCompletedHandler != null)
                {
                    ctrl.ExitAnimation.Completed -= ctrl._ExitCompletedHandler;
                    ctrl._ExitCompletedHandler = null;
                }
                if (sb != null)
                {
                    ctrl.ExitAnimation = sb;
                    ctrl._ExitCompletedHandler = new EventHandler<object>(ctrl.ExitAnimation_Completed);
                    ctrl.ExitAnimation.Completed += ctrl._ExitCompletedHandler;
                }
            }
        }

        private static void ExitAnimation_Completed1(object sender, object e)
        {
            throw new NotImplementedException();
        }

        public Storyboard ExitAnimation
        {
            get
            {
                try
                {
                    return (Storyboard)GetValue(ExitAnimationProperty);
                }
                catch (InvalidCastException)
                { }
                return null;
            }
            set
            {
                SetValue(ExitAnimationProperty, value);
            }
        }

        /// <summary>
        /// This value determines whether or not the visibility of the EntranceAnimationContentControl will be collapsed when
        /// the animation is finished. Defaults to true.
        /// </summary>
        public static readonly DependencyProperty CollapseWhenExitedProperty = DependencyProperty.Register(
            "CollapseWhenExited",
            typeof(bool),
            typeof(EntranceAnimationContentControl),
            new PropertyMetadata(true)
            );

        /// <summary>
        /// This value determines whether or not the visibility of the EntranceAnimationContentControl will be collapsed when
        /// the animation is finished. Defaults to true.
        /// </summary>
        public bool CollapseWhenExited
        {
            get 
            { 
                return (bool)GetValue(CollapseWhenExitedProperty); 
            }
            set
            {
                SetValue(CollapseWhenExitedProperty, value);
            }
        }

        /// <summary>
        /// Bind this boolean to a property in your view model that indicates when this control and it's content should be shown.
        /// </summary>
        public static readonly DependencyProperty DisableAnimationProperty = DependencyProperty.Register(
          "DisableAnimation",
          typeof(bool),
          typeof(EntranceAnimationContentControl),
          new PropertyMetadata(false)
        );

        /// <summary>
        /// Gets or sets whether to disable animations.
        /// </summary>
        public bool DisableAnimation
        {
            get
            {
                return (bool)GetValue(DisableAnimationProperty);
            }
            set
            {
                SetValue(DisableAnimationProperty, value);
            }
        }

        #endregion dependency property

        public delegate void ContentIsAvailableHandler(EntranceAnimationContentControl control);
        /// <summary>
        /// This event is raised when the entrance animation is finished. This lets you prepare the content by, for example, setting focus.
        /// </summary>
        public event ContentIsAvailableHandler ContentIsAvailable;

        private EventHandler<object> _EntranceCompletedHandler;
        private EventHandler<object> _ExitCompletedHandler;
        private bool _IsOpen = false;

        protected void Open()
        {
            _IsOpen = true;
            this.Visibility = Windows.UI.Xaml.Visibility.Visible;
            this.IsEnabled = true;
            if (EntranceAnimation != null)
            {
                if (_EntranceCompletedHandler == null)
                {
                    _EntranceCompletedHandler = new EventHandler<object>(EntranceAnimation_Completed);
                    EntranceAnimation.Completed += _EntranceCompletedHandler;
                }
                EntranceAnimation.Begin();
            }
            else
            {
                if (ContentIsAvailable != null)
                {
                    ContentIsAvailable(this);
                }
            }
        }

        protected void Close()
        {
            _IsOpen = false;
            if (ExitAnimation != null)
            {
                //if (_ExitCompletedHandler == null)
                //{
                //    _ExitCompletedHandler = new EventHandler<object>(ExitAnimation_Completed);
                //    ExitAnimation.Completed += _ExitCompletedHandler;
                //}
                ExitAnimation.Begin();
            }
            else
            {
                this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void EntranceAnimation_Completed(object sender, object e)
        {
            if (_IsOpen && ContentIsAvailable != null)
            {
                ContentIsAvailable(this);
            }
        }

        void ExitAnimation_Completed(object sender, object e)
        {
            if (!_IsOpen)
            {
                this.IsEnabled = false;
                if (CollapseWhenExited)
                {
                    this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
        }
    }
}
