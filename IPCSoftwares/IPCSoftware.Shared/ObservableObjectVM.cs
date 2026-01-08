using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared
{
    public class ObservableObjectVM : INotifyPropertyChanged
    {
   
        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsDirty { get; set; }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T storage, T value,
            [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            IsDirty = true;
            return true;
        }
    }
}
