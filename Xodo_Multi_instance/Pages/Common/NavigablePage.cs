using CompleteReader.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CompleteReader.Pages.Common
{
    public class NavigablePage : Page
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ActivateViewModel(e.Parameter);
        }

        protected void ActivateViewModel(object parameter)
        {
            var navigableViewModel = this.DataContext as INavigable;
            if (navigableViewModel != null)
            {
                navigableViewModel.NewINavigableAvailable += navigableViewModel_NewINavigableAvailable;
                navigableViewModel.Activate(parameter);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            DeactivateViewModel(e.Parameter);
        }

        protected void DeactivateViewModel(object parameter)
        {
            var navigableViewModel = this.DataContext as INavigable;
            if (navigableViewModel != null)
            {
                this.DataContext = null;
                navigableViewModel.Deactivate(parameter);
                navigableViewModel.NewINavigableAvailable -= navigableViewModel_NewINavigableAvailable;
            }
        }

        /// <summary>
        /// Override this to show the new content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newNavigable"></param>
        protected virtual void navigableViewModel_NewINavigableAvailable(INavigable sender, INavigable newNavigable)
        {

        }

    }
}
