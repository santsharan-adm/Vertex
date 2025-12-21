using IPCSoftware.App;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AlarmViewModel : BaseViewModel
    {
        private readonly CoreClient _coreClient;

        // Collection for the DataGrid
        public ObservableCollection<AlarmInstanceModel> ActiveAlarms { get; } =
            new ObservableCollection<AlarmInstanceModel>();

        // Commands
        public RelayCommand<AlarmInstanceModel> AcknowledgeCommand { get; }
        public ICommand GlobalAcknowledgeCommand { get; }
        public ICommand GlobalResetCommand { get; }

        private AlarmInstanceModel _currentBannerAlarm;
        public AlarmInstanceModel CurrentBannerAlarm
        {
            get => _currentBannerAlarm;
            set => SetProperty(ref _currentBannerAlarm, value);
        }

        public AlarmViewModel(CoreClient coreClient, IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;

            // Initialize Global Commands (Tags 38 and 39)
            GlobalAcknowledgeCommand = new RelayCommand(async () => await ExecuteGlobalWrite(38, "Global Acknowledge"));
            GlobalResetCommand = new RelayCommand(async () => await ExecuteGlobalWrite(39, "Global Reset"));

            // Initialize Row-Level Command
            AcknowledgeCommand = new RelayCommand<AlarmInstanceModel>(
                     execute: async (alarm) => await AcknowledgeAlarmRequestAsync(alarm),
                     canExecute: CanExecuteAcknowledge);

            _coreClient.OnAlarmMessageReceived += HandleIncomingAlarmMessage;
            Task.Run(LoadInitialActiveAlarms);
        }

        // Update these methods in AlarmViewModel.cs

        private async Task ExecuteGlobalWrite(int tagId, string actionName)
        {
            try
            {
                // 1. Write the boolean true to the PLC tag
                bool success = await _coreClient.WriteTagAsync(tagId, true);

                if (success)
                {
                    _logger.LogInfo($"{actionName} (Tag {tagId}) triggered successfully.", LogType.Audit);

                    // 2. Update the local UI collection immediately on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var now = DateTime.Now;
                        foreach (var alarm in ActiveAlarms)
                        {
                            if (tagId == 38) // Global Acknowledge logic
                            {
                                if (alarm.AlarmAckTime == null)
                                {
                                    alarm.AlarmAckTime = now;
                                    alarm.AcknowledgedByUser = Environment.UserName;
                                }
                            }
                            else if (tagId == 39) // Global Reset logic
                            {
                                // Update the reset timestamp in the model
                                alarm.AlarmResetTime = now;
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during {actionName}: {ex.Message}", LogType.Diagnostics);
            }
        }

        private bool CanExecuteAcknowledge(AlarmInstanceModel alarm)
        {
            return alarm != null && alarm.AlarmAckTime == null;
        }

        public async Task AcknowledgeAlarmRequestAsync(AlarmInstanceModel alarm)
        {
            try
            {
                if (alarm == null) return;

                bool success = await _coreClient.AcknowledgeAlarmAsync(alarm.AlarmNo, Environment.UserName);
                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        alarm.AlarmAckTime = DateTime.Now;
                        alarm.AcknowledgedByUser = Environment.UserName;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task LoadInitialActiveAlarms()
        {
            // Optional: Load initial data from server if supported
            await Task.Delay(100);
        }

        private void HandleIncomingAlarmMessage(AlarmMessage message)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var alarmInstance = message.AlarmInstance;
                    var existingAlarm = ActiveAlarms.FirstOrDefault(a => a.AlarmNo == alarmInstance.AlarmNo);

                    switch (message.MessageType)
                    {
                        case AlarmMessageType.Raised:
                            if (existingAlarm == null)
                            {
                                ActiveAlarms.Add(alarmInstance);
                                CurrentBannerAlarm = alarmInstance;
                            }
                            break;

                        case AlarmMessageType.Acknowledged:
                            if (existingAlarm != null)
                            {
                                existingAlarm.AlarmAckTime = alarmInstance.AlarmAckTime;
                                existingAlarm.AcknowledgedByUser = alarmInstance.AcknowledgedByUser;
                            }
                            break;

                        case AlarmMessageType.Cleared:
                            if (existingAlarm != null)
                            {
                                // Correct property name from AlarmInstanceModel.cs
                                existingAlarm.AlarmResetTime = DateTime.Now;
                                ActiveAlarms.Remove(existingAlarm);
                            }
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }
    }
}