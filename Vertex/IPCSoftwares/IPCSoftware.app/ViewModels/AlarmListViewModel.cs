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

    /// ViewModel responsible for managing the list of alarms.
    /// Handles loading, filtering, adding, editing, deleting, and acknowledging alarms.
    public class AlarmListViewModel : BaseViewModel
    {
        private readonly IAlarmConfigurationService _alarmService;                 // Provides CRUD operations for alarms
        private readonly INavigationService _nav;                                  // Handles navigation between views
        private ObservableCollection<AlarmConfigurationModel> _alarms;            // Complete alarm list
        private ObservableCollection<AlarmConfigurationModel> _filteredAlarms;    // Filtered list displayed in UI
        private AlarmConfigurationModel _selectedAlarm;                           // Currently selected alarm
        private string _filterText;                                               // Search/filter text entered by user


        // -------------------------------Public Bindable Properties --------------------------//

        /// All alarms loaded from the database/service.
        public ObservableCollection<AlarmConfigurationModel> Alarms
        {
            get => _alarms;
            set => SetProperty(ref _alarms, value);
        }

        /// List of alarms shown in the UI after applying search filters.
        public ObservableCollection<AlarmConfigurationModel> FilteredAlarms
        {
            get => _filteredAlarms;
            set => SetProperty(ref _filteredAlarms, value);
        }

        /// Currently selected alarm item in the list.
        public AlarmConfigurationModel SelectedAlarm
        {
            get => _selectedAlarm;
            set => SetProperty(ref _selectedAlarm, value);
        }

        /// Search text used to filter alarms dynamically.
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();                                    // Refresh filtered list whenever text changes
                }
            }
        }

        // ------------------------------- Commands (Bound to Buttons in UI)  ------------------------//

        public ICommand AddAlarmCommand { get; }                   // Opens Add Alarm form
        public ICommand EditAlarmCommand { get; }                  // Opens Edit Alarm form
        public ICommand DeleteAlarmCommand { get; }                // Deletes an alarm
        public ICommand AcknowledgeAlarmCommand { get; }          // Marks an alarm as acknowledged


        // -------------------------------Constructor-----------------------//

        /// Initializes the ViewModel, sets up commands, and loads alarms.
        public AlarmListViewModel(IAlarmConfigurationService alarmService, INavigationService nav)
        {
            _alarmService = alarmService;
            _nav = nav;
            Alarms = new ObservableCollection<AlarmConfigurationModel>();
            FilteredAlarms = new ObservableCollection<AlarmConfigurationModel>();

            // Command bindings
            AddAlarmCommand = new RelayCommand(OnAddAlarm);               // NEW
            EditAlarmCommand = new RelayCommand<AlarmConfigurationModel>(OnEditAlarm);
            DeleteAlarmCommand = new RelayCommand<AlarmConfigurationModel>(OnDeleteAlarm);
            //AcknowledgeAlarmCommand = new RelayCommand(OnAcknowledgeAlarm, CanAcknowledgeAlarm);

            // Load initial data
            _ = LoadDataAsync();
        }


        // ------------------------------- Data Loading and Filtering-------------------------//
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

        /// Filters alarms based on user-entered text (AlarmNo, Name, Text, or Severity).
        private void ApplyFilter()
        {
            FilteredAlarms.Clear();

            // If no filter text, show all alarms
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                foreach (var alarm in Alarms)
                {
                    FilteredAlarms.Add(alarm);
                }
            }
            else
            {
                // Case-insensitive matching on multiple fields
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

        // ------------------------------- Comman-------------------------------//

        /// Navigates to the Alarm Configuration screen to add a new alarm.
        /// After saving, reloads the alarm list.
        private void OnAddAlarm()
        {
            _nav.NavigateToAlarmConfiguration(null, async () =>
            {
                await LoadDataAsync();
            });
        }

        /// Navigates to the Alarm Configuration screen to edit the selected alarm.
        /// After saving, reloads the alarm list.
        private void OnEditAlarm(AlarmConfigurationModel alarm)
        {
            if (alarm == null) return;

            _nav.NavigateToAlarmConfiguration(alarm, async () =>
            {
                await LoadDataAsync();
            });
        }

        /// Deletes the selected alarm after user confirmation.
        /// Then reloads the list to reflect changes.
        private async void OnDeleteAlarm(AlarmConfigurationModel alarm)
        {
            if (alarm == null) return;

            // TODO: Add confirmation dialog
            await _alarmService.DeleteAlarmAsync(alarm.Id);
            await LoadDataAsync();
        }

        /// Determines whether the "Acknowledge" button should be enabled.

        private bool CanAcknowledgeAlarm()
        {
            return SelectedAlarm != null;
        }

        /// Marks the selected alarm as acknowledged in the system.
        private async void OnAcknowledgeAlarm()
        {
            if (SelectedAlarm == null) return;

            await _alarmService.AcknowledgeAlarmAsync(SelectedAlarm.Id);
            await LoadDataAsync();
        }
    }
}

