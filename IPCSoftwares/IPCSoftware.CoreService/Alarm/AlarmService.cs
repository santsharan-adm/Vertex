using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IPCSoftware.CoreService.Alarm
{
    public class AlarmService : BaseService
    {
        // Stores alarm definitions loaded from the config file
        private  List<AlarmConfigurationModel> _alarmDefinitions;
        private readonly IMessagePublisher _publisher;
        // Stores currently active alarm instances, keyed by AlarmNo
        private readonly ConcurrentDictionary<int, AlarmInstanceModel> _activeAlarms;
        private readonly IAlarmConfigurationService _alarmConfigService;

        // Assuming you have a simple Message Broker or Publisher service
        // private readonly IMessagePublisher _publisher; 

        public AlarmService(IAlarmConfigurationService alarmConfigService,
            IMessagePublisher publisher,
            IAppLogger logger) : base(logger)
        {
            _alarmConfigService = alarmConfigService;
        
            _activeAlarms = new ConcurrentDictionary<int, AlarmInstanceModel>();
            _publisher = publisher;
            _ = InitializeAlarms();
        }

        private async Task InitializeAlarms()
        {
            try
            {
                await _alarmConfigService.InitializeAsync();
                _alarmDefinitions = await _alarmConfigService.GetAllAlarmsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }

        }


        /// <summary>
        /// Processes real-time tag data to raise or clear alarms.
        /// </summary>
        /// <param name="processedData">Dictionary of Tag ID (int) and its final processed Value (object).</param>
        public void ProcessTagData(Dictionary<int, object> processedData)
        {
            try
            {
                // Group the alarm definitions by the TagNo they monitor
                var alarmsByTag = _alarmDefinitions.GroupBy(a => a.TagNo);

                foreach (var group in alarmsByTag)
                {
                    int tagId = group.Key;

                    // Check if the current PLC scan contains the monitored tag
                    if (!processedData.TryGetValue(tagId, out var newValue))
                    {
                        continue; // Skip if the tag data isn't in this scan
                    }

                    foreach (var config in group)
                    {
                        // 1. EVALUATE CONDITION
                        bool isAlarmConditionMet = CheckAlarmCondition(config, newValue);

                        // 2. CHECK ALARM STATE
                        bool isActive = _activeAlarms.ContainsKey(config.AlarmNo);

                        if (isAlarmConditionMet && !isActive)
                        {
                            // Condition Met (True) and Alarm is NOT Active -> RAISE ALARM
                            RaiseAlarm(config);
                        }
                        else if (!isAlarmConditionMet && isActive)
                        {
                            // Condition NOT Met (False) and Alarm IS Active -> CLEAR ALARM
                            // NOTE: If you need to enforce Acknowledgement before clearing, the logic changes here.
                            // For now, simple clear on reset.
                            ClearAlarm(config.AlarmNo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        // Inside IPCSoftware.CoreService.Services.Alarm/AlarmService.cs

        private bool CheckAlarmCondition(AlarmConfigurationModel config, object newValue)
        {
            try
            {
                // Case 1: The tag itself is a Boolean (if you have dedicated Boolean alarm tags)
                if (newValue is bool boolValue)
                {
                    return boolValue == true;
                }

                // Case 2: The tag is numeric (INT) and AlarmBit specifies a specific bit index (0-15).
                // This is the CRITICAL logic for your 16-bit alarm registers.
                if (int.TryParse(config.AlarmBit, out int bitIndex))
                {
                    // A valid bit index is usually 0 through 15 for a 16-bit INT.
                    if (bitIndex >= 0 && bitIndex <= 15)
                    {
                        // Try to handle 16-bit unsigned or signed integers
                        if (newValue is ushort ushortValue)
                        {
                            // Check if the specified bit is set
                            return ((ushortValue >> bitIndex) & 0x01) == 1;
                        }
                        if (newValue is short shortValue)
                        {
                            return (((ushort)shortValue >> bitIndex) & 0x01) == 1;
                        }
                        // For safety, handle as a generic integer if 16-bit types are boxed as 32-bit int
                        if (newValue is int intValue)
                        {
                            return (((uint)intValue >> bitIndex) & 0x01) == 1;
                        }
                    }
                }

                // Case 3: Fallback (If AlarmBit is not a valid index, but we assume the value must be non-zero to trigger)
                // This catches tags where the value itself represents the condition status (e.g., AlarmBit="1").
                if (newValue is IConvertible convertibleValue)
                {
                    try
                    {
                        double numericValue = convertibleValue.ToDouble(null);
                        return numericValue != 0.0;
                    }
                    catch { } // Ignore conversion errors for non-numeric types
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        private async Task RaiseAlarm(AlarmConfigurationModel config)
        {
            try
            {
                var newInstance = new AlarmInstanceModel
                {
                    InstanceId = Guid.NewGuid(),
                    AlarmNo = config.AlarmNo,
                    AlarmText = config.AlarmText,
                    Severity = config.Severity,
                    AlarmTime = DateTime.Now,
                    // New alarms start as Active
                };

                if (_activeAlarms.TryAdd(config.AlarmNo, newInstance))
                {
                    Console.WriteLine($"*** ALARM RAISED: {config.AlarmText} (Tag {config.TagNo}) ***");
                    _logger.LogInfo($"*** ALARM RAISED: {config.AlarmText} (Tag {config.TagNo}) ***", LogType.Diagnostics);
                    Console.WriteLine($"\n*** ALARM RAISED *** No: {config.AlarmNo} | Tag: {config.TagNo} | Bit: {config.AlarmBit} | Text: {config.AlarmText}");
                    _logger.LogInfo($"\n*** ALARM RAISED *** No: {config.AlarmNo} | Tag: {config.TagNo} | Bit: {config.AlarmBit} | Text: {config.AlarmText}", LogType.Diagnostics);
                    Console.ResetColor();
                    // ⚠️ Publish the new AlarmInstanceModel via your Messaging system
                    // _publisher.Publish(new AlarmMessage { AlarmInstance = newInstance });
                    await _publisher.PublishAsync(new AlarmMessage
                    {
                        AlarmInstance = newInstance,
                        MessageType = AlarmMessageType.Raised
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        public async Task ClearAlarm(int alarmNo)
        {
            try
            {
                if (_activeAlarms.TryRemove(alarmNo, out var clearedInstance))
                {
                    clearedInstance.AlarmResetTime = DateTime.Now;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"--- ALARM CLEARED: {clearedInstance.AlarmText} ---");
                    _logger.LogInfo ($"--- ALARM CLEARED: {clearedInstance.AlarmText} ---", LogType.Diagnostics);
                    Console.ResetColor();

                    // ⚠️ Publish the cleared AlarmInstanceModel via your Messaging system
                    // _publisher.Publish(new AlarmMessage { AlarmInstance = clearedInstance });

                    await _publisher.PublishAsync(new AlarmMessage
                    {
                        AlarmInstance = clearedInstance,
                        MessageType = AlarmMessageType.Cleared
                    });

                    // NOTE: to store history krishna will save 'clearedInstance' to a database here.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        // This method will be called from the UI via the UI Listener
        public async Task<bool> AcknowledgeAlarm(int alarmNo, string userName)
        {
            try
            {
                if (_activeAlarms.TryGetValue(alarmNo, out var alarm))
                {
                    // Only acknowledge if not already acknowledged
                    if (alarm.AlarmAckTime == null)
                    {
                        alarm.AlarmAckTime = DateTime.Now;
                        alarm.AcknowledgedByUser = userName;
                        Console.WriteLine($"--- ALARM ACKNOWLEDGED: {alarm.AlarmText} by {userName} ---");
                        _logger.LogInfo($"--- ALARM ACKNOWLEDGED: {alarm.AlarmText} by {userName} ---", LogType.Diagnostics);
                        // ⚠️ Publish the acknowledged AlarmInstanceModel
                        // _publisher.Publish(new AlarmMessage { AlarmInstance = alarm });
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }
    }
}