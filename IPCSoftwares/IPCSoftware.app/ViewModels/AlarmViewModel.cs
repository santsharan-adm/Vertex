//using IPCSoftware.App;               // Assuming RelayCommand and BaseViewModel are here
//using IPCSoftware.App.Services;
//using IPCSoftware.App.Services.UI;
//using IPCSoftware.Shared;
//using IPCSoftware.Shared.Models.ConfigModels; // For AlarmInstanceModel
//using IPCSoftware.Shared.Models.Messaging;   // For AlarmMessage, AlarmMessageType
//using System;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows;                      // Required for Application.Current.Dispatcher
//using System.Windows.Input;

//namespace IPCSoftware.App.ViewModels
//{
//    public class AlarmViewModel : BaseViewModel
//    {
//        // Placeholder: Replace with your actual UI Communication Client Interface
//        private readonly CoreClient _coreClient;

//        // Data Source for the View
//        public ObservableCollection<AlarmInstanceModel> ActiveAlarms { get; } =
//            new ObservableCollection<AlarmInstanceModel>();

//        // Command for the Acknowledge Button
//        public RelayCommand<AlarmInstanceModel> AcknowledgeCommand { get; }

//        public AlarmViewModel(CoreClient coreClient)
//        {
//            _coreClient = coreClient;
//            // ➡️ Uses the specialized command type from RelayCommand.cs
//            AcknowledgeCommand = new RelayCommand<AlarmInstanceModel>(ExecuteAcknowledge, CanExecuteAcknowledge);

//            // ➡️ Uses the newly defined event in CoreClient
//            _coreClient.OnAlarmMessageReceived += HandleIncomingAlarmMessage;
//            Task.Run(LoadInitialActiveAlarms);
//        }

//        // --- Initial Load Logic (If Core Service supports a dump request) ---
//        private async Task LoadInitialActiveAlarms()
//        {
//            // Example: Assume Request ID 8 returns the List<AlarmInstanceModel>
//            // var response = await _coreServiceClient.SendRequestAsync(new RequestPackage { RequestId = 8 });
//            // ... parse response and populate ActiveAlarms ...
//        }

//        // --- Alarm Command Logic ---
//        private bool CanExecuteAcknowledge(AlarmInstanceModel alarm)
//        {
//            return alarm != null && alarm.AlarmAckTime == null;
//        }

//        // Inside AlarmViewModel.cs (UI App)

//        private async void ExecuteAcknowledge(object parameter)
//        {
//            if (parameter is AlarmInstanceModel alarm)
//            {
//                // Call the reusable helper method
//                await AcknowledgeSingleAlarmAsync(alarm);

//                // Optional: Re-show the MessageBox, but it's often better to rely on UI feedback
//                // System.Windows.MessageBox.Show($"Alarm {alarm.AlarmNo} Acknowledged!", "Success");
//            }
//        }

//        public async Task AcknowledgeSingleAlarmAsync(AlarmInstanceModel alarm)
//        {
//            // 1. Call the CoreClient
//            // Note: We are using Environment.UserName here, which is fine for now (Step 4 is to fix hardcoding)
//            bool success = await _coreClient.AcknowledgeAlarmAsync(alarm.AlarmNo, Environment.UserName);

//            if (success)
//            {
//                // 2. OPTIMISTIC UPDATE: Update UI immediately (Relies on INotifyPropertyChanged on AlarmInstanceModel)
//                Application.Current.Dispatcher.Invoke(() =>
//                {
//                    // Update the local model so the banner and list react instantly
//                    alarm.AlarmAckTime = DateTime.Now;
//                    alarm.AcknowledgedByUser = Environment.UserName;


//                });
//            }
//        }

//        private async Task SendAcknowledgeRequest(AlarmInstanceModel alarmToAck)
//        {
//            // 3. Calls the corrected Acknowledge method
//            await _coreClient.AcknowledgeAlarmAsync(alarmToAck.AlarmNo, Environment.UserName);
//            // ... exception handling ...
//        }
//        // --- Message Handler Logic (Crucial for real-time updates) ---
//        private void HandleIncomingAlarmMessage(AlarmMessage message)
//        {
//            // Always ensure UI updates are on the dispatcher thread in WPF
//            Application.Current.Dispatcher.Invoke(() =>
//            {
//                System.Windows.MessageBox.Show($"[E] ALARM VIEWMODEL HIT. MessageType: {message.MessageType}", "AlarmViewModel Handler");
//                var alarmInstance = message.AlarmInstance;
//                var existingAlarm = ActiveAlarms.FirstOrDefault(a => a.AlarmNo == alarmInstance.AlarmNo);

//                switch (message.MessageType)
//                {
//                    case AlarmMessageType.Raised:
//                        if (existingAlarm == null)
//                        {
//                            ActiveAlarms.Add(alarmInstance);
//                        }
//                        break;

//                    case AlarmMessageType.Acknowledged:
//                        if (existingAlarm != null)
//                        {
//                            // Update the runtime fields based on the message payload
//                            existingAlarm.AlarmAckTime = alarmInstance.AlarmAckTime;
//                            existingAlarm.AcknowledgedByUser = alarmInstance.AcknowledgedByUser;
//                            // Ensure the DataGrid updates visually (may need INotifyPropertyChanged 
//                            // on AlarmInstanceModel or raising a property changed event on the collection).
//                        }
//                        break;

//                    case AlarmMessageType.Cleared:
//                        if (existingAlarm != null)
//                        {
//                            ActiveAlarms.Remove(existingAlarm);
//                        }
//                        break;
//                }
//            });
//        }
//    }
//}

using IPCSoftware.App;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels; // For AlarmInstanceModel
using IPCSoftware.Shared.Models.Messaging;   // For AlarmMessage, AlarmMessageType
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;                      // Required for Application.Current.Dispatcher
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AlarmViewModel : BaseViewModel
    {
        private readonly CoreClient _coreClient;

        // Data Source for the View (e.g., a DataGrid)
        public ObservableCollection<AlarmInstanceModel> ActiveAlarms { get; } =
            new ObservableCollection<AlarmInstanceModel>();

        // Command for the Acknowledge Button
        public RelayCommand<AlarmInstanceModel> AcknowledgeCommand { get; }

        // Placeholder property for the alarm currently shown in the banner (for Option 2 XAML)
        // You would need to update this property whenever the banner content changes.
        private AlarmInstanceModel _currentBannerAlarm;
        public AlarmInstanceModel CurrentBannerAlarm
        {
            get => _currentBannerAlarm;
            set => SetProperty(ref _currentBannerAlarm, value);
        }

        // Placeholder property for the banner message
        public string AlarmBannerTotalMessage { get; set; } // Ensure this uses SetProperty for INPC if it changes

        // Placeholder for the command to close the banner
        public ICommand CloseAlarmBannerCommand { get; }


        public AlarmViewModel(CoreClient coreClient)
        {
            _coreClient = coreClient;

            // Initialize Commands
            CloseAlarmBannerCommand = new RelayCommand(() => {/* Logic to hide the banner */});

            // CONSOLIDATED COMMAND INITIALIZATION
            AcknowledgeCommand = new RelayCommand<AlarmInstanceModel>(
     // Execute method needs to wrap the async Task method
     execute: async (alarm) => await AcknowledgeAlarmRequestAsync(alarm),
     // CanExecute remains the same
     canExecute: CanExecuteAcknowledge
 );

            // Initialize subscriptions and data load
            _coreClient.OnAlarmMessageReceived += HandleIncomingAlarmMessage;
            Task.Run(LoadInitialActiveAlarms);
        }

        // --- Alarm Command CanExecute Logic ---
        /// <summary>
        /// Determines if the Acknowledge command button should be enabled.
        /// </summary>
        /// <param name="alarm">The alarm instance passed from the CommandParameter.</param>
        /// <returns>True if the alarm is not null and has not been acknowledged.</returns>
        private bool CanExecuteAcknowledge(AlarmInstanceModel alarm)
        {
            // The button is enabled only if the alarm exists and its AlarmAckTime is null.
            return alarm != null && alarm.AlarmAckTime == null;
        }

        // --- Alarm Command Execute Logic ---
        /// <summary>
        /// Executes the acknowledgement request and updates the UI optimistically.
        /// </summary>
        /// <param name="alarm">The alarm instance to acknowledge.</param>
        private async void ExecuteAcknowledgeAlarm(AlarmInstanceModel alarm)
        {
            if (alarm == null) return;

            // 1. Call the CoreClient to send the acknowledgement
            bool success = await _coreClient.AcknowledgeAlarmAsync(alarm.AlarmNo, Environment.UserName);

            if (success)
            {
                // 2. OPTIMISTIC UI UPDATE (Ensures update is on the UI thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Update the local model. If AlarmInstanceModel implements INPC, 
                    // the UI (like the button) will react immediately and disable.
                    alarm.AlarmAckTime = DateTime.Now;
                    alarm.AcknowledgedByUser = Environment.UserName;
                });
            }
            // Add else block for error handling (e.g., logging or showing a toast notification)
        }

        public async Task AcknowledgeAlarmRequestAsync(AlarmInstanceModel alarm)
        {
            if (alarm == null) return;

            // 1. Call the CoreClient to send the acknowledgement
            bool success = await _coreClient.AcknowledgeAlarmAsync(alarm.AlarmNo, Environment.UserName);

            if (success)
            {
                // 2. OPTIMISTIC UI UPDATE
                Application.Current.Dispatcher.Invoke(() =>
                {
                    alarm.AlarmAckTime = DateTime.Now;
                    alarm.AcknowledgedByUser = Environment.UserName;
                });
            }
        }



        // --- Initial Load Logic (If Core Service supports a dump request) ---
        private async Task LoadInitialActiveAlarms()
        {
            // Example: Assume a data load from core service
            // var response = await _coreServiceClient.SendRequestAsync(new RequestPackage { RequestId = 8 });
            await Task.Delay(100); // Placeholder for async work
        }

        // --- Message Handler Logic (Crucial for real-time updates) ---
        private void HandleIncomingAlarmMessage(AlarmMessage message)
        {
            // Always ensure UI updates are on the dispatcher thread in WPF
            Application.Current.Dispatcher.Invoke(() =>
            {
                // System.Windows.MessageBox.Show($"[E] ALARM VIEWMODEL HIT. MessageType: {message.MessageType}", "AlarmViewModel Handler");

                var alarmInstance = message.AlarmInstance;
                var existingAlarm = ActiveAlarms.FirstOrDefault(a => a.AlarmNo == alarmInstance.AlarmNo);

                switch (message.MessageType)
                {
                    case AlarmMessageType.Raised:
                        if (existingAlarm == null)
                        {
                            ActiveAlarms.Add(alarmInstance);
                            // Also update the banner to show the new, critical alarm
                            CurrentBannerAlarm = alarmInstance;
                        }
                        break;

                    case AlarmMessageType.Acknowledged:
                        if (existingAlarm != null)
                        {
                            // Update the runtime fields based on the message payload (for remote acknowledgements)
                            existingAlarm.AlarmAckTime = alarmInstance.AlarmAckTime;
                            existingAlarm.AcknowledgedByUser = alarmInstance.AcknowledgedByUser;
                        }
                        break;

                    case AlarmMessageType.Cleared:
                        if (existingAlarm != null)
                        {
                            ActiveAlarms.Remove(existingAlarm);
                        }
                        break;
                }
            });
        }
    }
}