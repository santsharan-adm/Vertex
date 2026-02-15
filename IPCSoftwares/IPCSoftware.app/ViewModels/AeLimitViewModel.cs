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
    public class AeLimitViewModel : BaseViewModel, IDisposable
    {
        private readonly IAeLimitService _aeLimitService;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;
        private readonly SafePoller _liveDataTimer;

        private AeLimitSettings _settings;
        private bool _isBusy;

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

        // Collection to hold station data structure for JSON saving
        public ObservableCollection<AeLimitStationConfig> Stations { get; } = new();

        public AeLimitViewModel(
            IAeLimitService aeLimitService,
            CoreClient coreClient,
            IDialogService dialog,
            IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
            _dialog = dialog;
            _aeLimitService = aeLimitService;

            // Initialize Parameters (Tags from ConstantValues)
            MinX = CreateParam("Min X", ConstantValues.MIN_X);
            MaxX = CreateParam("Max X", ConstantValues.MAX_X);
            MinY = CreateParam("Min Y", ConstantValues.MIN_Y);
            MaxY = CreateParam("Max Y", ConstantValues.MAX_Y);
            MinZ = CreateParam("Min Angle", ConstantValues.MIN_Z);
            MaxZ = CreateParam("Max Angle", ConstantValues.MAX_Z);
            _allLiveParams = new[] { MinX, MaxX, MinY, MaxY, MinZ, MaxZ };

            RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !_isBusy);

            // Save Command triggers bulk write + handshake
            SaveCommand = new RelayCommand(async () => await SaveAndTransferAsync(), () => !_isBusy);

            // Start Live Polling (Every 200ms) for PLC Feedback values if needed
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
                // 1. Load latest settings first to ensure we don't overwrite PDCA data
    /*            _settings = await _aeLimitService.GetSettingsAsync();
                if (_settings == null) _settings = new AeLimitSettings();

                // 2. Update Station Limits in the Settings object based on "NewValue"
                // Assuming Station 0 is the one we are editing limits for
                if (_settings.Stations.Count > 0)
                {
                    // Map NewValues to the JSON model if required for persistence
                    // (Assuming you want to save the new limits to JSON as well)
                    // Example: _settings.Stations[0].Limits.MinX = MinX.NewValue; 
                    // This depends on your specific mapping logic between UI Params and JSON Model
                }

                await _aeLimitService.SaveSettingsAsync(_settings);
                _logger.LogInfo("[AE UI] Limits saved to JSON.", LogType.Audit);*/

                // 3. Write All Parameters to PLC (NewValues)
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

                // 4. Handshake Logic
                _logger.LogInfo("[AE UI] Setting Transfer Start (DM10301.0 = 1)...", LogType.Audit);
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 1); // Start Bit

                bool transferComplete = await WaitForPlcConfirmationAsync();

                // Reset Start Bit
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
                _dialog.ShowMessage("An error occurred during save.", "Error");
            }
            finally
            {
                _isBusy = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task<bool> WaitForPlcConfirmationAsync()
        {
            int timeoutMs = 5000;
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
                _settings = await _aeLimitService.GetSettingsAsync();

                // If you need to populate "NewValue" boxes with current JSON values, do it here.
                // For now, assuming NewValue starts at 0 or user entry.
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