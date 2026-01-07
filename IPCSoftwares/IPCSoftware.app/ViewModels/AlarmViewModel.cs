using IPCSoftware.App;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AlarmViewModel : BaseViewModel
    {
        private readonly CoreClient _coreClient;
        private readonly IAlarmHistoryService _historyService; // 1. Add Service Field

        // Collection for the DataGrid
        public ObservableCollection<AlarmInstanceModel> ActiveAlarms { get; } =
            new ObservableCollection<AlarmInstanceModel>();


        // 2. Add History Collection
        public ObservableCollection<AlarmHistoryModel> HistoryAlarms { get; } = new ObservableCollection<AlarmHistoryModel>();
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

        // 3. Add Date Selection for History Tab
        private DateTime _selectedHistoryDate = DateTime.Today;
        public DateTime SelectedHistoryDate
        {
            get => _selectedHistoryDate;
            set { SetProperty(ref _selectedHistoryDate, value); LoadHistory(); }
        }

        public AlarmViewModel(CoreClient coreClient, IAlarmHistoryService historyService, IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
            _historyService = historyService; // Assign
            // Initialize Global Commands (Tags 38 and 39)
            GlobalAcknowledgeCommand = new RelayCommand(async () => await ExecuteGlobalWrite(ConstantValues.TAG_Global_Ack, "Global Acknowledge"));
            GlobalResetCommand = new RelayCommand(async () => await ExecuteGlobalWrite(ConstantValues.TAG_Global_Reset, "Global Reset"));

            // Initialize Row-Level Command
            AcknowledgeCommand = new RelayCommand<AlarmInstanceModel>(
                     execute: async (alarm) => await AcknowledgeAlarmRequestAsync(alarm),
                     canExecute: CanExecuteAcknowledge);

            _coreClient.OnAlarmMessageReceived += HandleIncomingAlarmMessage;
            Task.Run(LoadInitialActiveAlarms);
            LoadHistory();
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(ActiveAlarms);
           // collectionView.Filter+= AlarmFilter;    
        }
        bool bActive;
      /*  bool AlarmFilter(object item)
        {
            if (bActive)
            {
                if (item is AlarmInstanceModel alarm)
                {
                    AlarmInstanceModel alarm = (AlarmInstanceModel)item;
                    return alarm.AlarmResetTime is null;
                }
            }
            else return true;
        }*/
        // Update these methods in AlarmViewModel.cs

        private async Task ExecuteGlobalWrite(int tagId, string actionName)
        {
            try
            {
                // 1. Write the boolean true to the PLC tag
                await _coreClient.WriteTagAsync(tagId, true);
                _logger.LogInfo($"{actionName} (Tag {tagId}) triggered successfully.", LogType.Audit);
                var now = DateTime.Now;
                string currentUser = Environment.UserName;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var alarm in ActiveAlarms)
                    {

                        if (tagId == ConstantValues.TAG_Global_Reset) // Global Reset logic
                        {
                            // Update the reset timestamp in the model
                            alarm.AlarmResetTime = now;
                            Task.Run(() => _historyService.LogHistoryAsync(alarm, currentUser));

                            // Optional: If viewing today's history, add to UI immediately
                            if (SelectedHistoryDate.Date == DateTime.Today)
                            {
                                HistoryAlarms.Insert(0, new AlarmHistoryModel
                                {
                                    AlarmNo = alarm.AlarmNo,
                                    AlarmText = alarm.AlarmText,
                                    Severity = alarm.Severity,
                                    RaisedTime = alarm.AlarmTime,
                                    ResetTime = now,
                                    ResetBy = currentUser
                                });
                            }
                        }
                    }
                });
                if (tagId == ConstantValues.TAG_Global_Reset)
                {
                    await Task.Delay(2000); // Wait 2s
                    await _coreClient.WriteTagAsync(tagId, false); // Turn Off
                    _logger.LogInfo($"{actionName} (Tag {tagId}) pulsed off.", LogType.Audit);
                }
            }



            //    if (success)
            //    {
            //        _logger.LogInfo($"{actionName} (Tag {tagId}) triggered successfully.", LogType.Audit);

            //        // 2. Update the local UI collection immediately on the UI thread
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            foreach (var alarm in ActiveAlarms)
            //            {
            //                if (tagId == 38) // Global Acknowledge logic
            //                {
            //                    if (alarm.AlarmAckTime == null)
            //                    {
            //                        alarm.AlarmAckTime = now;
            //                        alarm.AcknowledgedByUser = Environment.UserName;
            //                    }
            //                }
            //                else if (tagId == 39) // Global Reset logic
            //                {
            //                    // Update the reset timestamp in the model
            //                    alarm.AlarmResetTime = now;
            //                }
            //            }
            //        });

            //        // 3. Pulse Logic: Turn OFF Tag 39 after 2 seconds
            //        if (tagId == 39)
            //        {
            //            await Task.Delay(2000); // Wait 2s
            //            await _coreClient.WriteTagAsync(tagId, false); // Turn Off
            //            _logger.LogInfo($"{actionName} (Tag {tagId}) pulsed off.", LogType.Audit);
            //        }
            //    }
            //}
            catch (Exception ex)
            {
                _logger.LogError($"Error during {actionName}: {ex.Message}", LogType.Diagnostics);
            }
        }

        public async void LoadHistory()
        {
            var data = await _historyService.GetHistoryAsync(SelectedHistoryDate);
            Application.Current.Dispatcher.Invoke(() => {
                HistoryAlarms.Clear();
                foreach (var item in data) HistoryAlarms.Add(item);
            });
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
                                //ActiveAlarms.Remove(existingAlarm);
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