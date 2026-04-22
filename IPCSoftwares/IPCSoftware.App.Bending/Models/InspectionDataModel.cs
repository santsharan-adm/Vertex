using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
 /// Represents a single inspection data row for input/output inspection tables
    /// </summary>
    public class InspectionDataModel : INotifyPropertyChanged
    {
        private string _partNumber;
 private double _measurement1;
        private double _measurement2;
        private double _measurement3;
        private double _measurement4;
        private bool _isOk;
        private string _result;

        public string PartNumber
        {
  get { return _partNumber; }
            set { SetProperty(ref _partNumber, value); }
        }

        public double Measurement1
        {
          get { return _measurement1; }
   set { SetProperty(ref _measurement1, value); }
        }

        public double Measurement2
        {
            get { return _measurement2; }
            set { SetProperty(ref _measurement2, value); }
        }

   public double Measurement3
        {
     get { return _measurement3; }
         set { SetProperty(ref _measurement3, value); }
        }

 public double Measurement4
        {
     get { return _measurement4; }
            set { SetProperty(ref _measurement4, value); }
    }

        public bool IsOk
        {
            get { return _isOk; }
            set { SetProperty(ref _isOk, value); }
   }

     public string Result
    {
        get { return _result; }
            set { SetProperty(ref _result, value); }
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
