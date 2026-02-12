using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AeLimitViewModel : BaseViewModel
    {
        private readonly IAeLimitService _aeLimitService;
        private AeLimitSettings _settings;
        private bool _isBusy;

        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;
        private readonly SafePoller _liveDataTimer;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand SaveCommand { get; }

        // Parameters
        public AeLimitParameterItem MinX { get; }
        public AeLimitParameterItem MaxX { get; }
        public AeLimitParameterItem MinY { get; }
        public AeLimitParameterItem MaxY { get; }
        public AeLimitParameterItem MinZ { get; }
        public AeLimitParameterItem MaxZ { get; }

        private readonly AeLimitParameterItem[] _allLiveParams;

        public ObservableCollection<AeLimitStationConfig> Stations { get; } = new();

        #region Properties
        public string MachineId
        {
            get => _settings?.MachineId;
            set { if (_settings != null) { _settings.MachineId = value; OnPropertyChanged(); } }
        }
        // ... (Other properties like SubmitId, VendorCode etc. assumed unchanged from your snippet)
        public string SubmitId { get => _settings?.SubmitId; set { if (_settings != null) { _settings.SubmitId = value; OnPropertyChanged(); } } }
        public string VendorCode { get => _settings?.VendorCode; set { if (_settings != null) { _settings.VendorCode = value; OnPropertyChanged(); } } }
        public string TossingDefault { get => _settings?.TossingDefault; set { if (_settings != null) { _settings.TossingDefault = value; OnPropertyChanged(); } } }
        public string OperatorIdDefault { get => _settings?.OperatorIdDefault; set { if (_settings != null) { _settings.OperatorIdDefault = value; OnPropertyChanged(); } } }
        public string ModeDefault { get => _settings?.ModeDefault; set { if (_settings != null) { _settings.ModeDefault = value; OnPropertyChanged(); } } }
        public string TestSeriesDefault { get => _settings?.TestSeriesIdDefault; set { if (_settings != null) { _settings.TestSeriesIdDefault = value; OnPropertyChanged(); } } }
        public string PriorityDefault { get => _settings?.PriorityDefault; set { if (_settings != null) { _settings.PriorityDefault = value; OnPropertyChanged(); } } }
        public string OnlineFlag { get => _settings?.OnlineFlagDefault; set { if (_settings != null) { _settings.OnlineFlagDefault = value; OnPropertyChanged(); } } }
        #endregion

        public AeLimitViewModel(IAeLimitService aeLimitService,
              CoreClient coreClient,
              IDialogService dialog, IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
            _dialog = dialog;
            _aeLimitService = aeLimitService;

            // Initialize Parameters
            MinX = CreateParam("Min X", ConstantValues.MIN_X);
            MaxX = CreateParam("Max X", ConstantValues.MAX_X);
            MinY = CreateParam("Min Y", ConstantValues.MIN_Y);
            MaxY = CreateParam("Max Y", ConstantValues.MAX_Y);
            MinZ = CreateParam("Min Angle", ConstantValues.MIN_Z);
            MaxZ = CreateParam("Max Angle", ConstantValues.MAX_Z);
            _allLiveParams = new[] { MinX, MaxX, MinY, MaxY, MinZ, MaxZ };

            RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !_isBusy);

            // Save Command now triggers the bulk write + handshake logic
            SaveCommand = new RelayCommand(async () => await SaveAndTransferAsync(), () => !_isBusy && _settings != null);

            // Start Live Polling (Every 200ms)
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
            if (_isBusy || _settings == null) return;
            _isBusy = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                // 1. Save Settings to JSON
                _settings.Stations = Stations.Select(s => s.Clone()).ToList();
                await _aeLimitService.SaveSettingsAsync(_settings);
                _logger.LogInfo("[AE UI] Configuration saved to JSON.", LogType.Audit);

                // 2. Write All Parameters to PLC (NewValues)
                _logger.LogInfo("[AE UI] Transferring parameters to PLC...", LogType.Audit);
                bool allWritesSuccess = true;

                foreach (var param in _allLiveParams)
                {
                    // Write NewValue to the WriteTagId
                    bool success = await _coreClient.WriteTagAsync(param.WriteTagId, param.NewValue);
                    if (!success) allWritesSuccess = false;
                }

                if (!allWritesSuccess)
                {
                    _dialog.ShowWarning("Failed to write some parameters to PLC. Aborting handshake.");
                    return;
                }

                // 3. Handshake Logic
                // Set DM10301.0 (Start) -> 1
                _logger.LogInfo("[AE UI] Setting Transfer Start (DM10301.0 = 1)...", LogType.Audit);
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 1); // Start Bit

                // 4. Wait for Confirmation (DM10480.0)
                bool transferComplete = await WaitForPlcConfirmationAsync();

                // 5. Reset Start Bit (DM10301.0 = 0)
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 0);

                if (transferComplete)
                {
                    _logger.LogInfo("[AE UI] PLC Confirmation Received (DM10480.0).", LogType.Audit);
                    _dialog.ShowMessage("Settings Saved & Transferred Successfully!");
                }
                else
                {
                    _logger.LogWarning("[AE UI] PLC Transfer Timeout. No Confirmation received.", LogType.Diagnostics);
                    _dialog.ShowWarning("Settings Saved, but PLC Confirmation timed out.\nPlease check PLC status.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE UI] Save/Transfer Failed: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowMessage("An error occurred during save.");
            }
            finally
            {
                _isBusy = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task<bool> WaitForPlcConfirmationAsync()
        {
            // Wait up to 5 seconds for the PLC to set DM10480.0 (Read Tag) to 1
            int timeoutMs = 5000;
            int delayMs = 200;
            int elapsed = 0;

            while (elapsed < timeoutMs)
            {
                // Read fresh data
                var data = await _coreClient.GetIoValuesAsync(5);
                if (data != null && data.TryGetValue(ConstantValues.ACK_LIMIT.Read, out object val))
                {
                    // Assuming ACK_LIMIT.Read maps to DM10480.0
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
                _settings = await _aeLimitService.GetSettingsAsync();
                Stations.Clear();
                if (_settings?.Stations != null)
                {
                    foreach (var station in _settings.Stations.OrderBy(s => s.SequenceIndex))
                    {
                        Stations.Add(station.Clone());
                    }
                }
                // Notify property changes for global settings
                OnPropertyChanged(string.Empty);
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

