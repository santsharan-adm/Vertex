using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.UI.CommonViews.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class ViewModelBase : BaseViewModel
    {
        public ViewModelBase(IAppLogger logger) : base(logger)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}