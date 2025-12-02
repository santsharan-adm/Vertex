using IPCSoftware.App.Controls;
using IPCSoftware.App.Views;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class OEEDashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        // HEADER DATA
        public string CurrentDateTime { get; set; }

        // LEFT PANEL
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double OverallOEE { get; set; }


        // RIGHT SUMMARY
        public string OperatingTime { get; set; }
        public string Downtime { get; set; }
        public int GoodUnits { get; set; }
        public int RejectedUnits { get; set; }
        public string Remarks { get; set; }
        public DispatcherTimer Timer { get; }
        public string OEEText => "87.4%";
        // TREND DATA
        private List<double> _cycleTrend;
        public List<double> CycleTrend
        {
            get => _cycleTrend;
            set { _cycleTrend = value; Notify(); }
        }

        public ObservableCollection<PieSliceModel> OEEParts { get; set; } =
                new ObservableCollection<PieSliceModel>
            {
                new PieSliceModel { Label="OK", Value=1140 },
                new PieSliceModel { Label="Tossed", Value=35 },
                new PieSliceModel { Label="NG", Value=25 },
                new PieSliceModel { Label="Rework", Value=20 }
            };



        public OEEDashboardViewModel()
        {
            // ----- HEADER -----
            CurrentDateTime = DateTime.Now.ToString("dddd, MMM dd yyyy HH:mm");

            // ----- LEFT PANEL (DUMMY LIVE VALUES) -----
            Availability = 92.5;
            Performance = 88.1;
            Quality = 96.2;
            OverallOEE = Math.Round((Availability * Performance * Quality) / 10000, 2);

            // ----- SUMMARY -----
            OperatingTime = "7h 32m";
            Downtime = "28m";
            GoodUnits = 1325;
            RejectedUnits = 48;
            Remarks = "All processes stable.";


            {
                ShowImageCommand = new RelayCommand<CameraImageModel>(ShowImage);
            }


            CycleTrend = new List<double>
                {
                    5.8, 6.0, 5.9, 6.1, 5.7, 6.2, 5.9, 6.3, 5.6
                };



            // ----- LIVE CLOCK UPDATE -----
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += (s, e) =>
            {
                CurrentDateTime = DateTime.Now.ToString("dddd, MMM dd yyyy HH:mm");
                Notify(nameof(CurrentDateTime));
            };
            Timer.Start();

        }

        public ICommand ShowImageCommand { get; }

        private void ShowImage(CameraImageModel img)
        {
            if (img == null) return;

            var window = new FullImageView(img.ImagePath);
            window.ShowDialog();
        }

        private void OpenImagePopup(string path)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var popup = new FullImageView(path);
                popup.Owner = App.Current.MainWindow;
                popup.ShowDialog();

            });
        }
        public ObservableCollection<CameraImageModel> CameraImages { get; set; } = new ObservableCollection<CameraImageModel>
{
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="OK" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="NG" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="TOSSED" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="OK" },

    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="OK" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="NG" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="OK" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="NG" },

    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="OK" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="TOSSED" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="OK" },
    new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.jpg", Result="NG" },
};

    }
}
