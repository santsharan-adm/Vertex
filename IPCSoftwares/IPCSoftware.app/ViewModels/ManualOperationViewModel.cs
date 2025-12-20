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
    // --- Attributes & Helper Classes ---
    public class GroupAttribute : Attribute
    {
        public string Name { get; }
        public GroupAttribute(string name) => Name = name;
    }

    public class ModeItem : ObservableObjectVM
    {
        public ManualOperationMode Mode { get; set; }
        public string Group { get; set; }

        // Visual States
        private bool _isBlinking;
        public bool IsBlinking
        {
            get => _isBlinking;
            set => SetProperty(ref _isBlinking, value);
        }

        private bool _isActive; // Green State
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
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
        [Group("Transport Conveyor")] TransportConveyorSpeedSwitching, // Toggle Low/High

        // Manual X-Axis
        [Group("Manual X-Axis Jog")] ManualXAxisJogBackward,
        [Group("Manual X-Axis Jog")] ManualXAxisJogForward,
        [Group("Manual X-Axis Jog")] XAxisJogSpeedSwitching,

        // Manual Y-Axis
        [Group("Manual Y-Axis Jog")] ManualYAxisJogBackward,
        [Group("Manual Y-Axis Jog")] ManualYAxisJogForward,
        [Group("Manual Y-Axis Jog")] YAxisJogSpeedSwitching
    }

    public class ManualOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly DispatcherTimer _feedbackTimer;

        // --- TAG CONFIGURATION (A = Write 40-69, B = Read 80-109) ---
        private readonly Dictionary<ManualOperationMode, int> _writeTagMap = new();
        private readonly Dictionary<ManualOperationMode, int> _readTagMap = new();

        // High Speed Tags Mapping (Used for the 6 motion commands that have 2 speeds)
        private readonly Dictionary<ManualOperationMode, int> _writeHighSpeedTagMap = new();
        private readonly Dictionary<ManualOperationMode, int> _readHighSpeedTagMap = new();

        // Speed State (False = Low, True = High)
        private bool _conveyorHighSpeed = false;
        private bool _xAxisHighSpeed = false;
        private bool _yAxisHighSpeed = false;

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

            InitializeTags(); // 1. Load Tag IDs (40-69 / 80-109)

            // 2. Initialize Modes List
            Modes = new ObservableCollection<ModeItem>(
                Enum.GetValues(typeof(ManualOperationMode))
                    .Cast<ManualOperationMode>()
                    .Select(mode => new ModeItem
                    {
                        Mode = mode,
                        Group = GetGroupName(mode)
                    }));

            ButtonClickCommand = new RelayCommand<ManualOperationMode>(OnButtonClicked);

            // 3. Start Polling Loop
            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _feedbackTimer.Tick += FeedbackLoop_Tick;
            _feedbackTimer.Start();
        }

        private void InitializeTags()
        {
            // Base Offsets
            int aStart = 40; // Write Start
            int bStart = 80; // Read Start (A + 40)

            // 1. TRAY LIFT (40-41)
            MapTag(ManualOperationMode.TrayLiftUp, aStart + 0, bStart + 0);
            MapTag(ManualOperationMode.TrayLiftDown, aStart + 1, bStart + 1);

            // 2. POSITIONING CYLINDER (42-43)
            MapTag(ManualOperationMode.PositioningCylinderUp, aStart + 2, bStart + 2);
            MapTag(ManualOperationMode.PositioningCylinderDown, aStart + 3, bStart + 3);

            // 3. POSITIONS 0-12 (44-56)
            int posOffset = 4; // Starts at A44
            for (int i = 0; i <= 12; i++)
            {
                // Enum name construction via simple loop index won't work easily with generic MapTag 
                // so we do it explicitly or assume enum order. 
                // Let's rely on specific Enum values:
                var mode = (ManualOperationMode)Enum.Parse(typeof(ManualOperationMode), $"MoveToPos{i}");
                MapTag(mode, aStart + posOffset + i, bStart + posOffset + i);
            }

            // 4. CONVEYOR (Low: 57-59, High: 64-65)
            // Low Speed & Stop
            MapTag(ManualOperationMode.TransportConveyorReverse, 57, 97);
            MapTag(ManualOperationMode.TransportConveyorForward, 58, 98);
            MapTag(ManualOperationMode.TransportConveyorStop, 59, 99);

            // High Speed (Mapped to Dictionary directly)
            MapHighSpeedTag(ManualOperationMode.TransportConveyorReverse, 64, 104);
            MapHighSpeedTag(ManualOperationMode.TransportConveyorForward, 65, 105);

            // 5. X AXIS (Low: 60-61, High: 66-67)
            MapTag(ManualOperationMode.ManualXAxisJogBackward, 60, 100);
            MapTag(ManualOperationMode.ManualXAxisJogForward, 61, 101);

            MapHighSpeedTag(ManualOperationMode.ManualXAxisJogBackward, 66, 106);
            MapHighSpeedTag(ManualOperationMode.ManualXAxisJogForward, 67, 107);

            // 6. Y AXIS (Low: 62-63, High: 68-69)
            MapTag(ManualOperationMode.ManualYAxisJogBackward, 62, 102);
            MapTag(ManualOperationMode.ManualYAxisJogForward, 63, 103);

            MapHighSpeedTag(ManualOperationMode.ManualYAxisJogBackward, 68, 108);
            MapHighSpeedTag(ManualOperationMode.ManualYAxisJogForward, 69, 109);
        }

        private void MapTag(ManualOperationMode mode, int writeId, int readId)
        {
            _writeTagMap[mode] = writeId;
            _readTagMap[mode] = readId;
        }

        private void MapHighSpeedTag(ManualOperationMode mode, int writeId, int readId)
        {
            _writeHighSpeedTagMap[mode] = writeId;
            _readHighSpeedTagMap[mode] = readId;
        }

        private async void OnButtonClicked(ManualOperationMode mode)
        {
            try
            {
                var item = Modes.First(m => m.Mode == mode);
                string group = item.Group;

                // --- LOGIC A: SPEED SWITCHING ---
                if (mode == ManualOperationMode.TransportConveyorSpeedSwitching) { _conveyorHighSpeed = !_conveyorHighSpeed; ToggleVisual(item); return; }
                if (mode == ManualOperationMode.XAxisJogSpeedSwitching) { _xAxisHighSpeed = !_xAxisHighSpeed; ToggleVisual(item); return; }
                if (mode == ManualOperationMode.YAxisJogSpeedSwitching) { _yAxisHighSpeed = !_yAxisHighSpeed; ToggleVisual(item); return; }

                // --- LOGIC B: CONVEYOR STOP ---
                if (mode == ManualOperationMode.TransportConveyorStop)
                {
                    ResetGroupVisuals("Transport Conveyor");
                    await WriteTag(mode, 1);
                    item.IsBlinking = true;
                    return;
                }

                // --- LOGIC C: MOVEMENT COMMANDS ---

                int tagId = GetWriteTagId(mode);
                if (tagId == 0) return;

                // 2. Logic Split based on Group Type
                if (group == "Tray Lift" || group == "Positioning Cylinder" || group == "Move to Position")
                {
                    // TYPE 1: Pulse Logic (Send A -> Wait B -> Send 0)
                    if (!item.IsBlinking)
                    {
                        _logger.LogInfo($"[Manual] Pulse Start: {mode} (Tag {tagId})", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagId, 1); // Send A=1
                        item.IsBlinking = true;
                    }
                }
                else
                {
                    // TYPE 2: Toggle Logic (X/Y/Conveyor)
                    if (item.IsActive)
                    {
                        // Stop Logic
                        _logger.LogInfo($"[Manual] Stop: {mode} (Tag {tagId})", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagId, 0); // Send A=0
                        item.IsActive = false;
                        item.IsBlinking = false;
                    }
                    else
                    {
                        // Start Logic
                        _logger.LogInfo($"[Manual] Start: {mode} (Tag {tagId})", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagId, 1); // Send A=1
                        item.IsBlinking = true; // Wait for B
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Button Click Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void FeedbackLoop_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Request IO Packet (ID 5 assumed)
                var liveData = await _coreClient.GetIoValuesAsync(5);
                if (liveData == null) return;

                foreach (var item in Modes)
                {
                    // Determine which Tag ID to check (Low vs High speed based on selection)
                    int readTagId = GetReadTagId(item.Mode);
                    if (readTagId == 0) continue;

                    if (liveData.TryGetValue(readTagId, out object? valObj))
                    {
                        bool bSignal = Convert.ToBoolean(valObj); // B Parameter

                        string group = item.Group;

                        // --- FEEDBACK LOGIC TYPE 1: Tray/Cylinder/Position ---
                        if (group == "Tray Lift" || group == "Positioning Cylinder" || group == "Move to Position")
                        {
                            if (item.IsBlinking && bSignal)
                            {
                                item.IsBlinking = false;
                                int writeTag = GetWriteTagId(item.Mode);
                                await _coreClient.WriteTagAsync(writeTag, 0); // Reset A=0
                                _logger.LogInfo($"[Manual] Action Complete: {item.Mode}", LogType.Audit);
                            }
                        }
                        // --- FEEDBACK LOGIC TYPE 2: X/Y/Conveyor ---
                        else if (group == "Transport Conveyor" || group.Contains("Jog"))
                        {
                            if (item.IsBlinking && bSignal)
                            {
                                item.IsBlinking = false;
                                item.IsActive = true;
                                _logger.LogInfo($"[Manual] Action Active: {item.Mode}", LogType.Audit);
                            }
                            // Logic to auto-turn off UI if PLC turns off bit?
                            // else if (item.IsActive && !bSignal) item.IsActive = false; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Logger.LogError($"Feedback Loop Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        // Helpers
        private int GetWriteTagId(ManualOperationMode mode)
        {
            if (_conveyorHighSpeed && IsConveyorMove(mode)) return _writeHighSpeedTagMap.GetValueOrDefault(mode, 0);
            if (_xAxisHighSpeed && IsXMove(mode)) return _writeHighSpeedTagMap.GetValueOrDefault(mode, 0);
            if (_yAxisHighSpeed && IsYMove(mode)) return _writeHighSpeedTagMap.GetValueOrDefault(mode, 0);

            return _writeTagMap.GetValueOrDefault(mode, 0);
        }

        private int GetReadTagId(ManualOperationMode mode)
        {
            if (_conveyorHighSpeed && IsConveyorMove(mode)) return _readHighSpeedTagMap.GetValueOrDefault(mode, 0);
            if (_xAxisHighSpeed && IsXMove(mode)) return _readHighSpeedTagMap.GetValueOrDefault(mode, 0);
            if (_yAxisHighSpeed && IsYMove(mode)) return _readHighSpeedTagMap.GetValueOrDefault(mode, 0);

            return _readTagMap.GetValueOrDefault(mode, 0);
        }

        private bool IsConveyorMove(ManualOperationMode m) => m == ManualOperationMode.TransportConveyorForward || m == ManualOperationMode.TransportConveyorReverse;
        private bool IsXMove(ManualOperationMode m) => m == ManualOperationMode.ManualXAxisJogForward || m == ManualOperationMode.ManualXAxisJogBackward;
        private bool IsYMove(ManualOperationMode m) => m == ManualOperationMode.ManualYAxisJogForward || m == ManualOperationMode.ManualYAxisJogBackward;

        private async Task WriteTag(ManualOperationMode mode, object val)
        {
            if (_writeTagMap.TryGetValue(mode, out int tag))
                await _coreClient.WriteTagAsync(tag, val);
        }

        private void ToggleVisual(ModeItem item)
        {
            item.IsActive = !item.IsActive;
        }

        private void ResetGroupVisuals(string group)
        {
            foreach (var m in Modes.Where(x => x.Group == group))
            {
                m.IsActive = false;
                m.IsBlinking = false;
            }
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