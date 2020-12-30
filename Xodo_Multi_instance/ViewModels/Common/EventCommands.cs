using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace CompleteReader.ViewModels.Common
{
    public static class PointerPressedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(PointerPressedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
                control.PointerPressed += control_PointerPressed;
        }

        static void control_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e.Pointer))
            {
                command.Execute(new Tuple<object, PointerRoutedEventArgs>(sender, e));

            }
        }
    }

    public static class PointerMovedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(PointerMovedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
                control.PointerMoved += control_PointerMoved;
        }

        static void control_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e.Pointer))
            {
                command.Execute(new Tuple<object, PointerRoutedEventArgs>(sender, e));
            }
        }
    }

    public static class TappedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(TappedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    control.Tapped += control_Tapped;
                }
                else
                {
                    control.Tapped -= control_Tapped;
                }
            }
        }

        static void control_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }

    public static class RightTappedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(RightTappedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    control.RightTapped += control_RightTapped;
                }
                else
                {
                    control.RightTapped -= control_RightTapped;
                }
            }
        }

        static void control_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }

    public static class HoldingCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(HoldingCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    control.Holding += control_Holding;
                }
                else
                {
                    control.Holding -= control_Holding;
                }
            }
        }

        static void control_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }

    public static class TextChangedCommand
    {
        public static System.Collections.Generic.Dictionary<DependencyObject, TextChangedEventHandler> _TextChangedHandlers =
            new System.Collections.Generic.Dictionary<DependencyObject, TextChangedEventHandler>();

        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(TextChangedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as TextBox;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    TextChangedEventHandler handler = new TextChangedEventHandler(control_TextChanged);
                    _TextChangedHandlers[d] = handler;
                    control.TextChanged += handler;
                }
                else
                {
                    if (_TextChangedHandlers.ContainsKey(d))
                    {
                        control.TextChanged -= _TextChangedHandlers[d];
                        _TextChangedHandlers.Remove(d);
                    }
                }
            }
        }

        private static void control_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox control = sender as TextBox;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(control.Text))
            {
                command.Execute(control.Text);
            }
        }
    }

    public static class PasswordChangedCommand
    {
        public static System.Collections.Generic.Dictionary<DependencyObject, RoutedEventHandler> _TextChangedHandlers =
            new System.Collections.Generic.Dictionary<DependencyObject, RoutedEventHandler>();

        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(PasswordChangedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as PasswordBox;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    RoutedEventHandler handler = new RoutedEventHandler(control_PasswordChanged);
                    _TextChangedHandlers[d] = handler;
                    control.PasswordChanged += handler;
                }
                else
                {
                    if (_TextChangedHandlers.ContainsKey(d))
                    {
                        control.PasswordChanged -= _TextChangedHandlers[d];
                        _TextChangedHandlers.Remove(d);
                    }
                }
            }
        }

        private static void control_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox control = sender as PasswordBox;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(control.Password))
            {
                command.Execute(control.Password);
            }
        }
    }

    public static class LoadedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(LoadedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
                control.Loaded += control_Loaded;
        }

        static void control_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(sender))
            {
                command.Execute(sender);
            }
        }
    }

    public static class AnimationCompletedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(AnimationCompletedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var storyBoard = d as Windows.UI.Xaml.Media.Animation.Storyboard;
            if (storyBoard != null)
                storyBoard.Completed += storyBoard_Completed;
        }

        private static void storyBoard_Completed(object sender, object e)
        {
            Windows.UI.Xaml.Media.Animation.Storyboard storyboard = sender as Windows.UI.Xaml.Media.Animation.Storyboard;
            var command = GetCommand(storyboard);

            if (command != null && command.CanExecute(sender))
            {
                command.Execute(sender);
            }
        }

        static void control_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(sender))
            {
                command.Execute(sender);
            }
        }
    }

    public static class ItemClickCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(ItemClickCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as ListViewBase;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    control.ItemClick += OnItemClick;
                }
                else
                {
                    control.ItemClick -= OnItemClick;
                }
            }
                
        }

        private static void OnItemClick(object sender, ItemClickEventArgs e)
        {
            var control = sender as ListViewBase;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e.ClickedItem))
                command.Execute(e.ClickedItem);
        }
    }

    public static class LostFocusCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(LostFocusCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
                control.LostFocus += control_LostFocus;
        }

        static void control_LostFocus(object sender, RoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(sender))
            {
                command.Execute(sender);
            }
        }
    }

    public static class KeyDownCommand
    {
        public static System.Collections.Generic.Dictionary<DependencyObject, KeyEventHandler> _KeyDownHandlers = 
            new System.Collections.Generic.Dictionary<DependencyObject, KeyEventHandler>();

        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(KeyDownCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    KeyEventHandler handler = new KeyEventHandler(control_KeyDown);
                    _KeyDownHandlers[d] = handler;
                    control.KeyDown += handler;
                }
                else
                {
                    if (_KeyDownHandlers.ContainsKey(d))
                    {
                        control.KeyDown -= _KeyDownHandlers[d];
                        _KeyDownHandlers.Remove(d);
                    }
                }
            }
        }

        static void control_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // handle in key up instead. Apparently a bug:
                // https://social.msdn.microsoft.com/Forums/windowsapps/en-US/734d6c7a-8da2-48c6-9b3d-fa868b4dfb1d/c-textbox-keydown-triggered-twice-in-metro-applications?forum=winappswithcsharp
                return;
            }

            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }

    public static class KeyUpCommand
    {
        public static System.Collections.Generic.Dictionary<DependencyObject, KeyEventHandler> _KeyUpHandlers = new System.Collections.Generic.Dictionary<DependencyObject, KeyEventHandler>();

        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(KeyUpCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = d as FrameworkElement;
            if (control != null)
            {
                if (e.NewValue != null)
                {
                    KeyEventHandler handler = new KeyEventHandler(control_KeyUp);
                    _KeyUpHandlers[d] = handler;
                    control.KeyUp += handler;
                }
                else
                {
                    if (_KeyUpHandlers.ContainsKey(d))
                    {
                        control.KeyUp -= _KeyUpHandlers[d];
                        _KeyUpHandlers.Remove(d);
                    }
                }
            }
                
        }

        static void control_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }

    public static class SelectionChangedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(SelectionChangedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ListViewBase control = d as ListViewBase;
            if (control != null)
            {
                control.SelectionChanged += control_SelectionChanged;
            }
        }

        static void control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FrameworkElement control = sender as FrameworkElement;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }

    public static class AppBarOpenedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(AppBarOpenedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            AppBar appBar = d as AppBar;
            if (appBar != null)
                appBar.Opened += appBar_Opened;
        }

        static void appBar_Opened(object sender, object e)
        {
            AppBar appBar = sender as AppBar;
            var command = GetCommand(appBar);

            if (command != null && command.CanExecute(sender))
            {
                command.Execute(sender);
            }
        }
    }

    public static class FlyoutOpenedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(FlyoutOpenedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            Flyout flyout = d as Flyout;
            if (flyout != null)
                flyout.Opened += flyout_Opened;
        }

        static void flyout_Opened(object sender, object e)
        {
            Flyout flyout = sender as Flyout;
            var command = GetCommand(flyout);
            if (command != null && command.CanExecute(sender))
            {
                command.Execute(sender);
            }
        }
    }

    public static class FlyoutClosedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(FlyoutClosedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            Flyout flyout = d as Flyout;
            if (flyout != null)
                flyout.Closed += flyout_Closed;
        }

        static void flyout_Closed(object sender, object e)
        {
            Flyout flyout = sender as Flyout;
            var command = GetCommand(flyout);
            if (command != null && command.CanExecute(sender))
            {
                command.Execute(sender);
            }
        }
    }

    public static class CheckedCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(CheckedCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            RadioButton radioButton = d as RadioButton;
            if (radioButton != null)
                radioButton.Checked += radioButton_Checked;
        }

        static void radioButton_Checked(object sender, object e)
        {
            RadioButton radioButton = sender as RadioButton;
            var command = GetCommand(radioButton);
            if (command != null && command.CanExecute(sender))
            {
                command.Execute(radioButton.Tag);
            }
        }
    }
}
