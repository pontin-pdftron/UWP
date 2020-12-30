using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompleteReader.ViewModels.Common
{
    public delegate void NewINavigableAvailableDelegate(INavigable sender, INavigable newNavigable);

    public interface INavigable
    {
        event NewINavigableAvailableDelegate NewINavigableAvailable;

        void Activate(object parameter);
        void Deactivate(object parameter);
    }
}
