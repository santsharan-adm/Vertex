using IPCSoftware.App.Controls;
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
        private readonly DispatcherTimer _liveDataTimer;
        // 2. For Cycle Sync (JSON Polling) - e.g. Images, Inspection Results
        private readonly DispatcherTimer _uiSyncTimer;

        // --- JSON State Sync Variables ---
        private readonly string _jsonStatePath;
        private DateTime _lastJsonWriteTime;


        // --- Commands ---
        public ICommand ToggleThemeCommand { get; }
        public ICommand ShowImageCommand { get; }
        public ICommand OpenCardDetailCommand { get; }

        // --- Member Variables ---
        private bool _disposed;
        private bool _isDarkTheme = false;
        private Dictionary<int, Action<object>> _tagValueMap;

        // Filter for Writable Tags (Settings/Inputs)
        private HashSet<int> allowedTagNos = new HashSet<int>
        {
            15, 16, 17, 18, 19, 20
        };

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

        // The Collection for User Inputs (Writable Tags)
        public ObservableCollection<WritableTagItem> AllInputs { get; } = new();


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

        private int _operatingTime = 0;
        public int OperatingTime
        {
            get => _operatingTime;
            set => SetProperty(ref _operatingTime, value);
        }

        private int _downtime = 0;
        public int Downtime
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
            InitializeAsync();      // Writable Tags
            InitializeCameraGrid(); // Camera Grid placeholders

            // --- COMMANDS ---
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            OpenCardDetailCommand = new RelayCommand<string>(OpenCardDetail);
            ShowImageCommand = new RelayCommand<CameraImageItem>(ShowImage);

            DummyData();
            // 1. Live Data Timer (1000ms) - Gets OEE, IOs, Status from Core Service via TCP
            _liveDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _liveDataTimer.Tick += LiveDataTimerTick;
            _liveDataTimer.Start();

            // 2. UI Sync Timer (200ms) - Gets Images and Station Data from JSON (Cycle Synced)
            _uiSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _uiSyncTimer.Tick += UiSyncTick;
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

        private async void InitializeAsync()
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

        private void InitializeCameraGrid()
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

        /*   private void InitializeTagMap()
           {
               _tagValueMap = new Dictionary<int, Action<object>>
               {

                  // [16] = v => QRCodeText = v?.ToString()
                   //[17] = v => OkNgStatus = Convert.ToInt32(v),
                   //[18] = v => CameraStatus = Convert.ToBoolean(v)
                   //[19] = v => CameraStatus = Convert.ToBoolean(v)
                   //[20] = v => CameraStatus = Convert.ToBoolean(v)
               };
           }
   */


        private void InitializeTagMap()
        {
            _tagValueMap = new Dictionary<int, Action<object>>
            {
                // Mapping Actual PLC Tags to UI Properties

                // Tag 24: UpTime (Operating Time)
                //  [ConstantValues.TAG_UpTime] = v => OperatingTime = v.ToString(),

                // Tag 26: DownTime
                //  [ConstantValues.TAG_DownTime] = v => Downtime = v.ToString(),

                // Tag 28: Good Units (OK)
                [ConstantValues.TAG_OK] = v => GoodUnits = Convert.ToInt32(v),

                // Tag 29: Rejected Units (NG)
                [ConstantValues.TAG_NG] = v => RejectedUnits = Convert.ToInt32(v),

                // Tag 27: InFlow (Total)
                [ConstantValues.TAG_InFlow] = v => InFlow = Convert.ToInt32(v),

                // Tag 22: Cycle Time (Optional: Update trend or display)
                [ConstantValues.TAG_CycleTime] = v => CycleTime = Convert.ToInt32(v)
            };
        }

        #endregion


        #region Timer Loops

        // Loop 1: Live Data (TCP)
        private async void LiveDataTimerTick(object sender, EventArgs e)
        {
            if (_disposed) return;
            try
            {
                var resultDict = await _coreClient.GetIoValuesAsync(4);
               

                if (resultDict != null && resultDict.TryGetValue(4, out object oeeObj))
                {
                    // Deserialization: Convert object (which might be JObject/JsonElement) to OeeResult
                    var json = JsonConvert.SerializeObject(oeeObj);
                    var oeeResult = JsonConvert.DeserializeObject<OeeResult>(json);

                    if (oeeResult != null)
                    {
                        // Map OEE Result to View Model Properties
                        // Assuming percentages are 0.0-1.0 in backend, display as % (0-100) or keep as is depending on UI converter.
                        // Assuming UI expects 0-100 based on previous dummy data "92.5".
                        Availability = Math.Round(oeeResult.Availability * 100, 1);
                        Performance = Math.Round(oeeResult.Performance * 100, 1);
                        Quality = Math.Round(oeeResult.Quality * 100, 1);
                        OverallOEE = Math.Round(oeeResult.OverallOEE * 100, 1);

                        OperatingTime = oeeResult.OperatingTime;
                        Downtime = oeeResult.Downtime;
                        GoodUnits = oeeResult.OKParts;
                        RejectedUnits = oeeResult.NGParts;
                        CycleTime = oeeResult.CycleTime;
                        InFlow = oeeResult.TotalParts;
                    }
                }

                // NOTE: If you need to update Header/Summary values from liveData manually (without map):
                // if (liveData.ContainsKey(99)) OverallOEE = Convert.ToDouble(liveData[99]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Live Data Error: {ex.Message}");
            }
        }



        // Loop 2: Cycle Sync (JSON)
        private void UiSyncTick(object sender, EventArgs e)
        {
            if (_disposed) return;
            SyncUiWithJson();
        }

        #endregion


        #region Logic Methods

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

        private void SyncUiWithJson()
        {
            try
            {
                // 1. Check if JSON file exists
                if (!File.Exists(_jsonStatePath))
                {
                    // CASE: File Deleted by Service (Reset Cycle)
                    if (_lastJsonWriteTime != DateTime.MinValue)
                    {
                        ResetDashboard();
                        _lastJsonWriteTime = DateTime.MinValue;
                    }
                    return;
                }

                // 2. Check if file has been modified
                DateTime currentWriteTime = File.GetLastWriteTime(_jsonStatePath);
                if (currentWriteTime == _lastJsonWriteTime) return; // No changes

                // 3. Read content
                string jsonContent = string.Empty;
                for (int k = 0; k < 3; k++) // Retry for lock
                {
                    try { jsonContent = File.ReadAllText(_jsonStatePath); break; }
                    catch { System.Threading.Thread.Sleep(50); }
                }

                if (string.IsNullOrEmpty(jsonContent)) return;

                // 4. Deserialize
                var state = JsonConvert.DeserializeObject<CycleStateModel>(jsonContent);
                if (state == null) return;

                // 5. Update UI Properties
                // We use JSON for Batch ID to keep it in sync with the images
                QRCodeText = state.BatchId;

                // If the QR Image is stored as Station 0 in JSON:
                if (state.Stations.ContainsKey(0))
                {
                    var qrData = state.Stations[0];
                    // Load QR Image if changed
                    // Note: We don't have a check for LastLoadedFilePath for QR code separate var, 
                    // but we can check the source string
                    if (QrCodeImage == null || (QrCodeImage is BitmapImage bmp && bmp.UriSource?.LocalPath != qrData.ImagePath))
                    {
                        Application.Current.Dispatcher.Invoke(() => QrCodeImage = LoadBitmapSafe(qrData.ImagePath));
                    }
                }

                // 6. Update Grid Items (Stations 1-12)
                foreach (var kvp in state.Stations)
                {
                    int stationNo = kvp.Key;
                    if (stationNo == 0) continue; // Skip QR

                    var data = kvp.Value;
                    var uiItem = CameraImages.FirstOrDefault(x => x.StationNumber == stationNo);

                    if (uiItem != null)
                    {
                        // Update Image
                        if (uiItem.LastLoadedFilePath != data.ImagePath)
                        {
                            uiItem.LastLoadedFilePath = data.ImagePath;
                            uiItem.ImagePath = LoadBitmapSafe(data.ImagePath);
                        }

                        // Update Data (X, Y, Z, Status)
                        uiItem.Result = data.Status;
                        uiItem.ValX = data.X;
                        uiItem.ValY = data.Y;
                        uiItem.ValZ = data.Z;
                    }
                }

                _lastJsonWriteTime = currentWriteTime;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync Error: {ex.Message}");
            }
        }

        private void ResetDashboard()
        {
            QRCodeText = "Waiting for Scan...";
            QrCodeImage = null; // Clear QR Image

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
            catch { return null; }
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
            var window = new FullImageView(img, title);
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


        #region Dispose
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _liveDataTimer.Stop();
            _liveDataTimer.Tick -= LiveDataTimerTick;

            _uiSyncTimer.Stop();
            _uiSyncTimer.Tick -= UiSyncTick;

            GC.SuppressFinalize(this);
        }
        #endregion


    }
}
