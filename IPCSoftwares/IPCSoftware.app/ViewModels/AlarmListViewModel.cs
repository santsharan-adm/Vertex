using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AlarmListViewModel : BaseViewModel
    {
        private readonly IAlarmConfigurationService _alarmService;
        private readonly INavigationService _nav;
        private ObservableCollection<AlarmConfigurationModel> _alarms;
        private ObservableCollection<AlarmConfigurationModel> _filteredAlarms;
        private AlarmConfigurationModel _selectedAlarm;
        private string _filterText;

        public ObservableCollection<AlarmConfigurationModel> Alarms
        {
            get => _alarms;
            set => SetProperty(ref _alarms, value);
        }

        public ObservableCollection<AlarmConfigurationModel> FilteredAlarms
        {
            get => _filteredAlarms;
            set => SetProperty(ref _filteredAlarms, value);
        }

        public AlarmConfigurationModel SelectedAlarm
        {
            get => _selectedAlarm;
            set => SetProperty(ref _selectedAlarm, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ICommand AddAlarmCommand { get; }        // NEW
        public ICommand EditAlarmCommand { get; }
        public ICommand DeleteAlarmCommand { get; }
        public ICommand AcknowledgeAlarmCommand { get; }

        public AlarmListViewModel(IAlarmConfigurationService alarmService, INavigationService nav)
        {
            _alarmService = alarmService;
            _nav = nav;
            Alarms = new ObservableCollection<AlarmConfigurationModel>();
            FilteredAlarms = new ObservableCollection<AlarmConfigurationModel>();

            AddAlarmCommand = new RelayCommand(OnAddAlarm);               // NEW
            EditAlarmCommand = new RelayCommand<AlarmConfigurationModel>(OnEditAlarm);
            DeleteAlarmCommand = new RelayCommand<AlarmConfigurationModel>(OnDeleteAlarm);
            //AcknowledgeAlarmCommand = new RelayCommand(OnAcknowledgeAlarm, CanAcknowledgeAlarm);

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            var alarms = await _alarmService.GetAllAlarmsAsync();
            Alarms.Clear();
            FilteredAlarms.Clear();

            foreach (var alarm in alarms)
            {
                Alarms.Add(alarm);
                FilteredAlarms.Add(alarm);
            }
        }

        private void ApplyFilter()
        {
            FilteredAlarms.Clear();

            if (string.IsNullOrWhiteSpace(FilterText))
            {
                foreach (var alarm in Alarms)
                {
                    FilteredAlarms.Add(alarm);
                }
            }
            else
            {
                var filtered = Alarms.Where(a =>
                    a.AlarmNo.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    (a.AlarmName?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.AlarmText?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Severity?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false)
                );

                foreach (var alarm in filtered)
                {
                    FilteredAlarms.Add(alarm);
                }
            }
        }

        // NEW - Add Alarm
        private void OnAddAlarm()
        {
            _nav.NavigateToAlarmConfiguration(null, async () =>
            {
                await LoadDataAsync();
            });
        }

        private void OnEditAlarm(AlarmConfigurationModel alarm)
        {
            if (alarm == null) return;

            _nav.NavigateToAlarmConfiguration(alarm, async () =>
            {
                await LoadDataAsync();
            });
        }

        private async void OnDeleteAlarm(AlarmConfigurationModel alarm)
        {
            if (alarm == null) return;

            // TODO: Add confirmation dialog
            await _alarmService.DeleteAlarmAsync(alarm.Id);
            await LoadDataAsync();
        }

        private bool CanAcknowledgeAlarm()
        {
            return SelectedAlarm != null;
        }

        private async void OnAcknowledgeAlarm()
        {
            if (SelectedAlarm == null) return;

            await _alarmService.AcknowledgeAlarmAsync(SelectedAlarm.Id);
            await LoadDataAsync();
        }
    }
}

