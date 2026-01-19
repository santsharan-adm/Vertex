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
        public ICommand WriteParamCommand { get; }
        public ICommand AckSaveCommand { get; }     // Ack Button
        public AeLimitParameterItem MinX { get; }
        public AeLimitParameterItem MaxX { get; }
        public AeLimitParameterItem MinY { get; }
        public AeLimitParameterItem MaxY { get; }
        public AeLimitParameterItem MinZ { get; }
        public AeLimitParameterItem MaxZ { get; }
        private readonly AeLimitParameterItem[] _allLiveParams;

        public ObservableCollection<AeLimitStationConfig> Stations { get; } = new();

        public string MachineId
        {
            get => _settings?.MachineId;
            set { if (_settings != null) { _settings.MachineId = value; OnPropertyChanged(); } }
        }

        public string SubmitId
        {
            get => _settings?.SubmitId;
            set { if (_settings != null) { _settings.SubmitId = value; OnPropertyChanged(); } }
        }

        public string VendorCode
        {
            get => _settings?.VendorCode;
            set { if (_settings != null) { _settings.VendorCode = value; OnPropertyChanged(); } }
        }

        public string TossingDefault
        {
            get => _settings?.TossingDefault;
            set { if (_settings != null) { _settings.TossingDefault = value; OnPropertyChanged(); } }
        }

        public string OperatorIdDefault
        {
            get => _settings?.OperatorIdDefault;
            set { if (_settings != null) { _settings.OperatorIdDefault = value; OnPropertyChanged(); } }
        }

        public string ModeDefault
        {
            get => _settings?.ModeDefault;
            set { if (_settings != null) { _settings.ModeDefault = value; OnPropertyChanged(); } }
        }

        public string TestSeriesDefault
        {
            get => _settings?.TestSeriesIdDefault;
            set { if (_settings != null) { _settings.TestSeriesIdDefault = value; OnPropertyChanged(); } }
        }

        public string PriorityDefault
        {
            get => _settings?.PriorityDefault;
            set { if (_settings != null) { _settings.PriorityDefault = value; OnPropertyChanged(); } }
        }

        public string OnlineFlag
        {
            get => _settings?.OnlineFlagDefault;
            set { if (_settings != null) { _settings.OnlineFlagDefault = value; OnPropertyChanged(); } }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SaveCommand { get; }

        public AeLimitViewModel(IAeLimitService aeLimitService,
              CoreClient coreClient,
            IDialogService dialog, IAppLogger logger) : base(logger)

        {
            _coreClient = coreClient;
            _dialog = dialog;
            _aeLimitService = aeLimitService;
            WriteParamCommand = new RelayCommand<AeLimitParameterItem>(OnWriteParameter);
            RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !_isBusy);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !_isBusy && _settings != null);
            _ = LoadAsync();
            AckSaveCommand = new RelayCommand(async () => await OnAckSave());
            MinX = CreateParam("Min X", ConstantValues.MIN_X);
            MaxX = CreateParam("Max X", ConstantValues.MAX_X);
            MinY = CreateParam("Min Y", ConstantValues.MIN_Y);
            MaxY = CreateParam("Max Y", ConstantValues.MAX_Y);
            MinZ = CreateParam("Min Angle", ConstantValues.MIN_Z);
            MaxZ = CreateParam("Max Angle", ConstantValues.MAX_Z);
            _allLiveParams = new[] { MinX, MaxX, MinY, MaxY, MinZ, MaxZ };

        

            // 2. Start Live Polling (Every 200ms)
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(200), OnLiveDataTick);
            _liveDataTimer.Start();
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
                var data = await _coreClient.GetIoValuesAsync(5); // Adjust ID if needed
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

        private async void OnWriteParameter(AeLimitParameterItem param)
        {
            if (param == null) return;

            bool confirm = _dialog.ShowYesNo($"Update {param.Name} to {param.NewValue}?", "Confirm Update");
            if (!confirm) return;

            try
            {
                _logger.LogInfo($"[AE UI] Writing {param.Name} -> {param.NewValue}", LogType.Audit);

                // Write to the WRITE Tag ID
                bool success = await _coreClient.WriteTagAsync(param.WriteTagId, param.NewValue);

                if (success)
                {
                    _dialog.ShowMessage("Value updated successfully.");
                }
                else
                {
                    _dialog.ShowWarning("Failed to update value. Check logs.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE UI] Write Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task OnAckSave()
        {
            try
            {
                _logger.LogInfo("[AE UI] Sending ACK_LIMIT_WRITE...", LogType.Audit);

                // Pulse Logic: 1 -> Wait -> 0
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT_WRITE, 1);
                await Task.Delay(200);
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT_WRITE, 0);

                _dialog.ShowMessage("Value Saved sucessfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE UI] Ack Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        public void Dispose()
        {
            _liveDataTimer?.Dispose();
        }

        private async Task LoadAsync()
        {
            if (_isBusy) return;
            _isBusy = true;
            try
            {
                _settings = await _aeLimitService.GetSettingsAsync();
                UpdateCollections();
                RaiseGlobals();
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

        private async Task SaveAsync()
        {
            if (_isBusy || _settings == null) return;
            _isBusy = true;
            try
            {
                _settings.Stations = Stations.Select(s => s.Clone()).ToList();
                await _aeLimitService.SaveSettingsAsync(_settings);
                _logger.LogInfo("[AE UI] Settings saved.", LogType.Audit);

                try
                {
                    _logger.LogInfo("[AE UI] Sending ACK_LIMIT_WRITE...", LogType.Audit);

                    // Pulse Logic: 1 -> Wait -> 0
                    await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT_WRITE, 1);
                    await Task.Delay(200);
                    await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT_WRITE, 0);

                    _dialog.ShowMessage("Value Saved sucessfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[AE UI] Ack Error: {ex.Message}", LogType.Diagnostics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE UI] Save failed: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void UpdateCollections()
        {
            Stations.Clear();
            if (_settings?.Stations == null) return;
            foreach (var station in _settings.Stations.OrderBy(s => s.SequenceIndex))
            {
                Stations.Add(station.Clone());
            }
        }

        private void RaiseGlobals()
        {
            OnPropertyChanged(nameof(MachineId));
            OnPropertyChanged(nameof(SubmitId));
            OnPropertyChanged(nameof(VendorCode));
            OnPropertyChanged(nameof(TossingDefault));
            OnPropertyChanged(nameof(OperatorIdDefault));
            OnPropertyChanged(nameof(ModeDefault));
            OnPropertyChanged(nameof(TestSeriesDefault));
            OnPropertyChanged(nameof(PriorityDefault));
            OnPropertyChanged(nameof(OnlineFlag));
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
