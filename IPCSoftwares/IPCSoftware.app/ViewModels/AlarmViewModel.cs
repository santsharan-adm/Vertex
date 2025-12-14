using IPCSoftware.App;               // Assuming RelayCommand and BaseViewModel are here
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
        // Placeholder: Replace with your actual UI Communication Client Interface
        private readonly CoreClient _coreClient;

        // Data Source for the View
        public ObservableCollection<AlarmInstanceModel> ActiveAlarms { get; } =
            new ObservableCollection<AlarmInstanceModel>();

        // Command for the Acknowledge Button
        public ICommand AcknowledgeCommand { get; }

        public AlarmViewModel(CoreClient coreClient)
        {
            _coreClient = coreClient;
            // ➡️ Uses the specialized command type from RelayCommand.cs
            AcknowledgeCommand = new RelayCommand<AlarmInstanceModel>(ExecuteAcknowledge, CanExecuteAcknowledge);

            // ➡️ Uses the newly defined event in CoreClient
            _coreClient.OnAlarmMessageReceived += HandleIncomingAlarmMessage;
            Task.Run(LoadInitialActiveAlarms);
        }

        // --- Initial Load Logic (If Core Service supports a dump request) ---
        private async Task LoadInitialActiveAlarms()
        {
            // Example: Assume Request ID 8 returns the List<AlarmInstanceModel>
            // var response = await _coreServiceClient.SendRequestAsync(new RequestPackage { RequestId = 8 });
            // ... parse response and populate ActiveAlarms ...
        }

        // --- Alarm Command Logic ---
        private bool CanExecuteAcknowledge(AlarmInstanceModel alarm)
        {
            return alarm != null && alarm.AlarmAckTime == null;
        }

        // Inside AlarmViewModel.cs (UI App)

        private async void ExecuteAcknowledge(object parameter)
        {
            if (parameter is AlarmInstanceModel alarm)
            {
                // Optional: specific user name, or default
                string user = "Operator";

                // Call the CoreClient
                bool success = await _coreClient.AcknowledgeAlarmAsync(alarm.AlarmNo, user);

                if (success)
                {
                    // Update UI immediately (or wait for the Core to send a 'Cleared/Acked' message)
                    // For now, let's update the local model to reflect the ack
                    alarm.AlarmAckTime = DateTime.Now;
                    alarm.AcknowledgedByUser = user;

                    // Force UI refresh if needed (PropertyChange)
                    OnPropertyChanged(nameof(ActiveAlarms));

                    System.Windows.MessageBox.Show($"Alarm {alarm.AlarmNo} Acknowledged!", "Success");
                }
                else
                {
                    System.Windows.MessageBox.Show($"Failed to Acknowledge Alarm {alarm.AlarmNo}", "Error");
                }
            }
        }

        private async Task SendAcknowledgeRequest(AlarmInstanceModel alarmToAck)
        {
            // 3. Calls the corrected Acknowledge method
            await _coreClient.AcknowledgeAlarmAsync(alarmToAck.AlarmNo, Environment.UserName);
            // ... exception handling ...
        }
        // --- Message Handler Logic (Crucial for real-time updates) ---
        private void HandleIncomingAlarmMessage(AlarmMessage message)
        {
            // Always ensure UI updates are on the dispatcher thread in WPF
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show($"[E] ALARM VIEWMODEL HIT. MessageType: {message.MessageType}", "AlarmViewModel Handler");
                var alarmInstance = message.AlarmInstance;
                var existingAlarm = ActiveAlarms.FirstOrDefault(a => a.AlarmNo == alarmInstance.AlarmNo);

                switch (message.MessageType)
                {
                    case AlarmMessageType.Raised:
                        if (existingAlarm == null)
                        {
                            ActiveAlarms.Add(alarmInstance);
                        }
                        break;

                    case AlarmMessageType.Acknowledged:
                        if (existingAlarm != null)
                        {
                            // Update the runtime fields based on the message payload
                            existingAlarm.AlarmAckTime = alarmInstance.AlarmAckTime;
                            existingAlarm.AcknowledgedByUser = alarmInstance.AcknowledgedByUser;
                            // Ensure the DataGrid updates visually (may need INotifyPropertyChanged 
                            // on AlarmInstanceModel or raising a property changed event on the collection).
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