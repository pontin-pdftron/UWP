using CompleteReader.ViewModels.Common;
using pdftron.PDF;
using pdftron.SDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace CompleteReader.ViewModels.Viewer.Helpers
{
    public class PasswordFileViewModel : ViewModelBase
    {
        private bool _IsPopupOpen = false;
        public bool IsPopupOpen
        {
            get { return _IsPopupOpen; }
            set
            {
                if (Set(ref _IsPopupOpen, value))
                {
                    if (!value)
                    {
                        PopupClosed();
                    }
                    else
                    {
                        SetDefaultValues();
                    }
                }
            }
        }

        private string _CurrentPassword;
        public string CurrentPassword
        {
            get { return _CurrentPassword; }
            set
            {
                if (Set(ref _CurrentPassword, value))
                {
                    RaisePropertyChanged("IsOkEnabled");
                }
            }
        }

        private string _IsProtectedText;

        public string IsProtectedText
        {
            get { return _IsProtectedText; }
            set { Set(ref _IsProtectedText, value); }
        }

        private bool _IsProtected;

        public bool IsProtected
        {
            get { return _IsProtected; }
            set
            {
                if (Set(ref _IsProtected, value))
                {
                    RaisePropertyChanged("IsOkEnabled");
                }
            }
        }

        public bool IsOkEnabled
        {
            get { return IsProtected || ( !IsProtected && !string.IsNullOrEmpty(CurrentPassword)); }
        }

        public delegate void PopupClosedDelegate();
        public event PopupClosedDelegate PopupClosed;

        public delegate void PasswordConfirmedDelegate();
        public event PasswordConfirmedDelegate PasswordConfirmed;

        public PasswordFileViewModel(bool isProtected)
        {
            IsProtected = isProtected;
            if (isProtected)
            {
                Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                IsProtectedText = loader.GetString("PasswordPopup_PlaceHolderText");
            }
            Init();
        }

        private void Init()
        {
            CancelCommand = new RelayCommand(CancelCommandImpl);
            OkCommand = new RelayCommand(OkCommandImpl);
        }

        private void SetDefaultValues()
        {
            CurrentPassword = "";
        }


        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand OkCommand { get; private set; }

        private void CancelCommandImpl(object command)
        {
            IsPopupOpen = false;
        }

        private void OkCommandImpl(object command)
        {
            _IsPopupOpen = false;
            RaisePropertyChanged("IsPopupOpen");
            PasswordConfirmed();
        }

        public Tuple<bool, string> ApplyPassword(PDFDoc doc)
        {
            if (doc == null)
                return null;

            bool shouldUnlock = false;
            bool everythingIsOk = false;

            doc.Lock();
            shouldUnlock = true;
            try
            {
                doc.RemoveSecurity();
                if (!string.IsNullOrEmpty(CurrentPassword))
                {
                    SecurityHandler handler = new SecurityHandler();
                    handler.ChangeRevisionNumber(6);
                    handler.ChangeUserPassword(CurrentPassword);

                    handler.SetPermission(SecurityHandlerPermission.e_print, true);
                    handler.SetPermission(SecurityHandlerPermission.e_extract_content, false);

                    doc.SetSecurityHandler(handler);
                    everythingIsOk = true;
                }
                else
                {
                    everythingIsOk = true;
                }
            }
            catch (Exception ex)
            {
                everythingIsOk = false;
                string exm = pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex);
                Utilities.AnalyticsHandler.CURRENT.SendEvent(Utilities.AnalyticsHandler.EXCEPTION_APP, pdftron.PDF.Tools.Utilities.AnalyticsHandlerBase.GetExceptionMessage(ex));
            }
            finally
            {
                if (shouldUnlock)
                {
                    doc.Unlock();
                }
            }
            return new Tuple<bool, string>(everythingIsOk, CurrentPassword);
        }
    }
}
