using IPCSoftware.App.Services; // CoreClient
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    // --- Helper Classes ---
    public class GroupAttribute : Attribute
    {
        public string Name { get; }
        public GroupAttribute(string name) => Name = name;
    }

    public class ModeItem : ObservableObjectVM
    {
        public ManualOperationMode Mode { get; set; }
        public string Group { get; set; }

        private bool _isActive;
        public bool IsActive // Green Status
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private bool _isBlinking;
        public bool IsBlinking // Waiting for Feedback
        {
            get => _isBlinking;
            set => SetProperty(ref _isBlinking, value);
        }
    }

    public enum ManualOperationMode
    {
        // Tray lift
        [Group("Tray Lift")] TrayLiftUp,
        [Group("Tray Lift")] TrayLiftDown,

        // Positioning Cylinder
        [Group("Positioning Cylinder")] PositioningCylinderUp,
        [Group("Positioning Cylinder")] PositioningCylinderDown,

        // Move to Position (0-12)
        [Group("Move to Position")] MoveToPos0,
        [Group("Move to Position")] MoveToPos1,
        [Group("Move to Position")] MoveToPos2,
        [Group("Move to Position")] MoveToPos3,
        [Group("Move to Position")] MoveToPos4,
        [Group("Move to Position")] MoveToPos5,
        [Group("Move to Position")] MoveToPos6,
        [Group("Move to Position")] MoveToPos7,
        [Group("Move to Position")] MoveToPos8,
        [Group("Move to Position")] MoveToPos9,
        [Group("Move to Position")] MoveToPos10,
        [Group("Move to Position")] MoveToPos11,
        [Group("Move to Position")] MoveToPos12,

        // Transport Conveyor
        [Group("Transport Conveyor")] TransportConveyorReverse,
        [Group("Transport Conveyor")] TransportConveyorForward,
        [Group("Transport Conveyor")] TransportConveyorStop,
        [Group("Transport Conveyor")] TransportConveyorLowSpeed,
        [Group("Transport Conveyor")] TransportConveyorHighSpeed,

        // Manual X-Axis
        [Group("Manual X-Axis Jog")] ManualXAxisJogBackward,
        [Group("Manual X-Axis Jog")] ManualXAxisJogForward,
        [Group("Manual X-Axis Jog")] XAxisJogLowSpeed,
        [Group("Manual X-Axis Jog")] XAxisJogHighSpeed,

        // Manual Y-Axis
        [Group("Manual Y-Axis Jog")] ManualYAxisJogBackward,
        [Group("Manual Y-Axis Jog")] ManualYAxisJogForward,
        [Group("Manual Y-Axis Jog")] YAxisJogLowSpeed,
        [Group("Manual Y-Axis Jog")] YAxisJogHighSpeed
    }

    public class ManualOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly DispatcherTimer _feedbackTimer;

        // Tag Maps (A = Write, B = Read)
        private readonly Dictionary<ManualOperationMode, int> _tagMapA = new();
        private readonly Dictionary<ManualOperationMode, int> _tagMapB = new();

        // Speed State (False = Low, True = High) - These are derived from button state now
        private bool _conveyorHighSpeed => IsModeActive(ManualOperationMode.TransportConveyorHighSpeed);
        private bool _xAxisHighSpeed => IsModeActive(ManualOperationMode.XAxisJogHighSpeed);
        private bool _yAxisHighSpeed => IsModeActive(ManualOperationMode.YAxisJogHighSpeed);

        // --- Properties ---
        public ObservableCollection<ModeItem> Modes { get; }
        public ICommand ButtonClickCommand { get; }

        // Filtered Lists for UI
        public IEnumerable<ModeItem> TrayModes => GetGroup("Tray Lift");
        public IEnumerable<ModeItem> CylinderModes => GetGroup("Positioning Cylinder");
        public IEnumerable<ModeItem> ConveyorModes => GetGroup("Transport Conveyor");
        public IEnumerable<ModeItem> XAxisModes => GetGroup("Manual X-Axis Jog");
        public IEnumerable<ModeItem> YAxisModes => GetGroup("Manual Y-Axis Jog");
        public ModeItem HomePositionMode => Modes.FirstOrDefault(x => x.Mode == ManualOperationMode.MoveToPos0);
        public IEnumerable<ModeItem> GridPositionModes => GetGroup("Move to Position").Where(x => x.Mode != ManualOperationMode.MoveToPos0);

        public ManualOperationViewModel(IAppLogger logger, CoreClient coreClient) : base(logger)
        {
            _coreClient = coreClient;

            InitializeTags(); // Load Tag IDs

            // Populate Modes
            Modes = new ObservableCollection<ModeItem>(
                Enum.GetValues(typeof(ManualOperationMode))
                    .Cast<ManualOperationMode>()
                    .Select(mode => new ModeItem
                    {
                        Mode = mode,
                        Group = GetGroupName(mode)
                    }));

            ButtonClickCommand = new RelayCommand<ManualOperationMode>(OnButtonClicked);

            // Faster Polling for Responsiveness (100ms)
            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _feedbackTimer.Tick += FeedbackLoop_Tick;
            _feedbackTimer.Start();

            // Initial Sync
            _ = SyncUiWithPlcState();
        }

        // Initialize defaults if PLC is fresh, otherwise read from PLC
        private async Task SyncUiWithPlcState()
        {
            try
            {
                var liveData = await _coreClient.GetIoValuesAsync(5);
                if (liveData == null)
                {
                    // If read fails, set defaults locally
                    SetDefaultSpeeds();
                    return;
                }

                // Update ALL buttons based on PLC state
                foreach (var item in Modes)
                {
                    // Check if the "Read" tag (B) is high
                    if (_tagMapB.TryGetValue(item.Mode, out int readTagId))
                    {
                        if (liveData.TryGetValue(readTagId, out object? val))
                        {
                            item.IsActive = Convert.ToBoolean(val);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Sync Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void SetDefaultSpeeds()
        {
            // Default UI state if PLC read fails
            SetActiveLocal(ManualOperationMode.TransportConveyorLowSpeed, true);
            SetActiveLocal(ManualOperationMode.XAxisJogLowSpeed, true);
            SetActiveLocal(ManualOperationMode.YAxisJogLowSpeed, true);
        }

        private void InitializeTags()
        {
            int aStart = 40;
            int bStart = 80;

            // Tray Lift
            MapTag(ManualOperationMode.TrayLiftDown, aStart + 0, bStart + 0);
            MapTag(ManualOperationMode.TrayLiftUp, aStart + 1, bStart + 1);

            // Positioning Cylinder
            MapTag(ManualOperationMode.PositioningCylinderUp, aStart + 2, bStart + 2);
            MapTag(ManualOperationMode.PositioningCylinderDown, aStart + 3, bStart + 3);

            // Conveyor
            MapTag(ManualOperationMode.TransportConveyorForward, aStart + 4, bStart + 4);
            MapTag(ManualOperationMode.TransportConveyorReverse, aStart + 5, bStart + 5);
            MapTag(ManualOperationMode.TransportConveyorStop, aStart + 6, bStart + 6);
            MapTag(ManualOperationMode.TransportConveyorLowSpeed, aStart + 7, bStart + 7);
            MapTag(ManualOperationMode.TransportConveyorHighSpeed, aStart + 8, bStart + 8);

            // X Axis
            MapTag(ManualOperationMode.ManualXAxisJogForward, aStart + 9, bStart + 9);
            MapTag(ManualOperationMode.ManualXAxisJogBackward, aStart + 10, bStart + 10);
            MapTag(ManualOperationMode.XAxisJogLowSpeed, aStart + 11, bStart + 11);
            MapTag(ManualOperationMode.XAxisJogHighSpeed, aStart + 12, bStart + 12);

            // Y Axis
            MapTag(ManualOperationMode.ManualYAxisJogBackward, aStart + 13, bStart + 13);
            MapTag(ManualOperationMode.ManualYAxisJogForward, aStart + 14, bStart + 14);
            MapTag(ManualOperationMode.YAxisJogLowSpeed, aStart + 15, bStart + 15);
            MapTag(ManualOperationMode.YAxisJogHighSpeed, aStart + 16, bStart + 16);

            // Positions 0-12
            int posOffset = 17;
            for (int i = 0; i <= 12; i++)
            {
                var mode = (ManualOperationMode)Enum.Parse(typeof(ManualOperationMode), $"MoveToPos{i}");
                MapTag(mode, aStart + posOffset + i, bStart + posOffset + i);
            }
        }

        private void MapTag(ManualOperationMode mode, int writeId, int readId)
        {
            _tagMapA[mode] = writeId;
            _tagMapB[mode] = readId;
        }

        private async void OnButtonClicked(ManualOperationMode mode)
        {
            try
            {
                var item = Modes.First(m => m.Mode == mode);
                string group = item.Group;

                // 1. Handle STOP Button Special Case
                if (mode == ManualOperationMode.TransportConveyorStop)
                {
                    await HandleStopLogic(item);
                    return;
                }

                // 2. Interlock Check (Prevent Forward if Backward is On, Low/High conflict, etc.)
                if (!CheckInterlocks(item)) return;

                // 3. Get Tag
                if (!_tagMapA.TryGetValue(mode, out int tagA)) return;

                // 4. Logic Split
                if (group == "Tray Lift" || group == "Positioning Cylinder" || group == "Move to Position")
                {
                    // TYPE 1: PULSE (Write 1 -> Blink -> Wait B -> Write 0)
                    if (!item.IsBlinking)
                    {
                        _logger.LogInfo($"[Manual] Pulse {mode} (Tag {tagA}=1)", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagA, 1);
                        item.IsBlinking = true; // Waiting for B
                    }
                }
                else
                {
                    // TYPE 2: LATCH (Jog/Conveyor/Speed)
                    // (Write 1 -> Blink -> Wait B -> Green) OR (Write 0 -> Off)

                    if (item.IsActive || item.IsBlinking)
                    {
                        // Turn OFF
                        _logger.LogInfo($"[Manual] Stop {mode} (Tag {tagA}=0)", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagA, 0);
                        item.IsActive = false;
                        item.IsBlinking = false;
                    }
                    else
                    {
                        // Turn ON
                        _logger.LogInfo($"[Manual] Start {mode} (Tag {tagA}=1)", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagA, 1);
                        item.IsBlinking = true; // Wait for B confirmation
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Btn Click Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private bool CheckInterlocks(ModeItem item)
        {
            var groupItems = GetGroup(item.Group).ToList();

            // Allow clicking the item itself (to turn it off)
            if (item.IsActive || item.IsBlinking) return true;

            // Define exclusive pairs/sets
            var exclusive = new List<ManualOperationMode>();

            switch (item.Mode)
            {
                case ManualOperationMode.TrayLiftUp: exclusive.Add(ManualOperationMode.TrayLiftDown); break;
                case ManualOperationMode.TrayLiftDown: exclusive.Add(ManualOperationMode.TrayLiftUp); break;

                case ManualOperationMode.PositioningCylinderUp: exclusive.Add(ManualOperationMode.PositioningCylinderDown); break;
                case ManualOperationMode.PositioningCylinderDown: exclusive.Add(ManualOperationMode.PositioningCylinderUp); break;

                case ManualOperationMode.TransportConveyorForward: exclusive.Add(ManualOperationMode.TransportConveyorReverse); break;
                case ManualOperationMode.TransportConveyorReverse: exclusive.Add(ManualOperationMode.TransportConveyorForward); break;

                case ManualOperationMode.TransportConveyorLowSpeed: exclusive.Add(ManualOperationMode.TransportConveyorHighSpeed); break;
                case ManualOperationMode.TransportConveyorHighSpeed: exclusive.Add(ManualOperationMode.TransportConveyorLowSpeed); break;

                case ManualOperationMode.ManualXAxisJogForward: exclusive.Add(ManualOperationMode.ManualXAxisJogBackward); break;
                case ManualOperationMode.ManualXAxisJogBackward: exclusive.Add(ManualOperationMode.ManualXAxisJogForward); break;

                case ManualOperationMode.XAxisJogLowSpeed: exclusive.Add(ManualOperationMode.XAxisJogHighSpeed); break;
                case ManualOperationMode.XAxisJogHighSpeed: exclusive.Add(ManualOperationMode.XAxisJogLowSpeed); break;

                case ManualOperationMode.ManualYAxisJogForward: exclusive.Add(ManualOperationMode.ManualYAxisJogBackward); break;
                case ManualOperationMode.ManualYAxisJogBackward: exclusive.Add(ManualOperationMode.ManualYAxisJogForward); break;

                case ManualOperationMode.YAxisJogLowSpeed: exclusive.Add(ManualOperationMode.YAxisJogHighSpeed); break;
                case ManualOperationMode.YAxisJogHighSpeed: exclusive.Add(ManualOperationMode.YAxisJogLowSpeed); break;
            }

            // Check if any exclusive mode is currently active or blinking
            foreach (var exMode in exclusive)
            {
                var other = groupItems.FirstOrDefault(m => m.Mode == exMode);
                if (other != null && (other.IsActive || other.IsBlinking))
                {
                    _logger.LogWarning($"Interlock: Cannot start {item.Mode} while {other.Mode} is active.", LogType.Audit);
                    return false;
                }
            }
            return true;
        }

        private async Task HandleStopLogic(ModeItem stopItem)
        {
            try
            {
                // 1. Get all Conveyor Items
                var fwd = Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorForward);
                var rev = Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorReverse);
                var low = Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorLowSpeed);
                var high = Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorHighSpeed);

                // 2. Turn off UI immediately (Visual Feedback)
                fwd.IsActive = false; fwd.IsBlinking = false;
                rev.IsActive = false; rev.IsBlinking = false;
                low.IsActive = false; low.IsBlinking = false;
                high.IsActive = false; high.IsBlinking = false;

                // 3. Reset Internal Speed Flags (Optional, depends if you want them reset)
                // _conveyorHighSpeed = false; 

                // 4. Send 0 to PLC for Motion tags (Safety)
                if (_tagMapA.TryGetValue(fwd.Mode, out int tF)) await _coreClient.WriteTagAsync(tF, 0);
                if (_tagMapA.TryGetValue(rev.Mode, out int tR)) await _coreClient.WriteTagAsync(tR, 0);

                // 5. Send 0 to PLC for Speed tags (Resetting Speed)
                if (_tagMapA.TryGetValue(low.Mode, out int tL)) await _coreClient.WriteTagAsync(tL, 0);
                if (_tagMapA.TryGetValue(high.Mode, out int tH)) await _coreClient.WriteTagAsync(tH, 0);

                // 6. Pulse Stop Tag
                if (_tagMapA.TryGetValue(stopItem.Mode, out int tStop))
                {
                    _logger.LogInfo($"[Manual] Transport Stop Activated (Tag {tStop})", LogType.Audit);
                    await _coreClient.WriteTagAsync(tStop, 1);
                    stopItem.IsBlinking = true; // Wait for B (Stop Confirmed) to clear
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Stop Logic Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void FeedbackLoop_Tick(object? sender, EventArgs e)
        {
            try
            {
                var liveData = await _coreClient.GetIoValuesAsync(5);
                if (liveData == null) return;

                foreach (var item in Modes)
                {
                    if (!_tagMapB.TryGetValue(item.Mode, out int tagB)) continue;

                    if (liveData.TryGetValue(tagB, out object? valObj))
                    {
                        bool bSignal = Convert.ToBoolean(valObj);
                        string group = item.Group;

                        // --- FEEDBACK TYPE 1 (Tray/Pos/Cyl/Stop) ---
                        if (group == "Tray Lift" || group == "Positioning Cylinder" || group == "Move to Position" || item.Mode == ManualOperationMode.TransportConveyorStop)
                        {
                            if (item.IsBlinking && bSignal)
                            {
                                item.IsBlinking = false; // Stop Blinking

                                // Send A=0 (Reset Trigger)
                                if (_tagMapA.TryGetValue(item.Mode, out int tagA))
                                    await _coreClient.WriteTagAsync(tagA, 0);

                                _logger.LogInfo($"[Manual] Action Complete: {item.Mode}", LogType.Audit);
                            }
                        }
                        // --- FEEDBACK TYPE 2 (Jog/Conveyor/Speed) ---
                        else
                        {
                            if (item.IsBlinking && bSignal)
                            {
                                item.IsBlinking = false;
                                item.IsActive = true; // Turn Green
                                _logger.LogInfo($"[Manual] Action Active: {item.Mode}", LogType.Audit);
                            }
                            else if (item.IsActive && !bSignal)
                            {
                                // PLC turned it off externally -> UI Updates automatically
                                item.IsActive = false;
                            }
                            // Persistence: If we enter screen and B is 1, ensure IsActive is true
                            else if (!item.IsActive && !item.IsBlinking && bSignal)
                            {
                                item.IsActive = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Feedback Loop: {ex.Message}", LogType.Diagnostics);
            }
        }

        private bool IsModeActive(ManualOperationMode mode)
        {
            var m = Modes.FirstOrDefault(x => x.Mode == mode);
            return m?.IsActive ?? false;
        }

        private void SetActiveLocal(ManualOperationMode mode, bool state)
        {
            var m = Modes.FirstOrDefault(x => x.Mode == mode);
            if (m != null) m.IsActive = state;
        }

        private IEnumerable<ModeItem> GetGroup(string groupName) => Modes.Where(x => x.Group == groupName);

        private string GetGroupName(ManualOperationMode mode)
        {
            var attr = mode.GetType().GetMember(mode.ToString())[0]
                .GetCustomAttributes(typeof(GroupAttribute), false)
                .Cast<GroupAttribute>().FirstOrDefault();
            return attr?.Name ?? "Other";
        }

        public void Dispose() => _feedbackTimer.Stop();
    }
}