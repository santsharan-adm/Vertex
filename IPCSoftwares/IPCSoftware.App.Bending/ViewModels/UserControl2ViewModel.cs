using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class UserControl2ViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;

        public ICommand ToggleSidebarCommand { get; }

        public UserControl2ViewModel(INavigationService navigationService)
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
