using IPCSoftware.App.Controls;
using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
        // --- DI Services ---
        private readonly IPLCTagConfigurationService _tagService;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;

        // --- Timers ---
        // 1. For Live Data (TCP Polling) - e.g. OEE, Machine Status
        private readonly SafePoller _liveDataTimer;
        // 2. For Cycle Sync (JSON Polling) - e.g. Images, Inspection Results
        private readonly SafePoller _uiSyncTimer;
        private int _liveDataRunning = 0;

        // --- JSON State Sync Variables ---
        private readonly string _jsonStatePath;
        private DateTime _lastJsonWriteTime;


        // --- Commands ---
        public ICommand ToggleThemeCommand { get; }
        public ICommand ShowImageCommand { get; }
        public ICommand OpenCardDetailCommand { get; }

        // --- Member Variables ---
        private bool _isDarkTheme = false;



        // Map Grid Index(0-11) to Physical Station Number(1-12)
        private readonly int[] _visualToStationMap = new int[]
        {
                1, 2, 3,
                4, 5, 6,    // Changed to Linear Order
                7, 8, 9,
                10, 11, 12  // Changed to Linear Order
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

        private string _qrCodeText = "Waiting for start...";
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

        // The Collection for the 12 Grid Stations
        public ObservableCollection<CameraImageItem> CameraImages { get; } = new ObservableCollection<CameraImageItem>();

        private double _latestX;
        public double LatestX
        {
            get => _latestX;
            set => SetProperty(ref _latestX, value);
        }

        private double _latestY;
        public double LatestY
        {
            get => _latestY;
            set => SetProperty(ref _latestY, value);
        }

        private double _latestTheta;
        public double LatestTheta
        {
            get => _latestTheta;
            set => SetProperty(ref _latestTheta, value);
        }


        #endregion


        #region Hrd coded value

        // LEFT PANEL


        private double _quality = 0;
        public double Quality
        {
            get => _quality;
            set => SetProperty(ref _quality, value);
        }

        private double _availability = 0;
        public double Availability
        {
            get => _availability;
            set => SetProperty(ref _availability, value);
        }

        private double _performance = 0;
        public double Performance
        {
            get => _performance;
            set => SetProperty(ref _performance, value);
        }

        private double _overallOEE = 0;
        public double OverallOEE
        {
            get => _overallOEE;
            set => SetProperty(ref _overallOEE, value);
        }

        private string _operatingTime;
        public string OperatingTime
        {
            get => _operatingTime;
            set => SetProperty(ref _operatingTime, value);
        }

        private string _downtime;
        public string Downtime
        {
            get => _downtime;
            set => SetProperty(ref _downtime, value);
        }

        private int _goodUnits = 0;
        public int GoodUnits
        {
            get => _goodUnits;
            set => SetProperty(ref _goodUnits, value);
        }

        private int _rejectedUnits = 0;
        public int RejectedUnits
        {
            get => _rejectedUnits;
            set => SetProperty(ref _rejectedUnits, value);
        }

        private int _inFlow = 0; // Total Input
        public int InFlow
        {
            get => _inFlow;
            set => SetProperty(ref _inFlow, value);
        }

        private int _cycleTime = 0; // Total Input
        public int CycleTime
        {
            get => _cycleTime;
            set => SetProperty(ref _cycleTime, value);
        }

        public string Remarks { get; set; }
        public DispatcherTimer Timer { get; }
        public string OEEText => "87.4%";
        // TREND DATA
        private List<double> _cycleTrend;
        public List<double> CycleTrend
        {
            get => _cycleTrend;
            set => SetProperty(ref _cycleTrend, value);
        }
        #endregion


        public OEEDashboardViewModel(
            IPLCTagConfigurationService tagService,
            IOptions<CcdSettings> ccdSettng,
            CoreClient coreClient,
            IDialogService dialog,
            IAppLogger logger) : base(logger)
        {
            var ccd = ccdSettng.Value;
            _tagService = tagService;
            _coreClient = coreClient;
            _dialog = dialog;


            // Path to shared state file
            _jsonStatePath = Path.Combine(ccd.QrCodeImagePath, ccd.CurrentCycleStateFileName);
            // _jsonStatePath = Path.Combine(ConstantValues.QrCodeImagePath, "CurrentCycleState.json");

            // Initialize Lists
            //  InitializeAsync();      // Writable Tags
            InitializeCameraGrid(); // Camera Grid placeholders

            // --- COMMANDS ---
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            OpenCardDetailCommand = new RelayCommand<string>(OpenCardDetail);
            ShowImageCommand = new RelayCommand<CameraImageItem>(ShowImage);

            DummyData();
            // 1. Live Data Timer (1000ms) - Gets OEE, IOs, Status from Core Service via TCP
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(100), LiveDataTimerTick);
            _liveDataTimer.Start();

            // 2. UI Sync Timer (200ms) - Gets Images and Station Data from JSON (Cycle Synced)
            _uiSyncTimer = new SafePoller( TimeSpan.FromMilliseconds(100), UiSyncTick);
            _uiSyncTimer.Start();

            // Force initial sync
            SyncUiWithJson();
        }

        #region Data Intilization

        private void DummyData()
        {
            // --- DUMMY / INITIAL DATA ---
            // Availability = 92.5;
            // Performance = 88.1;
            // Quality = 96.2;
            OverallOEE = Math.Round((Availability * Performance * Quality) / 10000, 2);
            // OperatingTime = "7h 32m";
            //  Downtime = "28m";
            //  GoodUnits = 1325;
            //  RejectedUnits = 48;
            Remarks = "All processes stable.";
            CycleTrend = new List<double> { 2.8, 2.9, 2.7, 3.0, 2.8, 2.9, 2.85, 2.75, 2.9 };
        }

        /* private async void InitializeAsync()
         {
             try
             {
                 //  InitializeTagMap();
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
             catch (Exception ex)
             {
                 _logger.LogError(ex.Message, LogType.Diagnostics);
             }
         }*/

        private void InitializeCameraGrid()
        {
            try
            {
                // Populate the collection with 12 empty items
                CameraImages.Clear();
                for (int i = 0; i < 12; i++)
                {
                    CameraImages.Add(new CameraImageItem
                    {
                        StationNumber = _visualToStationMap[i],
                        Result = "Unchecked",
                        ImagePath = null,
                        ValX = 0,
                        ValY = 0,
                        ValZ = 0// Default border color logic
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        #endregion


        #region Timer Loops

        // Loop 1: Live Data (TCP)
        private async Task LiveDataTimerTick()
        {
            if (Interlocked.Exchange(ref _liveDataRunning, 1) == 1) return;
            try
            {
                var getTask = _coreClient.GetIoValuesAsync(4);
                var completed = await Task.WhenAny(getTask, Task.Delay(2000));

                if (completed == getTask)
                {
                    var resultDict = await getTask;
                    if (resultDict != null && resultDict.TryGetValue(4, out object oeeObj))
                    {
                        var json = JsonConvert.SerializeObject(oeeObj);
                        var oeeResult = JsonConvert.DeserializeObject<OeeResult>(json);
                        if (oeeResult != null)
                        {
                            Availability = Math.Round(oeeResult.Availability * 100, 1);
                            Performance = Math.Round(oeeResult.Performance * 100, 1);
                            Quality = Math.Round(oeeResult.Quality * 100, 1);
                            OverallOEE = Math.Round(oeeResult.OverallOEE * 100, 1);
                            OperatingTime = FormatDuration(oeeResult.OperatingTime);
                            Downtime = FormatDuration(oeeResult.Downtime);
                            GoodUnits = oeeResult.OKParts;
                            RejectedUnits = oeeResult.NGParts;
                            CycleTime = oeeResult.CycleTime;
                            InFlow = oeeResult.TotalParts;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("LiveDataTimerTick: GetIoValuesAsync timed out.", LogType.Diagnostics);
                }

                // NOTE: If you need to update Header/Summary values from liveData manually (without map):
                // if (liveData.ContainsKey(99)) OverallOEE = Convert.ToDouble(liveData[99]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Live Data Error: " + ex.Message, LogType.Diagnostics);
            }
            finally
            {
                Interlocked.Exchange(ref _liveDataRunning, 0);
            }
        }



        // Loop 2: Cycle Sync (JSON)
        private async Task UiSyncTick()
        {
          
            SyncUiWithJson();
        }

        #endregion


        #region Logic Methods

        private string FormatDuration(double totalSeconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
            if (t.TotalDays >= 1)
            {
                return $"{t.Days}d {t.Hours}h {t.Minutes}m";
            }
            else
            {
                return t.ToString(@"hh\:mm\:ss");
            }
        }


        private void SyncUiWithJson()
        {
            try
            {
                // 1. Reset Check
                if (!File.Exists(_jsonStatePath))
                {
                    if (_lastJsonWriteTime != DateTime.MinValue) { ResetDashboard(); _lastJsonWriteTime = DateTime.MinValue; }
                    return;
                }

                // 2. Read
                DateTime currentWriteTime = File.GetLastWriteTime(_jsonStatePath);
                if (currentWriteTime == _lastJsonWriteTime) return;

                string jsonContent = string.Empty;
                for (int k = 0; k < 3; k++) { try { jsonContent = File.ReadAllText(_jsonStatePath); break; } catch { Thread.Sleep(50); } }
                if (string.IsNullOrEmpty(jsonContent)) return;

                var state = JsonConvert.DeserializeObject<CycleStateModel>(jsonContent);
                if (state == null) return;

                // 3. Logic: If BatchID changed from what we have, reset first
                if (state.BatchId != QRCodeText)
                {
                    ResetDashboard();
                }

                QRCodeText = state.BatchId;

                // 4. Update QR Image
                if (state.Stations.ContainsKey(0))
                {
                    var qrData = state.Stations[0];
                    if (QrCodeImage == null || (QrCodeImage is BitmapImage bmp && bmp.UriSource?.LocalPath != qrData.ImagePath))
                        Application.Current.Dispatcher.Invoke(() => QrCodeImage = LoadBitmapSafe(qrData.ImagePath));
                }

                // 5. Update Grid & Find Latest
                StationResult latestStation = null;

                foreach (var kvp in state.Stations)
                {
                    if (kvp.Key == 0) continue;

                    var data = kvp.Value;

                    // Track latest (highest ID processed so far)
                    if (latestStation == null || data.StationNumber > latestStation.StationNumber)
                    {
                        latestStation = data;
                    }

                    var uiItem = CameraImages.FirstOrDefault(x => x.StationNumber == kvp.Key);
                    if (uiItem != null)
                    {
                        if (uiItem.LastLoadedFilePath != data.ImagePath)
                        {
                            string pathCopy = data.ImagePath;
                            uiItem.LastLoadedFilePath = pathCopy;

                            // Load bitmap off UI thread to avoid blocking dispatcher
                            _ = Task.Run(() =>
                            {
                                BitmapImage bmp = null;
                                try
                                {
                                    if (!string.IsNullOrEmpty(pathCopy) && File.Exists(pathCopy))
                                    {
                                        using var fs = new FileStream(pathCopy, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                        var bi = new BitmapImage();
                                        bi.BeginInit();
                                        bi.CacheOption = BitmapCacheOption.OnLoad;
                                        bi.StreamSource = fs;
                                        bi.EndInit();
                                        bi.Freeze();
                                        bmp = bi;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"LoadBitmapSafe background load failed: {ex.Message}", LogType.Diagnostics);
                                }

                                // assign on UI thread
                                Application.Current?.Dispatcher.Invoke(() => uiItem.ImagePath = bmp);
                            });
                        }
                        uiItem.Result = data.Status;
                        uiItem.ValX = data.X; uiItem.ValY = data.Y; uiItem.ValZ = data.Z;
                    }
                }

                // 6. Update Latest Measurement Panel
                if (latestStation != null)
                {
                    LatestX = latestStation.X;
                    LatestY = latestStation.Y;
                    LatestTheta = latestStation.Z; // Assuming Z maps to Theta
                }

                _lastJsonWriteTime = currentWriteTime;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Sync Error: {ex.Message}"); }
        }
        private void ResetDashboard()
        {
            QRCodeText = "Waiting for Scan...";
            QrCodeImage = null; // Clear QR Image

            // Clear Measurements
            LatestX = 0;
            LatestY = 0;
            LatestTheta = 0;

            foreach (var item in CameraImages)
            {
                item.ImagePath = null;
                item.LastLoadedFilePath = null;
                item.Result = "Unchecked";
                item.ValX = 0;
                item.ValY = 0;
                item.ValZ = 0;
            }
        }

        private BitmapImage LoadBitmapSafe(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { throw; }
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
            try
            {
                if (img == null) return;
                string title = $"INSPECTION POSITION {img.StationNumber}"; // Use StationNumber or Id
                var window = new FullImageView(img, title);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void OpenCardDetail(string cardType)
        {
            try
            {
                string title = "";
                var data = new List<MetricDetailItem>();

                switch (cardType)
                {
                    case "Efficiency":
                        title = "Efficiency Breakdown Details";
                        data.Add(new MetricDetailItem { MetricName = "Availability", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });
                        data.Add(new MetricDetailItem { MetricName = "Performance", CurrentVal = "0%", WeeklyVal = "0%D", MonthlyVal = "0%" });
                        data.Add(new MetricDetailItem { MetricName = "Quality", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });
                        break;

                    case "OEE":
                        title = "OEE Score Statistics";
                        data.Add(new MetricDetailItem { MetricName = "OEE Score", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });

                        break;

                    case "CycleTime":
                        title = "Cycle Time Trends";
                        data.Add(new MetricDetailItem { MetricName = "Avg Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });
                        data.Add(new MetricDetailItem { MetricName = "Ideal Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });
                        break;
                    case "OperatingTime":
                        title = "Operating Time";
                        data.Add(new MetricDetailItem { MetricName = "Operating Time", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
                        break;

                    case "Downtime":
                        title = "Downtime Statistics";
                        data.Add(new MetricDetailItem { MetricName = "Total Stop", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
                        data.Add(new MetricDetailItem { MetricName = "Minor Stops", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
                        data.Add(new MetricDetailItem { MetricName = "Changeover", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
                        break;

                    case "AvgCycle": // Or "CycleTime" depending on your CommandParameter
                        title = "Cycle Time Metrics";
                        data.Add(new MetricDetailItem { MetricName = "Actual Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });
                        data.Add(new MetricDetailItem { MetricName = "Ideal Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });

                        break;

                    case "InFlow":
                        title = "Input Statistics";
                        data.Add(new MetricDetailItem { MetricName = "Total Input", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
                        break;

                    case "OK":
                        title = "Production Quality (OK)";
                        data.Add(new MetricDetailItem { MetricName = "Good Units", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
                        data.Add(new MetricDetailItem { MetricName = "Yield Rate", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });
                        break;

                    case "NG":
                        title = "Rejection Statistics (NG)";
                        data.Add(new MetricDetailItem { MetricName = "Total Rejects", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        #endregion


        #region Dispose
        public void Dispose()
        {
            try
            {
                _liveDataTimer.Dispose();

                _uiSyncTimer.Dispose();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        #endregion


    }
}
