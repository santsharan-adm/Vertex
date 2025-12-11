using IPCSoftware.App.Controls;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class OEEDashboardViewModel : BaseViewModel, IDisposable
    {
        // Di variables
        private readonly IPLCTagConfigurationService _tagService;
        private readonly DispatcherTimer _timer;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;


        //command variables
        public ICommand ToggleThemeCommand { get; }
        public ICommand ShowImageCommand { get; }
        public ICommand OpenCardDetailCommand { get; }

        //normal variables
        private bool _disposed;
        private string _lastLoadedImagePath = string.Empty; 
        private bool _isDarkTheme = false;

        private Dictionary<int, Action<object>> _tagValueMap;

        private HashSet<int> allowedTagNos = new HashSet<int>
        {
            15, 16, 17, 18, 19, 20
        };

        // Map Grid Index(0-11) to Physical Station Number(1-12)
        private readonly int[] _visualToStationMap = new int[]
        {
            1, 2, 3,
            6, 5, 4,
            7, 8, 9,
            12, 11, 10
        };

        private string _currentThemePath = "/IPCSoftware.App;component/Styles/LightTheme.xaml";
        public string CurrentThemePath
        {

            get => _currentThemePath;
            set => SetProperty(ref _currentThemePath, value);
        }

        #region Tag Properties with variables

        private short okNgStatus;
        public short OkNgStatus
        {
            get => okNgStatus;
            set => SetProperty(ref okNgStatus, value);
        }

        private string _qrCodeText = "Waiting for scan...";
        public string QRCodeText
        {
            get => _qrCodeText;
            set => SetProperty(ref _qrCodeText, value);
        }

        private ImageSource _qrCodeImage;
        public ImageSource QrCodeImage
        {
            get => _qrCodeImage;
            set => SetProperty(ref _qrCodeImage, value);
        }
        public ObservableCollection<CameraImageItem> CameraImages { get; } = new ObservableCollection<CameraImageItem>();

        #endregion


        #region Hrd coded value
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



        #endregion

      
        public ObservableCollection<WritableTagItem> AllInputs { get; } = new();

        public OEEDashboardViewModel(IPLCTagConfigurationService tagService, UiTcpClient tcpClient, IDialogService dialog)
        {
            _tagService = tagService;
            _coreClient = new CoreClient(tcpClient);
            _dialog = dialog;


            InitializeAsync();
            InitializeCameraGrid();

          

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


            
                ShowImageCommand = new RelayCommand<CameraImageItem>(ShowImage);
            

            CycleTrend = new List<double>
                            {
                                2.8, 2.9, 2.7, 3.0, 2.8, 2.9, 2.85, 2.75, 2.9
                            };

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _timer.Tick += TimerTick;
            _timer.Start();


            // ----- LIVE CLOCK UPDATE -----
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += (s, e) =>
            {
                CurrentDateTime = DateTime.Now.ToString("dddd, MMM dd yyyy HH:mm");

                OnPropertyChanged(nameof(CurrentDateTime));
            };
            Timer.Start();

           

        }

        #region Data Intilization
        private async void InitializeAsync()
        {
            InitializeTagMap();
            var allTags = await _tagService.GetAllTagsAsync();

            AllInputs.Clear();
            var writableFilteredTags = allTags
                .Where(t => t.CanWrite && allowedTagNos.Contains(t.TagNo))
                .ToList();

            foreach (var tag in writableFilteredTags)
            {
                AllInputs.Add(new WritableTagItem(tag));
            }
        }

        private void InitializeCameraGrid()
        {
            // Populate the collection with 12 empty items
            for (int i = 0; i < 12; i++)
            {
                CameraImages.Add(new CameraImageItem
                {
                    StationNumber = _visualToStationMap[i],
                    Result = "Unchecked" // Default border color logic
                });
            }
        }

        private void InitializeTagMap()
        {
            _tagValueMap = new Dictionary<int, Action<object>>
            {
                [16] = v => QRCodeText = v?.ToString()
                //[17] = v => OkNgStatus = Convert.ToInt32(v),
                //[18] = v => CameraStatus = Convert.ToBoolean(v)
                //[19] = v => CameraStatus = Convert.ToBoolean(v)
                //[20] = v => CameraStatus = Convert.ToBoolean(v)
            };
        }

        #endregion


        #region Method for commands
        private void ToggleTheme()
        {
            _isDarkTheme = !_isDarkTheme;
            CurrentThemePath = _isDarkTheme
            ? "/IPCSoftware.App;component/Styles/DarkTheme.xaml"
            : "/IPCSoftware.App;component/Styles/LightTheme.xaml";
        }

        private void ShowImage(CameraImageItem img)
        {
            if (img == null) return;
            string title = $"INSPECTION POSITION {img.StationNumber}"; // Use StationNumber or Id
            var window = new FullImageView(img.ImagePath, title);
             window.ShowDialog();
        }

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

        #endregion


        private async void TimerTick(object sender, EventArgs e)
        {
            if (_disposed)
                return;
            try
            {
                var liveData = await _coreClient.GetIoValuesAsync();
                UpdateValues(liveData);
                CheckForLatestQrImage();
                CheckForInspectionImages();
            }
            catch (Exception ex)
            {
                
            }
        }



        private void UpdateValues(Dictionary<int, object> dict)
        {
            if (dict == null) return;

            foreach (var kvp in dict)
            {
                if (_tagValueMap.TryGetValue(kvp.Key, out var setter))
                {
                    setter(kvp.Value);
                }
            }
        }



        private void CheckForLatestQrImage()
        {
            try
            {
                // Ensure directory exists
                string folderPath = ConstantValues.QrCodeImagePath;
                if (!Directory.Exists(folderPath)) return;

                var directory = new DirectoryInfo(folderPath);

                // Find files starting with "0" (e.g., 0_timestamp.bmp)
                // Filter for valid image extensions if needed
                var latestFile = directory.GetFiles("0*.*")
                                          .Where(f => FileTypeHelper.IsImageFile(f.Extension))
                                          .OrderByDescending(f => f.LastWriteTime)
                                          .FirstOrDefault();

                // If a file exists and it's different from the last one we showed
                if (latestFile != null && latestFile.FullName != _lastLoadedImagePath)
                {
                    _lastLoadedImagePath = latestFile.FullName;
                    UpdateQrImage(_lastLoadedImagePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for QR image: {ex.Message}");
            }
        }



        public void UpdateQrImage(string imagePath)
        {
            var bitmap = LoadBitmapSafe(imagePath);
            if (bitmap != null)
            {
                Application.Current.Dispatcher.Invoke(() => QrCodeImage = bitmap);
            }
        }



        private BitmapImage? LoadBitmapSafe(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                byte[] imageBytes = File.ReadAllBytes(path);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }



        private void CheckForInspectionImages()
        {
            try
            {
                string folderPath = ConstantValues.QrCodeImagePath;
                if (!Directory.Exists(folderPath)) return;

                var directory = new DirectoryInfo(folderPath);

                // Get all potential image files once to avoid disk hammering
                var allFiles = directory.GetFiles("*_raw.bmp"); // Or just *.bmp depending on your save logic

                // Loop through our visual grid slots
                for (int i = 0; i < CameraImages.Count; i++)
                {
                    var item = CameraImages[i];
                    int targetStation = item.StationNumber; // e.g., 6

                    // Find latest file for this specific station (starts with "6_")
                    var latestForStation = allFiles
                        .Where(f => f.Name.StartsWith($"{targetStation}_"))
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (latestForStation != null && latestForStation.FullName != item.LastLoadedFilePath)
                    {
                        // Found a new image! Update the item.
                        item.LastLoadedFilePath = latestForStation.FullName;

                        // Load Bitmap safely
                        var bitmap = LoadBitmapSafe(item.LastLoadedFilePath);

                        // Update UI on Dispatcher
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            item.ImagePath = bitmap;
                            // Optionally parse Result from filename if needed, e.g., "OK" or "NG"
                            item.Result = "OK"; // Defaulting to Green border for now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning inspection images: {ex.Message}");
            }
        }



        #region Dispose
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer.Stop();
            _timer.Tick -= TimerTick;
            GC.SuppressFinalize(this);
        }
        #endregion

    }



}
