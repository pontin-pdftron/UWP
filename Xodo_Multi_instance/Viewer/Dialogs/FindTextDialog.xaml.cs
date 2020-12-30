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
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CompleteReader.Viewer.Dialogs
{
    public sealed partial class FindTextDialog : UserControl
    {
        public FindTextDialog()
        {
            this.InitializeComponent();
            DataContextChanged += FindTextDialog_DataContextChanged;
        }

        private void FindTextDialog_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            FindTextViewModel vm = args.NewValue as FindTextViewModel;
            if (vm != null)
            {
                vm.FocusRequested += VM_FocusRequested;
            }
        }

        private void VM_FocusRequested(object sender, object e)
        {
            SetFocus();
        }

        public bool DoesSearchBoxHaveFocus
        {
            get
            {
                return SearchTermTextBox.FocusState != FocusState.Unfocused;
            }
        }

        // Focus is awkward in the VM, so it's easier to wire it here
        public void SetFocus()
        {
            SearchTermTextBox.Focus(FocusState.Programmatic);
        }

        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            SearchTermTextBox.Focus(FocusState.Programmatic);
        }

        // Wrapping text box under appbar button doesn't allow spaces, so work around it.
        private void SearchTermTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            //TextBox textBox = sender as TextBox;
            //if (!String.IsNullOrWhiteSpace(textBox.Text) && e.Key == Windows.System.VirtualKey.Space)
            //{
            //    int selStart = textBox.SelectionStart;
            //    string preString = textBox.Text.Substring(0, selStart);
            //    string postString = textBox.Text.Substring(selStart + textBox.SelectionLength);
            //    textBox.Text = preString + " " + postString;
            //    textBox.SelectionStart = selStart + 1;
            //    textBox.SelectionLength = 0;
            //}
        }
    }
}
