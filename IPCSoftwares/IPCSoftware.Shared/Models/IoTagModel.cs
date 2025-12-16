using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class IoTagModel : INotifyPropertyChanged
    {
        private object _value;

        public int Id { get; set; }
        public string Name { get; set; }

        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayStatus));
            }
        }

        public string DisplayStatus
        {
            get
            {
                if (Value is bool b)
                    return b ? "ON" : "OFF";
                return Value?.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
