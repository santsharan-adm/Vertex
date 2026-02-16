using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq; // For JObject
using System;
using System.Collections.ObjectModel;
using System.IO; // For File
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AeLimitViewModel : BaseViewModel, IDisposable
    {
        private readonly IAeLimitService _aeLimitService;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;
        private readonly SafePoller _liveDataTimer;
        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;

        private AeLimitSettings _settings;
        private bool _isBusy;
        private readonly string _appSettingsPath; // For saving units

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand SaveCommand { get; }

        // Parameters (Live Limits)
        public AeLimitParameterItem MinX { get; }
        public AeLimitParameterItem MaxX { get; }
        public AeLimitParameterItem MinY { get; }
        public AeLimitParameterItem MaxY { get; }
        public AeLimitParameterItem MinZ { get; }
        public AeLimitParameterItem MaxZ { get; }

        private readonly AeLimitParameterItem[] _allLiveParams;

        // Collection to hold station data structure for JSON saving logic
        public ObservableCollection<AeLimitStationConfig> Stations { get; } = new();

        // Configurable Units (Editable)
        private string _unitX;
        public string UnitX { get => _unitX; set => SetProperty(ref _unitX, value); }

        private string _unitY;
        public string UnitY { get => _unitY; set => SetProperty(ref _unitY, value); }

        private string _unitAngle;
        public string UnitAngle { get => _unitAngle; set => SetProperty(ref _unitAngle, value); }

        public AeLimitViewModel(
            IAeLimitService aeLimitService,
            CoreClient coreClient,
            IDialogService dialog,
            IOptionsMonitor<ExternalSettings> settingsMonitor,
            IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
            _dialog = dialog;
            _aeLimitService = aeLimitService;
            _settingsMonitor = settingsMonitor;

            _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            // Initialize Parameters using ConstantValues tags
            MinX = CreateParam("Min X", ConstantValues.MIN_X);
            MaxX = CreateParam("Max X", ConstantValues.MAX_X);
            MinY = CreateParam("Min Y", ConstantValues.MIN_Y);
            MaxY = CreateParam("Max Y", ConstantValues.MAX_Y);
            MinZ = CreateParam("Min Angle", ConstantValues.MIN_Z);
            MaxZ = CreateParam("Max Angle", ConstantValues.MAX_Z);

            _allLiveParams = new[] { MinX, MaxX, MinY, MaxY, MinZ, MaxZ };

            RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !_isBusy);

            // Save Command triggers bulk write + handshake + Unit Save
            SaveCommand = new RelayCommand(async () => await SaveAndTransferAsync(), () => !_isBusy);

            // Start Live Polling (Every 200ms) for PLC Feedback values
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(200), OnLiveDataTick);
            _liveDataTimer.Start();

            // Initial Load
            _ = LoadAsync();
        }

        private AeLimitParameterItem CreateParam(string name, TagPair tagPair)
        {
            return new AeLimitParameterItem
            {
                Name = name,
                ReadTagId = tagPair.Read,
                WriteTagId = tagPair.Write
            };
        }

        private async Task OnLiveDataTick()
        {
            try
            {
                // Read values to show "Min (Live)" column
                var data = await _coreClient.GetIoValuesAsync(5);
                if (data != null)
                {
                    foreach (var param in _allLiveParams)
                    {
                        if (data.TryGetValue(param.ReadTagId, out object val))
                        {
                            param.CurrentValue = Convert.ToDouble(val);
                        }
                    }
                }
            }
            catch { }
        }

        private async Task SaveAndTransferAsync()
        {
            if (_isBusy) return;
            _isBusy = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                // --- 1. Save Unit Settings to appsettings.json ---
                try
                {
                    var json = File.ReadAllText(_appSettingsPath);
                    var jsonObj = JObject.Parse(json);
                    if (jsonObj["External"] == null) jsonObj["External"] = new JObject();

                    var ext = jsonObj["External"];
                    ext["InspectionXUnit"] = UnitX;
                    ext["InspectionYUnit"] = UnitY;
                    ext["InspectionAngleUnit"] = UnitAngle;

                    File.WriteAllText(_appSettingsPath, jsonObj.ToString());
                    _logger.LogInfo("[AE UI] Units saved to appsettings.json.", LogType.Audit);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[AE UI] Failed to save Units: {ex.Message}", LogType.Diagnostics);
                }

                // --- 2. Save Limits to AELimit.json ---
                _settings = await _aeLimitService.GetSettingsAsync();
                if (_settings == null) _settings = new AeLimitSettings();

                // Sync UI Limit values into JSON model (global update for all stations)
                if (_settings.Stations != null)
                {
                    foreach (var station in _settings.Stations)
                    {
                        station.InspectionX.Lower = MinX.NewValue;
                        station.InspectionX.Upper = MaxX.NewValue;
                        station.InspectionX.Unit = UnitX; // Also sync unit to internal JSON

                        station.InspectionY.Lower = MinY.NewValue;
                        station.InspectionY.Upper = MaxY.NewValue;
                        station.InspectionY.Unit = UnitY;

                        station.InspectionAngle.Lower = MinZ.NewValue;
                        station.InspectionAngle.Upper = MaxZ.NewValue;
                        station.InspectionAngle.Unit = UnitAngle;
                    }
                }

                await _aeLimitService.SaveSettingsAsync(_settings);
                _logger.LogInfo("[AE UI] Limits saved to JSON.", LogType.Audit);

                // --- 3. Write All Parameters to PLC (NewValues) ---
                _logger.LogInfo("[AE UI] Transferring parameters to PLC...", LogType.Audit);
                bool allWritesSuccess = true;

                foreach (var param in _allLiveParams)
                {
                    bool success = await _coreClient.WriteTagAsync(param.WriteTagId, param.NewValue);
                    if (!success) allWritesSuccess = false;
                }

                if (!allWritesSuccess)
                {
                    _dialog.ShowWarning("Failed to write some parameters to PLC. Aborting handshake.");
                    return;
                }

                // --- 4. Handshake Logic ---
                // Set Transfer Start (DM10301.0) -> 1
                _logger.LogInfo("[AE UI] Setting Transfer Start...", LogType.Audit);
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 1);

                // Wait for Confirmation (DM10480.0)
                bool transferComplete = await WaitForPlcConfirmationAsync();

                // Reset Start Bit -> 0
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 0);

                if (transferComplete)
                {
                    _logger.LogInfo("[AE UI] PLC Confirmation Received.", LogType.Audit);
                    _dialog.ShowMessage("Limits Saved & Transferred Successfully!");
                }
                else
                {
                    _logger.LogWarning("[AE UI] PLC Transfer Timeout.", LogType.Diagnostics);
                    _dialog.ShowWarning("Settings Saved, but PLC Confirmation timed out.\nPlease check PLC status.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE UI] Save/Transfer Failed: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning("An error occurred during save.");
            }
            finally
            {
                _isBusy = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task<bool> WaitForPlcConfirmationAsync()
        {
            int timeoutMs = 5000; // 5 Seconds Timeout
            int delayMs = 200;
            int elapsed = 0;

            while (elapsed < timeoutMs)
            {
                var data = await _coreClient.GetIoValuesAsync(5);
                if (data != null && data.TryGetValue(ConstantValues.ACK_LIMIT.Read, out object val))
                {
                    bool isComplete = false;
                    if (val is bool bVal) isComplete = bVal;
                    else if (val is int iVal) isComplete = (iVal > 0);

                    if (isComplete) return true;
                }
                await Task.Delay(delayMs);
                elapsed += delayMs;
            }
            return false;
        }

        private async Task LoadAsync()
        {
            if (_isBusy) return;
            _isBusy = true;
            try
            {
                // Load Units from Config
                var extConfig = _settingsMonitor.CurrentValue;
                UnitX = !string.IsNullOrEmpty(extConfig.InspectionXUnit) ? extConfig.InspectionXUnit : "mm";
                UnitY = !string.IsNullOrEmpty(extConfig.InspectionYUnit) ? extConfig.InspectionYUnit : "mm";
                UnitAngle = !string.IsNullOrEmpty(extConfig.InspectionAngleUnit) ? extConfig.InspectionAngleUnit : "deg";

                // Load Limits
                _settings = await _aeLimitService.GetSettingsAsync();

                // Populate "NewValue" boxes with values from JSON (using Station 0 as reference)
                if (_settings?.Stations != null && _settings.Stations.Count > 0)
                {
                    var refStation = _settings.Stations[0];
                    MinX.NewValue = refStation.InspectionX.Lower;
                    MaxX.NewValue = refStation.InspectionX.Upper;
                    MinY.NewValue = refStation.InspectionY.Lower;
                    MaxY.NewValue = refStation.InspectionY.Upper;
                    MinZ.NewValue = refStation.InspectionAngle.Lower;
                    MaxZ.NewValue = refStation.InspectionAngle.Upper;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE UI] Load failed: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                _isBusy = false;
            }
        }

        public void Dispose()
        {
            _liveDataTimer?.Dispose();
        }
    }

    public class AeLimitParameterItem : ObservableObjectVM
    {
        public string Name { get; set; }
        public int ReadTagId { get; set; }
        public int WriteTagId { get; set; }

        private double _currentValue;
        public double CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }

        private double _newValue;
        public double NewValue
        {
            get => _newValue;
            set => SetProperty(ref _newValue, value);
        }
    }
}