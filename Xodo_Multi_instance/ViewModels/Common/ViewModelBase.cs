using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CompleteReader.ViewModels.Common
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs eargs = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, eargs);
            }
        }

        /// <summary>
        /// Sets the value, and if it changed, will raise a property changed event with propertyName
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <param name="newValue"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected bool Set<T>(ref T field, T newValue = default(T), [CallerMemberName]String propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// This will make the view model go back if applicable. Override this on ViewModels.
        /// </summary>
        /// <returns>True if the view model performed a backward navigation step, false otherwise</returns>
        public virtual bool GoBack()
        {
            return false;
        }
    }
}
