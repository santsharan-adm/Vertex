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
using Newtonsoft.Json.Linq;
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
        private readonly string _prodCsvFolder;
        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;
        private readonly IProductConfigurationService _productService; // NEW Injection


        private ExternalSettings Settings => _settingsMonitor.CurrentValue;

        // --- Dynamic Grid Properties ---
        private int _dynamicRows = 4;
        public int DynamicRows { get => _dynamicRows; set => SetProperty(ref _dynamicRows, value); }

        private int _dynamicColumns = 3;
        public int DynamicColumns { get => _dynamicColumns; set => SetProperty(ref _dynamicColumns, value); }


        // --- Timers ---
        // 1. For Live Data (TCP Polling) - e.g. OEE, Machine Status
        private readonly SafePoller _liveDataTimer;
        // 2. For Cycle Sync (JSON Polling) - e.g. Images, Inspection Results
        private readonly SafePoller _uiSyncTimer;
        private readonly SafePoller _resetLogicTimer;
        private int _resetTimerRunning = 0; // Lock for safety

        private int _liveDataRunning = 0;

        // --- JSON State Sync Variables ---
        private readonly string _jsonStatePath;
        private DateTime _lastJsonWriteTime;


        // --- Commands ---
        public ICommand ToggleThemeCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ReverseCommand { get; }
        public ICommand ShowImageCommand { get; }
        public ICommand OpenCardDetailCommand { get; }

        // --- Member Variables ---
        private bool _isDarkTheme = false;

        public ICommand ToggleMacMiniCommand { get; } // NEW COMMAND

        // ... (Properties) ...
        private bool _isMacMiniEnabled;
        public bool IsMacMiniEnabled
        {
            get => _isMacMiniEnabled;
            set => SetProperty(ref _isMacMiniEnabled, value);
        }

        public string MacMiniStatusText => IsMacMiniEnabled ? "Mac Mini: ON" : "Mac Mini: OFF";
        public Brush MacMiniStatusColor => IsMacMiniEnabled ? new SolidColorBrush(Color.FromRgb(0, 122, 204)) : Brushes.Gray; // Blue vs Gray




        // Map Grid Index(0-11) to Physxical Station Number(1-12)
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

        public string UnitX => _settingsMonitor.CurrentValue.InspectionXUnit;
        public string UnitY => _settingsMonitor.CurrentValue.InspectionYUnit;
        public string UnitTheta => _settingsMonitor.CurrentValue.InspectionAngleUnit;

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


        private bool _switchDirection;
        public bool SwitchDirection
        {
            get => _switchDirection;
            set
            {
                SetProperty(ref _switchDirection, value); OnPropertyChanged(nameof(ConveyorDirectionText)); OnPropertyChanged(nameof(ConveyorDirectionIcon));
            }
        }

        private bool _isConveyorReverseActive;
        public bool IsConveyorReverseActive
        {
            get => _isConveyorReverseActive;
            set
            {
                if (SetProperty(ref _isConveyorReverseActive, value))
                {
                    // Refresh visuals when state changes
                    OnPropertyChanged(nameof(ConveyorDirectionText));
                    OnPropertyChanged(nameof(ConveyorDirectionIcon));
                }
            }
        }

        public string ConveyorDirectionText
        {
            get
            {
                bool isLeft = IsConveyorReverseActive ^ SwitchDirection;
                return isLeft ? "Dir: Left" : "Dir: Right";
            }
        }

        public string ConveyorDirectionIcon
        {
            get
            {
                bool isLeft = IsConveyorReverseActive ^ SwitchDirection;
                // Unicode Arrows: ⬅ (Left), ➡ (Right)
                return isLeft ? "⏪" : "⏩";
            }
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

        private double _limitX_Min = 3.0;
        public double LimitX_Min { get => _limitX_Min; set { SetProperty(ref _limitX_Min, value); UpdateStatusColors(); } }

        private double _limitX_Max = 4.0;
        public double LimitX_Max { get => _limitX_Max; set { SetProperty(ref _limitX_Max, value); UpdateStatusColors(); } }

        private double _limitY_Min = 3.0;
        public double LimitY_Min { get => _limitY_Min; set { SetProperty(ref _limitY_Min, value); UpdateStatusColors(); } }

        private double _limitY_Max = 4.0;
        public double LimitY_Max { get => _limitY_Max; set { SetProperty(ref _limitY_Max, value); UpdateStatusColors(); } }

        private double _limitTheta_Min = 0.0;
        public double LimitTheta_Min { get => _limitTheta_Min; set { SetProperty(ref _limitTheta_Min, value); UpdateStatusColors(); } }

        private double _limitTheta_Max = 0.5;
        public double LimitTheta_Max { get => _limitTheta_Max; set { SetProperty(ref _limitTheta_Max, value); UpdateStatusColors(); } }

        // --- STATUS BRUSHES (For Green/Red text) ---
        private Brush _statusBrushX = Brushes.Gray;
        public Brush StatusBrushX { get => _statusBrushX; set => SetProperty(ref _statusBrushX, value); }

        private Brush _statusBrushY = Brushes.Gray;
        public Brush StatusBrushY { get => _statusBrushY; set => SetProperty(ref _statusBrushY, value); }

        private Brush _statusBrushTheta = Brushes.Gray;
        public Brush StatusBrushTheta { get => _statusBrushTheta; set => SetProperty(ref _statusBrushTheta, value); }

        // --- UPDATED PROPERTIES (Call UpdateStatusColors in setters) ---
       

        // --- LOGIC HELPER ---
        private void UpdateStatusColors()
        {
            // Define your colors (Use DynamicResource logic or hardcoded for now)
            var successBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745")); // Green
            var errorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));   // Red

            // Check X
            StatusBrushX = (LatestX >= LimitX_Min && LatestX <= LimitX_Max) ? successBrush : errorBrush;

            // Check Y
            StatusBrushY = (LatestY >= LimitY_Min && LatestY <= LimitY_Max) ? successBrush : errorBrush;

            // Check Theta
            StatusBrushTheta = (LatestTheta >= LimitTheta_Min && LatestTheta <= LimitTheta_Max) ? successBrush : errorBrush;
        }


        #endregion


        public OEEDashboardViewModel(
            IPLCTagConfigurationService tagService,
            IOptions<CcdSettings> ccdSettng,
            IOptions<ConfigSettings> configSettng,
           IOptionsMonitor<ExternalSettings> settingsMonitor,
            CoreClient coreClient,
            IDialogService dialog,
            ILogConfigurationService logConfigService,
            IProductConfigurationService productService,
            IAppLogger logger) : base(logger)
        {
            var ccd = ccdSettng.Value;
            _settingsMonitor = settingsMonitor;
            _tagService = tagService;
            _coreClient = coreClient;
            _dialog = dialog;
            _productService = productService;
            SwitchDirection = configSettng.Value.SwitchConveyorDirection;
            IsMacMiniEnabled = _settingsMonitor.CurrentValue.IsMacMiniEnabled;

            var prodLogConfigTask = logConfigService.GetByLogTypeAsync(LogType.Production);
            prodLogConfigTask.Wait();
            var prodLogConfig = prodLogConfigTask.Result;
            _prodCsvFolder = prodLogConfig?.DataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            // Path to shared state file
            _jsonStatePath = Path.Combine(ccd.QrCodeImagePath, ccd.CurrentCycleStateFileName);
            // _jsonStatePath = Path.Combine(ConstantValues.QrCodeImagePath, "CurrentCycleState.json");

            // Initialize Lists
            //  InitializeAsync();      // Writable Tags
            InitializeCameraGrid(); // Camera Grid placeholders

            // --- COMMANDS ---
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            ResetCommand = new RelayCommand((StartResetSequence));
            ReverseCommand = new RelayCommand(async () => await ReverseAsync());
            OpenCardDetailCommand = new RelayCommand<string>(OpenCardDetail);
            ShowImageCommand = new RelayCommand<CameraImageItem>(ShowImage);
            ToggleMacMiniCommand = new RelayCommand(ToggleMacMini);
            LoadCycleTimeTrend();
            DummyData();
            // 1. Live Data Timer (1000ms) - Gets OEE, IOs, Status from Core Service via TCP
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(100), LiveDataTimerTick);
            _liveDataTimer.Start();

            // 2. UI Sync Timer (200ms) - Gets Images and Station Data from JSON (Cycle Synced)
            _uiSyncTimer = new SafePoller( TimeSpan.FromMilliseconds(100), UiSyncTick);
            _uiSyncTimer.Start();

            _resetLogicTimer = new SafePoller(TimeSpan.FromMilliseconds(200), ResetSequenceTick);
            _resetLogicTimer.Start();

            _settingsMonitor.OnChange(settings => {
                OnPropertyChanged(nameof(UnitX));
                OnPropertyChanged(nameof(UnitY));
                OnPropertyChanged(nameof(UnitTheta));
            });

            // Force initial sync
            //SyncUiWithJson();
        }

        #region Data Intilization

        private void ToggleMacMini()
        {
            try
            {
                bool confirm = _dialog.ShowYesNo("Do you want to confirm?", "Confirmation");
                if (confirm)
                {
                    bool newState = !IsMacMiniEnabled;

                    // 1. Update Runtime UI immediately
                    IsMacMiniEnabled = newState;
                    OnPropertyChanged(nameof(MacMiniStatusText));
                    OnPropertyChanged(nameof(MacMiniStatusColor));

                    // 2. Write to appsettings.json
                    string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                    var json = File.ReadAllText(appSettingsPath);
                    var jsonObj = JObject.Parse(json);

                    if (jsonObj["External"] != null)
                    {
                        jsonObj["External"]["IsMacMiniEnabled"] = newState;
                        File.WriteAllText(appSettingsPath, jsonObj.ToString());

                        // Log
                        // _logger.LogInfo($"Mac Mini Logic set to: {newState}", LogType.Audit);
                    }

                }
            }
            catch (Exception ex)
            {
                // _logger.LogError($"Failed to update config: {ex.Message}", LogType.Diagnostics);
                // Revert UI if save failed
                IsMacMiniEnabled = !IsMacMiniEnabled;
            }
        }
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
          //  CycleTrend = new List<double> { 2.8, 2.9, 2.7, 3.0, 2.8, 2.9, 2.85, 2.75, 2.9 };
        }

        private async void LoadCycleTimeTrend()
        {
            try
            {
                // Run on background thread to keep UI responsive
                var trendData = await Task.Run(() =>
                {
                    var dailyAverages = new List<double>();
                    var endDate = DateTime.Today;
                    var startDate = endDate.AddDays(-6); // 7 days total including today

                    // Iterate from oldest date to today
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        var filePath = Path.Combine(_prodCsvFolder, $"Production_{date:yyyyMMdd}.csv");

                        if (!File.Exists(filePath))
                        {
                            dailyAverages.Add(0); // No data for this day
                            continue;
                        }

                        try
                        {
                            // Read specific file for this single day
                            var values = new List<double>();
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var sr = new StreamReader(fs))
                            {
                                string headerLine = sr.ReadLine();
                                if (!string.IsNullOrEmpty(headerLine))
                                {
                                    var headers = headerLine.Split(',').Select(h => h.Trim()).ToArray();
                                    int colIndex = Array.IndexOf(headers, "CT"); // Looking for CT column

                                    if (colIndex >= 0)
                                    {
                                        while (!sr.EndOfStream)
                                        {
                                            var line = sr.ReadLine();
                                            if (string.IsNullOrEmpty(line)) continue;

                                            var parts = line.Split(',');
                                            if (parts.Length > colIndex && double.TryParse(parts[colIndex], out double val))
                                            {
                                                // Optional: Convert ms to seconds if needed
                                                // val = val / 1000.0; 
                                                values.Add(val);
                                            }
                                        }
                                    }
                                }
                            }

                            if (values.Any())
                            {
                                // 1. Get Average
                                double avg = values.Average();

                                // 2. Divide by 100
                                avg = avg / 100.0;

                                // 3. Round to 1 decimal place (F1 equivalent for double) and add
                                dailyAverages.Add(Math.Round(avg, 1));
                            }
                            else
                            {
                                dailyAverages.Add(0);
                            }
                        }
                        catch
                        {
                            dailyAverages.Add(0); // Error reading file
                        }
                    }
                    return dailyAverages;
                });

                // Update UI
                CycleTrend = trendData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading cycle trend: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void InitializeCameraGrid()
        {
            try
            {
                // Populate the collection with 12 empty items
                var prodConfig = await _productService.LoadAsync();

                // 2. Set Grid Dimensions
                DynamicRows = prodConfig.GridRows > 0 ? prodConfig.GridRows : 4;
                DynamicColumns = prodConfig.GridColumns > 0 ? prodConfig.GridColumns : 3;
                int totalCells = DynamicRows * DynamicColumns;
                int activeItems = prodConfig.TotalItems;
                CameraImages.Clear();
                for (int i = 0; i < totalCells; i++)
                {
                    int stationId = i + 1;
                    CameraImages.Add(new CameraImageItem
                    {
                        StationNumber = stationId,
                        Result = "Unchecked",
                        IsEnabled = (i < activeItems),
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


        private async Task ResetSequenceTick()
        {
            // Prevent re-entry if the previous tick is still processing (e.g. slow network)
            if (Interlocked.Exchange(ref _resetTimerRunning, 1) == 1) return;

            try
            {
                // Only run logic if we are NOT Idle
                if (_resetState == ResetSequenceState.Idle) return;

                switch (_resetState)
                {
                    case ResetSequenceState.TriggerReset:
                        // Step A: Write B26 (Cycle Start) = TRUE
                        await _coreClient.WriteTagAsync(ConstantValues.RESET_TAG_ID, true);

                        _logger.LogInfo("State: TriggerReset -> Set B26 TRUE. Moving to Waiting.", LogType.Audit);

                        // Start Timeout Timer
                        _resetTimeoutStart = DateTime.Now;
                        _resetState = ResetSequenceState.WaitingForAck;
                        break;

                    case ResetSequenceState.WaitingForAck:
                        // Step B: Read Packet 5 to check for A27 (Ack)
                        var ackData = await _coreClient.GetIoValuesAsync(5);

                        bool ackReceived = false;
                        if (ackData != null && GetBoolState(ackData, ConstantValues.RESET_ACK_TAG_ID))
                        {
                            ackReceived = true;
                        }

                        if (ackReceived)
                        {
                            _logger.LogInfo("State: Waiting -> Ack (A27) Received! Finishing.", LogType.Audit);

                            // Step C: Write B26 = FALSE
                            await _coreClient.WriteTagAsync(ConstantValues.RESET_TAG_ID, false);

                            // Success UI Feedback
                            _dialog.ShowMessage("Cycle Reset Successful.");
                            _resetState = ResetSequenceState.Idle;
                        }
                       
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Reset Timer Error: {ex.Message}", LogType.Diagnostics);
                // Reset state on crash so user can try again
                _resetState = ResetSequenceState.Idle;
            }
            finally
            {
                Interlocked.Exchange(ref _resetTimerRunning, 0);
            }
        }

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
                            LatestX = oeeResult.XValue;
                            LatestY = oeeResult.YValue;
                            LatestTheta = oeeResult.AngleValue; // Assuming Z maps to Theta
                            LimitX_Min = oeeResult.MinX;
                            LimitX_Max = oeeResult.MaxX;
                            LimitY_Min = oeeResult.MinY;
                            LimitY_Max = oeeResult.MaxY;
                            LimitTheta_Min = oeeResult.MinZ;
                            LimitTheta_Max = oeeResult.MaxZ;

                        }
                    }
                }
                else
                {
                    _logger.LogWarning("LiveDataTimerTick: GetIoValuesAsync timed out.", LogType.Diagnostics);
                }

                var getData = await _coreClient.GetIoValuesAsync(5);

                if (getData.TryGetValue(ConstantValues.REVERSE_ACK_TAG_ID, out object revObj))
                {

                    bool newState = false;
                    if (revObj is bool b) newState = b;
                    else if (revObj is int i) newState = i > 0;

                    // Only update if changed to avoid UI flicker/overhead
                    if (IsConveyorReverseActive != newState)
                    {
                        // Dispatch to UI thread since properties affect UI
                        Application.Current.Dispatcher.Invoke(() => IsConveyorReverseActive = newState);
                    }
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

        private async Task ReverseAsync()
        {
            try
            {
                bool confirm = _dialog.ShowYesNo("Are you sure you want to change conveyor direction?", "Confirmation");
                if (confirm)
                {
                    // Toggle the current state
                    bool newState = !IsConveyorReverseActive;

                    _logger.LogInfo($"User toggling Conveyor Direction to: {newState}", LogType.Audit);

                    // Write to PLC
                    await _coreClient.WriteTagAsync(ConstantValues.REVERSE_TAG_ID, newState);

                    // Optional: Optimistic UI update (makes button feel instant)
                    IsConveyorReverseActive = newState;

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Reverse Command Error: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning("Failed to switch direction.");
            }
        }

        private void StartResetSequence()
        {
            bool confirm = _dialog.ShowYesNo("Are you sure you want to reset counter?", "Confirmation");
            if (confirm)
            {
                if (_resetState == ResetSequenceState.Idle)
                {
                    _logger.LogInfo("Reset Sequence Initiated by User.", LogType.Audit);
                    _resetState = ResetSequenceState.TriggerReset;
                }
                else
                {
                    _logger.LogWarning("Reset already in progress.", LogType.Audit);
                }
            }
        }

        private bool GetBoolState(Dictionary<int, object> tagValues, int tagId)
        {
            if (tagValues != null && tagValues.TryGetValue(tagId, out object val))
            {
                if (val is bool bVal) return bVal;
                if (val is int iVal) return iVal > 0;
                // Optional: Handle string "true"/"1" if needed
                if (val is string sVal)
                {
                    return sVal.Equals("1") || sVal.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        private enum ResetSequenceState
        {
            Idle,
            TriggerReset,
            WaitingForAck,
            Complete
        }

        private ResetSequenceState _resetState = ResetSequenceState.Idle;
        private DateTime _resetTimeoutStart;

  
        private void ShowImage(CameraImageItem img)
        {
            try
            {
                if (img == null) return;
                string title = $"INSPECTION POSITION {img.StationNumber}"; // Use StationNumber or Id
                var window = new FullImageView(img, title,
             LimitX_Min,  LimitX_Max,
             LimitY_Min,  LimitY_Max,
            _limitTheta_Min,  _limitTheta_Max, UnitX, UnitY,
            UnitTheta);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        private async void OpenCardDetail(string cardType)
        {
            try
            {
                string title = "";
                var data = new List<MetricDetailItem>();

                // Show a loading indicator if you have one, or just wait
                // Ideally, perform calculations then show window

                // Tuple Helpers for cleaner code
                (string T, string W, string M) stats;

                switch (cardType)
                {
                    case "Efficiency":
                        title = "Efficiency Breakdown Details";
                        // Availability
                        stats = await GetMetricStats("Availability", AggregationType.Average, "%");
                        data.Add(new MetricDetailItem { MetricName = "Availability", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });

                        // Performance
                        stats = await GetMetricStats("Performance", AggregationType.Average, "%");
                        data.Add(new MetricDetailItem { MetricName = "Performance", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });

                        // Quality
                        stats = await GetMetricStats("Quality", AggregationType.Average, "%");
                        data.Add(new MetricDetailItem { MetricName = "Quality", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;

                    case "OEE":
                        title = "OEE Score Statistics";
                        stats = await GetMetricStats("OEE", AggregationType.Average, "%");
                        data.Add(new MetricDetailItem { MetricName = "OEE Score", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;

                    case "CycleTime":
                    case "AvgCycle":
                        title = "Cycle Time Trends";
                        // Assuming CT column is in milliseconds or seconds. Sample said "345". 
                        // If 345 is ms, result is small. If 345 is ms, dividing by 1000 inside aggregate might be needed or handled here.
                        // For now assuming the raw value is what we want to average.
                        stats = await GetMetricStats("CT", AggregationType.Average, "s");
                        data.Add(new MetricDetailItem { MetricName = "Avg Cycle", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;

                    case "OperatingTime":
                        title = "Operating Time";
                        // Assuming Uptime column exists and is in Seconds
                        stats = await GetMetricStats("Uptime", AggregationType.TimeSum);
                        data.Add(new MetricDetailItem { MetricName = "Operating Time", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;

                    case "Downtime":
                        title = "Downtime Statistics";
                        // Assuming Downtime column exists and is in Seconds
                        stats = await GetMetricStats("Downtime", AggregationType.TimeSum);
                        data.Add(new MetricDetailItem { MetricName = "Total Stop", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;

                    case "InFlow":
                        title = "Input Statistics";
                        stats = await GetMetricStats("Total_IN", AggregationType.Sum);
                        data.Add(new MetricDetailItem { MetricName = "Total Input", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;

                    case "OK":
                        title = "Production Quality (OK)";
                        stats = await GetMetricStats("OK", AggregationType.Sum);
                        data.Add(new MetricDetailItem { MetricName = "Good Units", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;

                    case "NG":
                        title = "Rejection Statistics (NG)";
                        stats = await GetMetricStats("NG", AggregationType.Sum);
                        data.Add(new MetricDetailItem { MetricName = "Total Rejects", CurrentVal = stats.T, WeeklyVal = stats.W, MonthlyVal = stats.M });
                        break;
                }

                if (data.Count > 0)
                {
                    // Must execute UI updates on Dispatcher
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var win = new DashboardDetailWindow();
                        win.DataContext = new DashboardDetailViewModel(win, title, data);
                        win.ShowDialog();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        //private void OpenCardDetail(string cardType)
        //{
        //    try
        //    {
        //        string title = "";
        //        var data = new List<MetricDetailItem>();

        //        switch (cardType)
        //        {
        //            case "Efficiency":
        //                title = "Efficiency Breakdown Details";
        //                data.Add(new MetricDetailItem { MetricName = "Availability", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });
        //                data.Add(new MetricDetailItem { MetricName = "Performance", CurrentVal = "0%", WeeklyVal = "0%D", MonthlyVal = "0%" });
        //                data.Add(new MetricDetailItem { MetricName = "Quality", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });
        //                break;

        //            case "OEE":
        //                title = "OEE Score Statistics";
        //                data.Add(new MetricDetailItem { MetricName = "OEE Score", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });

        //                break;

        //            case "CycleTime":
        //                title = "Cycle Time Trends";
        //                data.Add(new MetricDetailItem { MetricName = "Avg Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });
        //                data.Add(new MetricDetailItem { MetricName = "Ideal Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });
        //                break;
        //            case "OperatingTime":
        //                title = "Operating Time";
        //                data.Add(new MetricDetailItem { MetricName = "Operating Time", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
        //                break;

        //            case "Downtime":
        //                title = "Downtime Statistics";
        //                data.Add(new MetricDetailItem { MetricName = "Total Stop", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
        //                data.Add(new MetricDetailItem { MetricName = "Minor Stops", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
        //                data.Add(new MetricDetailItem { MetricName = "Changeover", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
        //                break;

        //            case "AvgCycle": // Or "CycleTime" depending on your CommandParameter
        //                title = "Cycle Time Metrics";
        //                data.Add(new MetricDetailItem { MetricName = "Actual Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });
        //                data.Add(new MetricDetailItem { MetricName = "Ideal Cycle", CurrentVal = "0s", WeeklyVal = "0s", MonthlyVal = "0s" });

        //                break;

        //            case "InFlow":
        //                title = "Input Statistics";
        //                data.Add(new MetricDetailItem { MetricName = "Total Input", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
        //                break;

        //            case "OK":
        //                title = "Production Quality (OK)";
        //                data.Add(new MetricDetailItem { MetricName = "Good Units", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
        //                data.Add(new MetricDetailItem { MetricName = "Yield Rate", CurrentVal = "0%", WeeklyVal = "0%", MonthlyVal = "0%" });
        //                break;

        //            case "NG":
        //                title = "Rejection Statistics (NG)";
        //                data.Add(new MetricDetailItem { MetricName = "Total Rejects", CurrentVal = "0", WeeklyVal = "0", MonthlyVal = "0" });
        //                break;

        //                // Add more cases for "OperatingTime", "Downtime", "InFlow", etc.
        //        }

        //        if (data.Count > 0)
        //        {
        //            // Open the Light Theme Popup
        //            var win = new DashboardDetailWindow();
        //            win.DataContext = new DashboardDetailViewModel(win, title, data);
        //            win.ShowDialog();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.Message, LogType.Diagnostics);
        //    }
        //}

        #endregion

        #region Historical Data Logic

        private enum AggregationType { Sum, Average, TimeSum }

        /// <summary>
        /// Calculates Today, Weekly (Last 7 days excl today), and Monthly (Last 30 days excl today)
        /// </summary>
        private async Task<(string Today, string Weekly, string Monthly)> GetMetricStats(string csvColumnName, AggregationType type, string unit = "")
        {
            return await Task.Run(() =>
            {
                var todayDate = DateTime.Today;

                // 1. Get Data Points
                var todayValues = ReadColumnValues(csvColumnName, todayDate, todayDate);
                var weeklyValues = ReadColumnValues(csvColumnName, todayDate.AddDays(-7), todayDate.AddDays(-1));
                var monthlyValues = ReadColumnValues(csvColumnName, todayDate.AddDays(-30), todayDate.AddDays(-1));

                // 2. Local Helper for Aggregation
                string Aggregate(List<double> values)
                {
                    if (values == null || !values.Any()) return type == AggregationType.Average ? "0" + unit : (type == AggregationType.TimeSum ? "00:00:00" : "0");

                    double result = 0;
                    switch (type)
                    {
                        case AggregationType.Sum:
                            result = values.Sum();
                            return $"{result:N0}{unit}"; // 1,234

                        case AggregationType.Average:
                            result = values.Average();
                            if (unit.Equals("s", StringComparison.OrdinalIgnoreCase))
                            result = result / 100.0;
                            // If unit is %, multiply by 100 if raw data is 0.0-1.0
                            if (unit == "%" && result <= 1.0 && result > 0) result *= 100;
                            return $"{result:F2}{unit}";

                        case AggregationType.TimeSum:
                            result = values.Sum(); // Assumes Seconds
                            var t = TimeSpan.FromSeconds(result);
                            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
                    }
                    return "-";
                }

                return (Aggregate(todayValues), Aggregate(weeklyValues), Aggregate(monthlyValues));
            });
        }

        private List<double> ReadColumnValues(string columnName, DateTime from, DateTime to)
        {
            var results = new List<double>();

            // Iterate dates
            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                var filePath = Path.Combine(_prodCsvFolder, $"Production_{date:yyyyMMdd}.csv");
                if (!File.Exists(filePath)) continue;

                try
                {
                    // Use FileShare to avoid locking issues if file is being written to
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string headerLine = sr.ReadLine();
                        if (string.IsNullOrEmpty(headerLine)) continue;

                        var headers = headerLine.Split(',').Select(h => h.Trim()).ToArray();
                        int colIndex = Array.IndexOf(headers, columnName);

                        if (colIndex == -1) continue; // Column not found

                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            if (string.IsNullOrEmpty(line)) continue;

                            var parts = line.Split(',');
                            if (parts.Length > colIndex)
                            {
                                if (double.TryParse(parts[colIndex], out double val))
                                {
                                    results.Add(val);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading CSV: {ex.Message}");
                }
            }
            return results;
        }

        #endregion


        #region Dispose
        public void Dispose()
        {
            try
            {
                _liveDataTimer.Dispose();

                _uiSyncTimer.Dispose();
                _resetLogicTimer.Dispose(); 

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        #endregion


    }
}
