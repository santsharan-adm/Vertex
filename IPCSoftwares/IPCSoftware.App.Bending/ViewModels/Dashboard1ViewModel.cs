using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.UI.CommonViews.ViewModels;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class Dashboard1ViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        public ICommand ToggleSidebarCommand { get; }

        public Dashboard1ViewModel(INavigationService navigationService, IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            ToggleSidebarCommand = new RelayCommand(ExecuteGoToMenu);
        }

        private void ExecuteGoToMenu()
        {
            _navigationService.NavigateToBendingLandingPage();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}