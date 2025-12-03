using IPCSoftware.App.Controls;
using IPCSoftware.App.Views;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class OEEDashboardViewModel : BaseViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isDarkTheme = false;
        public ICommand ToggleThemeCommand { get; }

      
        private string _currentThemePath = "/IPCSoftware.App;component/Styles/LightTheme.xaml";

        public string CurrentThemePath
        {

            get => _currentThemePath;
            set => SetProperty(ref _currentThemePath, value);
        }


        private void ToggleTheme()
        {
            _isDarkTheme = !_isDarkTheme;
            CurrentThemePath = _isDarkTheme
            ? "/IPCSoftware.App;component/Styles/DarkTheme.xaml"
            : "/IPCSoftware.App;component/Styles/LightTheme.xaml";
            //string themePath = _isDarkTheme ? "Styles/DarkTheme.xaml" : "Styles/LightTheme.xaml";
            //SetTheme(themePath);
        }

        //private void SetTheme(string themePath)
        //{
        //    var newDict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };

        //    // Clear old theme dictionaries (assuming you only have 1 theme dict at a time)
        //    // In a real app, you might want to tag your theme dicts to remove only them
        //    Application.Current.Resources.MergedDictionaries.Clear();
        //    Application.Current.Resources.MergedDictionaries.Add(newDict);
        //}

      

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
            set  => SetProperty(ref _cycleTrend, value);
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
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            OpenCardDetailCommand = new RelayCommand<string>(OpenCardDetail);
            //ToggleThemeCommand = new RelayCommand(ToggleTheme);
            // Load default theme on startup
           // SetTheme("Styles/DarkTheme.xaml");
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
    2.8, 2.9, 2.7, 3.0, 2.8, 2.9, 2.85, 2.75, 2.9
};



            // ----- LIVE CLOCK UPDATE -----
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += (s, e) =>
            {
                CurrentDateTime = DateTime.Now.ToString("dddd, MMM dd yyyy HH:mm");

                OnPropertyChanged(nameof(CurrentDateTime));
            };
            Timer.Start();

            CameraImages = GetCameraImages();

        }

        public ICommand ShowImageCommand { get; }
        public ICommand OpenCardDetailCommand { get; }

        private void OpenCardDetail(string cardType)
        {
            string title = "";
            var data = new List<MetricDetailItem>();

            switch (cardType)
            {
                case "Efficiency":
                    title = "Efficiency Breakdown Details";
                    data.Add(new MetricDetailItem { MetricName = "Availability", CurrentVal = "85%", WeeklyVal = "87%", MonthlyVal = "88%" });
                    data.Add(new MetricDetailItem { MetricName = "Performance", CurrentVal = "95%", WeeklyVal = "94%", MonthlyVal = "93%" });
                    data.Add(new MetricDetailItem { MetricName = "Quality", CurrentVal = "97%", WeeklyVal = "98%", MonthlyVal = "98%" });
                    break;

                case "OEE":
                    title = "OEE Score Statistics";
                    data.Add(new MetricDetailItem { MetricName = "OEE Score", CurrentVal = "78%", WeeklyVal = "80%", MonthlyVal = "82%" });
  
                    break;

                case "CycleTime":
                    title = "Cycle Time Trends";
                    data.Add(new MetricDetailItem { MetricName = "Avg Cycle", CurrentVal = "2.9s", WeeklyVal = "3.1s", MonthlyVal = "3.0s" });
                    data.Add(new MetricDetailItem { MetricName = "Ideal Cycle", CurrentVal = "2.5s", WeeklyVal = "2.5s", MonthlyVal = "2.5s" });
                    break;
                case "OperatingTime":
                    title = "Operating Time";
                    data.Add(new MetricDetailItem { MetricName = "Operating Time", CurrentVal = "2.9", WeeklyVal = "3.1", MonthlyVal = "3.0" });
                    break;

                case "Downtime":
                    title = "Downtime Statistics";
                    data.Add(new MetricDetailItem { MetricName = "Total Stop", CurrentVal = "00:12:45", WeeklyVal = "01:30:00", MonthlyVal = "05:45:00" });
                    data.Add(new MetricDetailItem { MetricName = "Minor Stops", CurrentVal = "00:04:15", WeeklyVal = "00:25:00", MonthlyVal = "01:40:00" });
                    data.Add(new MetricDetailItem { MetricName = "Changeover", CurrentVal = "00:08:30", WeeklyVal = "01:05:00", MonthlyVal = "04:05:00" });
                    break;

                case "AvgCycle": // Or "CycleTime" depending on your CommandParameter
                    title = "Cycle Time Metrics";
                    data.Add(new MetricDetailItem { MetricName = "Actual Cycle", CurrentVal = "2.9s", WeeklyVal = "3.0s", MonthlyVal = "2.95s" });
                    data.Add(new MetricDetailItem { MetricName = "Ideal Cycle", CurrentVal = "2.5s", WeeklyVal = "2.5s", MonthlyVal = "2.5s" });
                  
                    break;

                case "InFlow":
                    title = "Input Statistics";
                    data.Add(new MetricDetailItem { MetricName = "Total Input", CurrentVal = "1165", WeeklyVal = "8150", MonthlyVal = "32500" });
                    break;

                case "OK":
                    title = "Production Quality (OK)";
                    data.Add(new MetricDetailItem { MetricName = "Good Units", CurrentVal = "1140", WeeklyVal = "7980", MonthlyVal = "31850" });
                    data.Add(new MetricDetailItem { MetricName = "Yield Rate", CurrentVal = "97.8%", WeeklyVal = "97.9%", MonthlyVal = "98.0%" });
                    break;

                case "NG":
                    title = "Rejection Statistics (NG)";
                    data.Add(new MetricDetailItem { MetricName = "Total Rejects", CurrentVal = "25", WeeklyVal = "170", MonthlyVal = "650" });
                           break;

                    // Add more cases for "OperatingTime", "Downtime", "InFlow", etc.
            }

            if (data.Count > 0)
            {
                // Open the Light Theme Popup
                var win = new DashboardDetailWindow();
                win.DataContext = new DashboardDetailViewModel(win, title, data);
                win.ShowDialog();
            }
        }
        private void ShowImage(CameraImageModel img)
        {
            if (img == null) return;
            string title = $"INSPECTION POSITION {img.Id}";

            var window = new FullImageView(img.ImagePath, title);
            window.ShowDialog();
        }


        private ObservableCollection<CameraImageModel> GetCameraImages()
        {
            var temp = new ObservableCollection<CameraImageModel>
                        {
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },

                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="NG" },

                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="OK" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="Unchecked" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="OK" },
                            new CameraImageModel { ImagePath="pack://application:,,,/IPCSoftware.App;component/Controls/Images/TestCamImage.png", Result="NG" },
                        };

            // Assign IDs automatically
            int id = 1;
            foreach (var item in temp)
                item.Id = id++;

            return temp;
        }



        public ObservableCollection<CameraImageModel> CameraImages { get; set; }
            = new ObservableCollection<CameraImageModel>();


    }
}
