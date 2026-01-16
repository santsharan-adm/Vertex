using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
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

        public AeLimitViewModel(IAeLimitService aeLimitService, IAppLogger logger) : base(logger)
        {
            _aeLimitService = aeLimitService;
            RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !_isBusy);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !_isBusy && _settings != null);
            _ = LoadAsync();
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
}
