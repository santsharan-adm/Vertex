using System.Windows.Media;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private string _robot1Name = "ROBOT 1";
        public string Robot1Name
        {
            get => _robot1Name;
            set { _robot1Name = value; OnPropertyChanged(); }
        }

        private string _robot2Name = "ROBOT 2";
        public string Robot2Name
        {
            get => _robot2Name;
            set { _robot2Name = value; OnPropertyChanged(); }
        }

        public DashboardViewModel()
        {
        }
    }
}