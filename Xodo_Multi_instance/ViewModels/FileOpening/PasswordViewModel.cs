using CompleteReader.Utilities;
using CompleteReader.ViewModels.Common;
using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;

namespace CompleteReader.ViewModels.FileOpening
{
    public class PasswordViewModel : CompleteReader.ViewModels.Common.ViewModelBase
    {
        public PasswordViewModel(PDFDoc doc)
        {
            _Doc = doc;
            InitCommands();
            HandlePassword();
        }

        public delegate void PasswordHandledDelegate(bool success, string password);
        public event PasswordHandledDelegate PasswordHandled;


        #region Commands

        private void InitCommands()
        {
            PasswordKeyDownCommand = new RelayCommand(PasswordKeyDownCommandImpl);
            PasswordKeyUpCommand = new RelayCommand(PasswordKeyUpCommandImpl);
            PasswordChangedCommand = new RelayCommand(PasswordChangedCommandImpl);
            PasswordOkPressedCommand = new RelayCommand(PasswordOkPressedCommandImpl);
            PasswordCancelPressedCommand = new RelayCommand(PasswordCancelPressedCommandImpl);
        }

        public RelayCommand PasswordKeyDownCommand { get; private set; }
        public RelayCommand PasswordKeyUpCommand { get; private set; }
        public RelayCommand PasswordChangedCommand { get; private set; }
        public RelayCommand PasswordOkPressedCommand { get; private set; }
        public RelayCommand PasswordCancelPressedCommand { get; private set; }

        private void PasswordKeyDownCommandImpl(object keyArgs)
        {
            KeyRoutedEventArgs args = keyArgs as KeyRoutedEventArgs;
            if (args != null)
            {
                PasswordKeyDown(args.Key);
            }
        }

        private void PasswordKeyUpCommandImpl(object keyArgs)
        {
            KeyRoutedEventArgs args = keyArgs as KeyRoutedEventArgs;
            if (args != null && args.Key == Windows.System.VirtualKey.Enter)
            {
                PasswordKeyDown(args.Key);
            }
        }

        private void PasswordChangedCommandImpl(object newPassword)
        {
            string password = newPassword as string;
            if (password != null)
            {
                PasswordChanged(password);
            }
        }

        private void PasswordOkPressedCommandImpl(object sender)
        {
            VerifyPassword();
        }

        private void PasswordCancelPressedCommandImpl(object sender)
        {
            CancelPassword();
        }

        #endregion Commands


        private const int DEFAULT_ATTEMPTS_AT_PASSWORD = 3;
        private int _AttemptsAtPassword = 0;
        private PDFDoc _Doc;

        private string _CurrentPassword = string.Empty;
        public string CurrentPassword
        {
            get { return _CurrentPassword; }
            set
            {
                if (Set(ref _CurrentPassword, value))
                {
                    RaisePropertyChanged("HasPasswordBoxGotContent");
                    IsIncorrectPasswordNotificationVisible = false;
                }
            }
        }

        private bool _IsIncorrectPasswordNotificationVisible = false;
        public bool IsIncorrectPasswordNotificationVisible
        {
            get { return _IsIncorrectPasswordNotificationVisible; }
            set
            {
                Set(ref _IsIncorrectPasswordNotificationVisible, value);
                RaisePropertyChanged("HasPasswordBoxGotContent");
            }
        }

        public bool HasPasswordBoxGotContent
        {
            get { return !string.IsNullOrEmpty(CurrentPassword) && CurrentPassword.Length > 0 && IsIncorrectPasswordNotificationVisible == false; }
        }

        private void HandlePassword()
        {
            CurrentPassword = string.Empty;
            IsIncorrectPasswordNotificationVisible = false;
            _AttemptsAtPassword = DEFAULT_ATTEMPTS_AT_PASSWORD;
        }

        private void PasswordChanged(string newPassword)
        {
            if (newPassword != null)
            {
                CurrentPassword = newPassword;
            }
        }

        private void PasswordKeyDown(Windows.System.VirtualKey key)
        {
            if (key == Windows.System.VirtualKey.Enter)
            {
                VerifyPassword();
            }
            else if (key == Windows.System.VirtualKey.Escape)
            {
                CancelPassword();
            }
        }

        private async void VerifyPassword()
        {
            if (string.IsNullOrEmpty(CurrentPassword))
            {
                return;
            }
            if (_Doc.InitStdSecurityHandler(CurrentPassword))
            {
                if (PasswordHandled != null)
                {
                    PasswordHandled(true, CurrentPassword);
                }
            }
            else
            {
                --_AttemptsAtPassword;
                IsIncorrectPasswordNotificationVisible = true;
                if (_AttemptsAtPassword == 0)
                {

                    if (PasswordHandled != null)
                    {
                        PasswordHandled(false, null);
                    }

                    AnalyticsHandler.CURRENT.SendEvent(AnalyticsHandler.Category.VIEWER, "Document Opened XFA");
                    await System.Threading.Tasks.Task.Delay(200);
                    Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    MessageDialog md = new MessageDialog(string.Format(loader.GetString("PasswordDialog_ManyFails_Info"), DEFAULT_ATTEMPTS_AT_PASSWORD), loader.GetString("PasswordDialog_ManyFails_Title"));
                    await CompleteReader.Utilities.MessageDialogHelper.ShowMessageDialogAsync(md);
                }
            }
        }

        private void CancelPassword()
        {
            if (PasswordHandled != null)
            {
                PasswordHandled(false, null);
            }
        }
    }
}
