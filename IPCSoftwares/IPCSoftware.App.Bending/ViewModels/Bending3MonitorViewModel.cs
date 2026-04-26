using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class Bending3MonitorViewModel : INotifyPropertyChanged
    {
        // Product 1 Data
        private double _p1_Upper;
        public double P1_Upper { get => _p1_Upper; set { _p1_Upper = value; OnPropertyChanged(); } }

        private double _p1_Value;
        public double P1_Value { get => _p1_Value; set { _p1_Value = value; OnPropertyChanged(); } }

        private double _p1_Lower;
        public double P1_Lower { get => _p1_Lower; set { _p1_Lower = value; OnPropertyChanged(); } }

        private string _p1_LM;
        public string P1_LM { get => _p1_LM; set { _p1_LM = value; OnPropertyChanged(); } }

        private string _p1_BT;
        public string P1_BT { get => _p1_BT; set { _p1_BT = value; OnPropertyChanged(); } }

        // Product 2 Data
        private double _p2_Upper;
        public double P2_Upper { get => _p2_Upper; set { _p2_Upper = value; OnPropertyChanged(); } }

        private double _p2_Value;
        public double P2_Value { get => _p2_Value; set { _p2_Value = value; OnPropertyChanged(); } }

        private double _p2_Lower;
        public double P2_Lower { get => _p2_Lower; set { _p2_Lower = value; OnPropertyChanged(); } }

        private string _p2_LM;
        public string P2_LM { get => _p2_LM; set { _p2_LM = value; OnPropertyChanged(); } }

        private string _p2_BT;
        public string P2_BT { get => _p2_BT; set { _p2_BT = value; OnPropertyChanged(); } }

        // Product 3 Data
        private double _p3_Upper;
        public double P3_Upper { get => _p3_Upper; set { _p3_Upper = value; OnPropertyChanged(); } }

        private double _p3_Value;
        public double P3_Value { get => _p3_Value; set { _p3_Value = value; OnPropertyChanged(); } }

        private double _p3_Lower;
        public double P3_Lower { get => _p3_Lower; set { _p3_Lower = value; OnPropertyChanged(); } }

        private string _p3_LM;
        public string P3_LM { get => _p3_LM; set { _p3_LM = value; OnPropertyChanged(); } }

        private string _p3_BT;
        public string P3_BT { get => _p3_BT; set { _p3_BT = value; OnPropertyChanged(); } }

        // Product 4 Data
        private double _p4_Upper;
        public double P4_Upper { get => _p4_Upper; set { _p4_Upper = value; OnPropertyChanged(); } }

        private double _p4_Value;
        public double P4_Value { get => _p4_Value; set { _p4_Value = value; OnPropertyChanged(); } }

        private double _p4_Lower;
        public double P4_Lower { get => _p4_Lower; set { _p4_Lower = value; OnPropertyChanged(); } }

        private string _p4_LM;
        public string P4_LM { get => _p4_LM; set { _p4_LM = value; OnPropertyChanged(); } }

        private string _p4_BT;
        public string P4_BT { get => _p4_BT; set { _p4_BT = value; OnPropertyChanged(); } }

        public Bending3MonitorViewModel()
        {
          
            P1_Upper = 50.5; P1_Value = 48.2; P1_Lower = 45.0; P1_LM = "OK"; P1_BT = "2.5s";
            P2_Upper = 55.0; P2_Value = 52.1; P2_Lower = 50.0; P2_LM = "OK"; P2_BT = "2.8s";
            P3_Upper = 60.0; P3_Value = 58.9; P3_Lower = 55.0; P3_LM = "OK"; P3_BT = "3.1s";
            P4_Upper = 45.0; P4_Value = 44.5; P4_Lower = 40.0; P4_LM = "OK"; P4_BT = "2.2s";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}