using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompleteReader.Utilities
{
    /// <summary>
    /// This class makes sure that there's only ever 1 message dialog open at any given time.
    /// </summary>
    class MessageDialogHelper
    {
        private static Windows.Foundation.IAsyncOperation<Windows.UI.Popups.IUICommand> _LastMessageDialog;

        /// <summary>
        /// Returns true if the message dialog got dismissed through normal means, and false if it was canceled.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public static async Task<Windows.UI.Popups.IUICommand> ShowMessageDialogAsync(Windows.UI.Popups.MessageDialog md)
        {
            if (_LastMessageDialog != null)
            {
                _LastMessageDialog.Cancel();
            }

            Windows.UI.Popups.IUICommand clickedCommand = null;
            bool success = true;
            try
            {
                _LastMessageDialog = md.ShowAsync();
                clickedCommand = await _LastMessageDialog;
                success = clickedCommand != null;
            }
            catch (TaskCanceledException)
            {
                success = false;
            }
            catch (Exception)
            {
                success = false;
            }
            return clickedCommand;
        }

        public static void CancelLastMessageDialog()
        {
            if (_LastMessageDialog != null && _LastMessageDialog.Status == Windows.Foundation.AsyncStatus.Started)
            {
                _LastMessageDialog.Cancel();
            }
        }
    }
}
