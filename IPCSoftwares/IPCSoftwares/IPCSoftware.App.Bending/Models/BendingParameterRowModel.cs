using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
    /// Represents a single bending parameter data row for UAT tables
    /// </summary>
 public class BendingParameterRowModel : INotifyPropertyChanged
    {
        private string _partNumber;
        private double _temperature1;
        private double _temperature2;
        private double _pressure1;
        private double _pressure2;

        public string PartNumber
    {
         get { return _partNumber; }
       set { SetProperty(ref _partNumber, value); }
        }

        public double Temperature1
   {
            get { return _temperature1; }
   set { SetProperty(ref _temperature1, value); }
        }

        public double Temperature2
    {
            get { return _temperature2; }
       set { SetProperty(ref _temperature2, value); }
        }

   public double Pressure1
        {
            get { return _pressure1; }
            set { SetProperty(ref _pressure1, value); }
        }

        public double Pressure2
        {
        get { return _pressure2; }
        set { SetProperty(ref _pressure2, value); }
        }

      public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
      return false;

        storage = value;
            OnPropertyChanged(propertyName);
    return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
 }
    }
}
