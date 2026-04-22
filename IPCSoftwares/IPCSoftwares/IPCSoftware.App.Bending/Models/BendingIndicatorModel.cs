using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
  /// Represents a single bending machine indicator (status light)
    /// </summary>
   public class BendingIndicatorModel : INotifyPropertyChanged
    {
    private bool _isActive;
        private string _toolTip;

        public bool IsActive
     {
            get { return _isActive; }
      set { SetProperty(ref _isActive, value); }
        }

        public string ToolTip
      {
       get { return _toolTip; }
           set { SetProperty(ref _toolTip, value); }
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

  /// <summary>
    /// Represents a single bending machine unit (B1, B2, B3, B4) with multiple indicators
    /// </summary>
    public class BendingUnitModel : INotifyPropertyChanged
    {
        private string _name;
        private ObservableCollection<BendingIndicatorModel> _indicators;

        public string Name
      {
      get { return _name; }
   set { SetProperty(ref _name, value); }
        }

    public ObservableCollection<BendingIndicatorModel> Indicators
        {
           get { return _indicators; }
        set { SetProperty(ref _indicators, value); }
        }

        public BendingUnitModel()
        {
       Indicators = new ObservableCollection<BendingIndicatorModel>();
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
