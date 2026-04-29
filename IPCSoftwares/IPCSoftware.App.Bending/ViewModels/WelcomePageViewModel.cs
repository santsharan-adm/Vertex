using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPCSoftware.App.Bending.ViewModels
{
    /// <summary>
    /// ViewModel for the Welcome Page
    /// </summary>
    public class WelcomePageViewModel : INotifyPropertyChanged
    {
        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set
            {
                _welcomeMessage = value;
                OnPropertyChanged();
            }
        }

        public WelcomePageViewModel()
        {
            WelcomeMessage = "Welcome to Bending Application";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
