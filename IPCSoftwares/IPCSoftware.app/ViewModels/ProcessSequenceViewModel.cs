using IPCSoftware.App.Helpers;
using IPCSoftware.App.Models;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class ProcessSequenceViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly SafePoller _poller;

        private readonly int _autoRunTag = ConstantValues.Mode_Auto.Read;
        private readonly int _cycleStartTag = ConstantValues.CYCLE_START_TRIGGER_TAG_ID;
        private readonly int _triggerTag = ConstantValues.TRIGGER_TAG_ID;
        private readonly int _previousStationTag = 56;
        private readonly int _plcSendTag = 59;
        private readonly int _returnTag = ConstantValues.Return_TAG_ID;

        private bool _lastAuto;
        private bool _lastCycle;
        private bool _lastTrigger;
        private bool _lastReturn;
        private bool _lastPrev;
        private bool _lastTag59;

        private int _stepCounter;
        private int _stationIndex;
        private int _currentStation;

        public ObservableCollection<SequenceLogEntry> Logs { get; } = new();
        public ICommand ClearCommand { get; }

        public ProcessSequenceViewModel(CoreClient coreClient, IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
            ClearCommand = new RelayCommand(Clear);

            _poller = new SafePoller(TimeSpan.FromMilliseconds(200), PollAsync, ex =>
            {
                _logger.LogError($"Sequence monitor error: {ex.Message}", LogType.Diagnostics);
            });
            _poller.Start();
        }

        private async Task PollAsync()
        {
            var snapshot = await _coreClient.GetIoValuesAsync(5);
            if (snapshot == null || snapshot.Count == 0) return;
            Evaluate(snapshot);
        }

        private void Evaluate(Dictionary<int, object> values)
        {
            var auto = GetBool(values, _autoRunTag);
            if (auto && !_lastAuto)
            {
                AddStep("AUTO RUN Status Tag ON");
            }
            _lastAuto = auto;

            var cycle = GetBool(values, _cycleStartTag);
            if (cycle && !_lastCycle)
            {
                AddStep("CYCLE START Tag ID 511 -> 1");
                _stationIndex = 0;
                _currentStation = 0;
            }
            else if (!cycle && _lastCycle)
            {
                AddStep("Final Tag ID 511 False received to declare cycle finish");
                AddStep("ALL Tag ID 15 and 59 reset to False");
                _stationIndex = 0;
                _currentStation = 0;
            }
            _lastCycle = cycle;

            var prev = GetBool(values, _previousStationTag);
            if (prev && !_lastPrev)
            {
                AddStep("Previous Station file received and Tag ID 56 sent to PLC");
            }
            _lastPrev = prev;

            var plc59 = GetBool(values, _plcSendTag);
            if (plc59 && !_lastTag59)
            {
                AddStep("Tag ID 59 value 1 sent to PLC");
            }
            else if (!plc59 && _lastTag59)
            {
                AddStep("Tag ID 59 reset to 0");
            }
            _lastTag59 = plc59;

            var trigger = GetBool(values, _triggerTag);
            if (trigger && !_lastTrigger)
            {
                var stationLabel = $"ST-{_stationIndex}";
                AddStep($"{stationLabel} A1 Tag ID 10 True RECEIVED");
                AddStep($"CCD PROCESS Start for {stationLabel}");
                _currentStation = _stationIndex;
                _stationIndex++;
            }
            else if (!trigger && _lastTrigger)
            {
                var stationLabel = $"ST-{_currentStation}";
                AddStep($"{stationLabel} A1 Tag ID 10 False RECEIVED");
            }
            _lastTrigger = trigger;

            var ret = GetBool(values, _returnTag);
            if (ret && !_lastReturn)
            {
                var stationLabel = $"ST-{_currentStation}";
                AddStep($"{stationLabel} B5 Tag ID 15 value 1 Sent");
            }
            else if (!ret && _lastReturn)
            {
                var stationLabel = $"ST-{_currentStation}";
                AddStep($"{stationLabel} B5 Tag ID 15 becomes False");
            }
            _lastReturn = ret;
        }

        private static bool GetBool(Dictionary<int, object> values, int tagId)
        {
            if (tagId <= 0) return false;
            if (values.TryGetValue(tagId, out var obj))
            {
                if (obj is bool b) return b;
                if (obj is int i) return i != 0;
                if (bool.TryParse(obj?.ToString(), out var parsed)) return parsed;
            }
            return false;
        }

        private void AddStep(string description)
        {
            var entry = new SequenceLogEntry
            {
                Step = Interlocked.Increment(ref _stepCounter),
                Timestamp = DateTime.Now,
                Description = description
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Add(entry);
                if (Logs.Count > 500)
                {
                    Logs.RemoveAt(0);
                }
            });
        }

        private void Clear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Clear();
                _stepCounter = 0;
            });
        }

        public void Dispose()
        {
            _poller?.Dispose();
        }
    }
}
