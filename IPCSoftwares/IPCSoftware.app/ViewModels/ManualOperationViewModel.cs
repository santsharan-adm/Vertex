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
using System.Diagnostics;
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
  /*      [Group("Manual X-Axis Jog")] XAxisJogLowSpeed,
        [Group("Manual X-Axis Jog")] XAxisJogHighSpeed,*/

        // Manual Y-Axis
        [Group("Manual Y-Axis Jog")] ManualYAxisJogBackward,
        [Group("Manual Y-Axis Jog")] ManualYAxisJogForward/*,
        [Group("Manual Y-Axis Jog")] YAxisJogLowSpeed,
        [Group("Manual Y-Axis Jog")] YAxisJogHighSpeed*/
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

        //   private bool _xAxisHighSpeed => IsModeActive(ManualOperationMode.XAxisJogHighSpeed);
        //  private bool _yAxisHighSpeed => IsModeActive(ManualOperationMode.YAxisJogHighSpeed);


        private const int TAG_WRITE_X_MINUS = 50;
        private const int TAG_READ_X_MINUS = 90;

        // X Plus
        private const int TAG_WRITE_X_PLUS = 49;
        private const int TAG_READ_X_PLUS = 89;
        // Y Minus
        private const int TAG_WRITE_Y_MINUS = 54;
        private const int TAG_READ_Y_MINUS = 94;
        // Y Plus
        private const int TAG_WRITE_Y_PLUS = 53;
        private const int TAG_READ_Y_PLUS = 93;

        private const int TAG_WRITE_TRAY_DOWN = 40;
        private const int TAG_READ_TRAY_DOWN = 80;

        private const int TAG_WRITE_TRAY_UP= 41;
        private const int TAG_READ_TRAY_UP = 81;


        private const int TAG_WRITE_CYL_DOWN = 43;
        private const int TAG_READ_CYL_DOWN = 83;

        private const int TAG_WRITE_CYL_UP= 42;
        private const int TAG_READ_CYL_UP = 82;

        private const int TAG_WRITE_CONV_FWD= 44;
        private const int TAG_READ_CONV_FWD= 84;

        private const int TAG_WRITE_CONV_REV= 45;
        private const int TAG_READ_CONV_REV = 85;

        private const int TAG_WRITE_CONV_STOP= 46;
        private const int TAG_READ_CONV_STOP= 86;

        private const int TAG_WRITE_CONV_LOW= 47;
        private const int TAG_READ_CONV_LOW= 87;

        private const int TAG_WRITE_CONV_HIGH= 48;
        private const int TAG_READ_CONV_HIGH= 88;



        private const int TAG_PARAM_A4 = 113;





        private bool _isJogXMinusActive; public bool IsJogXMinusActive { get => _isJogXMinusActive; set => SetProperty(ref _isJogXMinusActive, value); }
        private bool _isJogXPlusActive; public bool IsJogXPlusActive { get => _isJogXPlusActive; set => SetProperty(ref _isJogXPlusActive, value); }
        private bool _isJogYMinusActive; public bool IsJogYMinusActive { get => _isJogYMinusActive; set => SetProperty(ref _isJogYMinusActive, value); }
        private bool _isJogYPlusActive; public bool IsJogYPlusActive { get => _isJogYPlusActive; set => SetProperty(ref _isJogYPlusActive, value); }

        private bool _isTrayUpActive; public bool IsTrayUpActive { get => _isTrayUpActive; set => SetProperty(ref _isTrayUpActive, value); }
        private bool _isTrayDownActive; public bool IsTrayDownActive { get => _isTrayDownActive; set => SetProperty(ref _isTrayDownActive, value); }

        // Cylinder
        private bool _isCylUpActive; public bool IsCylUpActive { get => _isCylUpActive; set => SetProperty(ref _isCylUpActive, value); }
        private bool _isCylDownActive; public bool IsCylDownActive { get => _isCylDownActive; set => SetProperty(ref _isCylDownActive, value); }

        //Conveyor Direction
        private bool _isConvFwdActive; public bool IsConvFwdActive { get => _isConvFwdActive; set => SetProperty(ref _isConvFwdActive, value); }
        private bool _isConvRevActive; public bool IsConvRevActive { get => _isConvRevActive; set => SetProperty(ref _isConvRevActive, value); }

        //Converyor Speed
        private bool _isConvLowActive; public bool IsConvLowActive { get => _isConvLowActive; set => SetProperty(ref _isConvLowActive, value); }
        private bool _isConvHighActive; public bool IsConvHighActive { get => _isConvHighActive; set => SetProperty(ref _isConvHighActive, value); }

        private bool _isConvStopActive; public bool IsConvStopActive { get => _isConvStopActive; set => SetProperty(ref _isConvStopActive, value); }




        // --- Properties ---
        public ObservableCollection<ModeItem> Modes { get; }
        public ICommand ButtonClickCommand { get; }
        public ICommand ConfirmYCoordsCommand { get; }

        // Filtered Lists for UI
        public IEnumerable<ModeItem> TrayModes => GetGroup("Tray Lift");
        public IEnumerable<ModeItem> CylinderModes => GetGroup("Positioning Cylinder");
        public IEnumerable<ModeItem> ConveyorModes => GetGroup("Transport Conveyor");
        public IEnumerable<ModeItem> XAxisModes => GetGroup("Manual X-Axis Jog");
        public IEnumerable<ModeItem> YAxisModes => GetGroup("Manual Y-Axis Jog");
        public ModeItem HomePositionMode => Modes.FirstOrDefault(x => x.Mode == ManualOperationMode.MoveToPos0);

        public ICommand JogCommand { get; }
        public ICommand CylPosCommand { get; }
        public ICommand TrayLiftCommand { get; }

        public ICommand ConvDirCommand { get; }
        public ICommand ConvStopCommand { get; }
        public ICommand ConvSpeedCommand { get; }
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
            ConfirmYCoordsCommand = new RelayCommand(XYOriign);

            JogCommand = new RelayCommand<object>(async (args) => await OnJogAsync(args));
            TrayLiftCommand = new RelayCommand<object>(async (args) => await OnTrayAsync(args));
            CylPosCommand = new RelayCommand<object>(async (args) => await OnCylAsync(args));

            ConvDirCommand = new RelayCommand<object>(async (args) => await OnConvDirAsync(args));
            ConvStopCommand = new RelayCommand<object>(async (args) => await OnConvStopAsync(args));
            ConvSpeedCommand = new RelayCommand<object>(async (args) => await OnConvSpeedAsync(args));

            // Faster Polling for Responsiveness (100ms)
            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _feedbackTimer.Tick += FeedbackLoop_Tick;
            _feedbackTimer.Start();

            // Initial Sync
            _ = SyncUiWithPlcState();
        }


        private async void XYOriign()
        {
            await _coreClient.WriteTagAsync(TAG_PARAM_A4, 1);
            
           // await Task.Delay(2000);

            await _coreClient.WriteTagAsync(TAG_PARAM_A4, 0);
        }

        private async Task OnJogAsync(object args)
        {
            // 1. Check PLC Connection
            //if (!await IsPLCConnected())
            //{
            //    // Optionally log only once per press/release to avoid spam
            //    // _logger.LogWarning("Jog ignored: PLC disconnected", LogType.Audit);
            //    return;
            //}

            if (args is not string commandStr) return;
            var parts = commandStr.Split('|');
            if (parts.Length != 2) return;

            if (!Enum.TryParse(parts[0], out JogDirection dir)) return;
            bool isPressed = bool.Parse(parts[1]);

            int writeTagId = 0;

            // 2. Interlock Check (Only on Press)
            if (isPressed)
            {
                switch (dir)
                {
                    case JogDirection.XPlus:
                        if (IsJogXMinusActive) { _logger.LogWarning("Interlock: Cannot Jog X+ while X- is active.", LogType.Audit); return; }
                        writeTagId = TAG_WRITE_X_PLUS;
                        break;
                    case JogDirection.XMinus:
                        if (IsJogXPlusActive) { _logger.LogWarning("Interlock: Cannot Jog X- while X+ is active.", LogType.Audit); return; }
                        writeTagId = TAG_WRITE_X_MINUS;
                        break;
                    case JogDirection.YPlus:
                        if (IsJogYMinusActive) { _logger.LogWarning("Interlock: Cannot Jog Y+ while Y- is active.", LogType.Audit); return; }
                        writeTagId = TAG_WRITE_Y_PLUS;
                        break;
                    case JogDirection.YMinus:
                        if (IsJogYPlusActive) { _logger.LogWarning("Interlock: Cannot Jog Y- while Y+ is active.", LogType.Audit); return; }
                        writeTagId = TAG_WRITE_Y_MINUS;
                        break;
                }
            }
            else
            {
                // On Release, just get the tag to turn off
                switch (dir)
                {
                    case JogDirection.XPlus: writeTagId = TAG_WRITE_X_PLUS; break;
                    case JogDirection.XMinus: writeTagId = TAG_WRITE_X_MINUS; break;
                    case JogDirection.YPlus: writeTagId = TAG_WRITE_Y_PLUS; break;
                    case JogDirection.YMinus: writeTagId = TAG_WRITE_Y_MINUS; break;
                }
            }

            try
            {
                if (isPressed)
                {
                    _logger.LogInfo($"JOG START: {dir} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 1);
                    //await Task.Delay(1000);
                   // await _coreClient.WriteTagAsync(writeTagId, 0);
                }
                else
                {
                    _logger.LogInfo($"JOG STOP: {dir} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Jog Error: {ex.Message}", LogType.Diagnostics);
            }
        }


        private async Task OnTrayAsync(object args)
        {
           // if (!await IsPLCConnected()) return;

            if (args is not string commandStr) return;
            var parts = commandStr.Split('|');
            if (parts.Length != 2) return;

            string direction = parts[0]; // "Up" or "Down"
            bool isPressed = bool.Parse(parts[1]);
            int writeTagId = 0;

            if (isPressed)
            {
                if (direction == "TrayUP")
                {
                    // Interlock: Cannot go Up if Down is Active
                   // if (IsTrayDownActive) { _logger.LogWarning("Interlock: Cannot Tray Up while Tray Down is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_TRAY_UP;
                }
                else if (direction == "TrayDOWN")
                {
                    // Interlock: Cannot go Down if Up is Active
                   // if (IsTrayUpActive) { _logger.LogWarning("Interlock: Cannot Tray Down while Tray Up is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_TRAY_DOWN;
                }
            }
            else
            {
                // Release Logic
                if (direction == "TrayUP") writeTagId = TAG_WRITE_TRAY_UP;
                else if (direction == "TrayDOWN") writeTagId = TAG_WRITE_TRAY_DOWN;
            }

            try
            {
                if (isPressed)
                {
                    _logger.LogInfo($"TRAY START: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 1);
                   // await Task.Delay(1000);
                   // await _coreClient.WriteTagAsync(writeTagId, 0);
                }
                else
                {
                    _logger.LogInfo($"TRAY STOP: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 0);
                }
            }
            catch (Exception ex) { _logger.LogError($"Tray Error: {ex.Message}", LogType.Diagnostics); }
        }

        // =========================================================
        //                 2. CYLINDER LOGIC (Press/Hold)
        // =========================================================



        private async Task OnCylAsync(object args)
        {
            //if (!await IsPLCConnected()) return;

            if (args is not string commandStr) return;
            var parts = commandStr.Split('|');
            if (parts.Length != 2) return;

            string direction = parts[0]; // "Up" or "Down"
            bool isPressed = bool.Parse(parts[1]);
            int writeTagId = 0;

            if (isPressed)
            {
                if (direction == "CylUP")
                {
                  //  if (IsCylDownActive) { _logger.LogWarning("Interlock: Cannot Cyl Up while Cyl Down is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_CYL_UP;
                }
                else if (direction == "CylDOWN")
                {
                    //if (IsCylUpActive) { _logger.LogWarning("Interlock: Cannot Cyl Down while Cyl Up is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_CYL_DOWN;
                }
            }
            else
            {
                if (direction == "CylUP") writeTagId = TAG_WRITE_CYL_UP;
                else if (direction == "CylDOWN") writeTagId = TAG_WRITE_CYL_DOWN;
            }

            try
            {
                if (isPressed)
                {
                    _logger.LogInfo($"CYL START: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 1);
                   // await Task.Delay(1000);
                   // await _coreClient.WriteTagAsync(writeTagId, 0);
                }
                else
                {
                    _logger.LogInfo($"CYL STOP: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 0);
                }
            }
            catch (Exception ex) { _logger.LogError($"Cyl Error: {ex.Message}", LogType.Diagnostics); }
        }



        private async Task OnConvDirAsync(object args)
        {
            //if (!await IsPLCConnected()) return;

            if (args is not string commandStr) return;
            var parts = commandStr.Split('|');
            if (parts.Length != 2) return;
            string direction = parts[0]; // "Up" or "Down"
            bool isPressed = bool.Parse(parts[1]);
            int writeTagId = 0;
            Debug.WriteLine($" Key Pressed Direction  = {direction} IsPressed = {isPressed}   time is = {DateTime.Now.Millisecond}");
            if (isPressed)
            {
                if (direction == "ConvRev")
                {
                  //  if (IsCylDownActive) { _logger.LogWarning("Interlock: Cannot Cyl Up while Cyl Down is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_CONV_REV;
                }
                else if (direction == "ConvFwd")
                {
                    //if (IsCylUpActive) { _logger.LogWarning("Interlock: Cannot Cyl Down while Cyl Up is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_CONV_FWD;
                }
            }
            else
            {
                if (direction == "ConvFwd") writeTagId = TAG_WRITE_CONV_FWD;
                else if (direction == "ConvRev") writeTagId = TAG_WRITE_CONV_REV;
            }

            try
            {
                if (isPressed)
                {
                    _logger.LogInfo($"Conv START: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 1);
                   // await Task.Delay(1000);
                   // await _coreClient.WriteTagAsync(writeTagId, 0);
                }
                else
                {
                    _logger.LogInfo($"Conv STOP: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 0);
                }
            }
            catch (Exception ex) { _logger.LogError($"Conv Error: {ex.Message}", LogType.Diagnostics); }
        }


        private async Task OnConvStopAsync(object args)
        {
           // if (!await IsPLCConnected()) return;

            if (args is not string commandStr) return;
            var parts = commandStr.Split('|');
            if (parts.Length != 2) return;

            string direction = parts[0]; // "Up" or "Down"
            bool isPressed = bool.Parse(parts[1]);
            int writeTagId = 0;

            if (isPressed)
            {
                if (direction == "ConStop")
                {
                  //  if (IsCylDownActive) { _logger.LogWarning("Interlock: Cannot Cyl Up while Cyl Down is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_CONV_STOP;
                }
              
            }
            else
            {
                if (direction == "ConStop") writeTagId = TAG_WRITE_CONV_STOP;
            }

            try
            {
                if (isPressed)
                {
                    _logger.LogInfo($"Conv START: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 1);
                    await HandleStopLogicNew();
                    // await Task.Delay(1000);
                    // await _coreClient.WriteTagAsync(writeTagId, 0);
                }
                else
                {
                    _logger.LogInfo($"Conv STOP: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 0);
                }
            }
            catch (Exception ex) { _logger.LogError($"Conv Error: {ex.Message}", LogType.Diagnostics); }
        }

        private async Task OnConvSpeedAsync(object args)
        {
           // if (!await IsPLCConnected()) return;

            if (args is not string commandStr) return;
            var parts = commandStr.Split('|');
            if (parts.Length != 2) return;

            string direction = parts[0]; // "Up" or "Down"
            bool isPressed = bool.Parse(parts[1]);
            int writeTagId = 0;

            if (isPressed)
            {
                if (direction == "ConvLow")
                {
                  //  if (IsCylDownActive) { _logger.LogWarning("Interlock: Cannot Cyl Up while Cyl Down is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_CONV_LOW;
                }
                else if (direction == "ConvHigh")
                {
                    //if (IsCylUpActive) { _logger.LogWarning("Interlock: Cannot Cyl Down while Cyl Up is active.", LogType.Audit); return; }
                    writeTagId = TAG_WRITE_CONV_HIGH;
                }
            }
            else
            {
                if (direction == "ConvHigh") writeTagId = TAG_WRITE_CONV_HIGH;
                else if (direction == "ConvLow") writeTagId = TAG_WRITE_CONV_LOW;
            }

            try
            {
                if (isPressed)
                {
                    _logger.LogInfo($"Conv START: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 1);
                   // await Task.Delay(1000);
                   // await _coreClient.WriteTagAsync(writeTagId, 0);
                }
                else
                {
                    _logger.LogInfo($"Conv STOP: {direction} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 0);
                }
            }
            catch (Exception ex) { _logger.LogError($"Conv Error: {ex.Message}", LogType.Diagnostics); }
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
                   // SetDefaultSpeeds();
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

                    if (_tagMapA.TryGetValue(item.Mode, out int writeTagId))
                    {
                        if (liveData.TryGetValue(writeTagId, out object? val))
                        {
                            item.IsBlinking = Convert.ToBoolean(val);
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
         //   SetActiveLocal(ManualOperationMode.XAxisJogLowSpeed, true);
         //   SetActiveLocal(ManualOperationMode.YAxisJogLowSpeed, true);
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
          //  MapTag(ManualOperationMode.XAxisJogLowSpeed, aStart + 11, bStart + 11);
           // MapTag(ManualOperationMode.XAxisJogHighSpeed, aStart + 12, bStart + 12);

            // Y Axis
            MapTag(ManualOperationMode.ManualYAxisJogBackward, aStart + 13, bStart + 13);
            MapTag(ManualOperationMode.ManualYAxisJogForward, aStart + 14, bStart + 14);
          //  MapTag(ManualOperationMode.YAxisJogLowSpeed, aStart + 15, bStart + 15);
          //  MapTag(ManualOperationMode.YAxisJogHighSpeed, aStart + 16, bStart + 16);

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

        private async Task<bool> IsPLCConnected()
        {
            try
            {
                // Request ID 5 (IO Data + System Tags)
                var liveData = await _coreClient.GetIoValuesAsync(5);

                if (liveData.Count() == 0)
                    return false;
                return true;
            }
            catch (Exception)
            {
                // Connection lost to Core Service
                return false;
            }
        }

        private async void OnButtonClicked(ManualOperationMode mode)
        {
            try
            {
                if (!await IsPLCConnected())
                {
                    return;
                }
                var item = Modes.First(m => m.Mode == mode);
                string group = item.Group;

                // 1. Handle STOP Button Special Case
                if (mode == ManualOperationMode.TransportConveyorStop)
                {
                    await HandleStopLogic(item);
                    return;
                }

                // 2. Interlock Check (Prevent Forward if Backward is On, Low/High conflict, etc.)
                //if (group != "Move to Position")
                //{
                // if (!CheckInterlocks(item)) return;
                //}
                //else
                //{

                //}

                // 3. Get Tag
                if (!_tagMapA.TryGetValue(mode, out int tagA)) return;

                // 4. Logic Split
                if ( group == "Positioning Cylinder" || group == "Move to Position")
                {
                    // TYPE 1: PULSE (Write 1 -> Blink -> Wait B -> Write 0)
                  
                        _logger.LogInfo($"[Manual] Pulse {mode} (Tag {tagA}=1)", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagA, 1);
                        item.IsBlinking = true; // Waiting for B
                      //  await Task.Delay(1000);
                        await _coreClient.WriteTagAsync(tagA, 0);
                        item.IsBlinking = false;
                    
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
                       // item.IsBlinking = true;
                    }
                    else
                    {
                        // Turn ON
                        _logger.LogInfo($"[Manual] Start {mode} (Tag {tagA}=1)", LogType.Audit);
                        await _coreClient.WriteTagAsync(tagA, 1);
                        item.IsBlinking = true; // Wait for B confirmation
                        //await Task.Delay(1000);
                        await _coreClient.WriteTagAsync(tagA, 0);
                        item.IsBlinking = false;
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

              //  case ManualOperationMode.XAxisJogLowSpeed: exclusive.Add(ManualOperationMode.XAxisJogHighSpeed); break;
              //  case ManualOperationMode.XAxisJogHighSpeed: exclusive.Add(ManualOperationMode.XAxisJogLowSpeed); break;

                case ManualOperationMode.ManualYAxisJogForward: exclusive.Add(ManualOperationMode.ManualYAxisJogBackward); break;
                case ManualOperationMode.ManualYAxisJogBackward: exclusive.Add(ManualOperationMode.ManualYAxisJogForward); break;

             //   case ManualOperationMode.YAxisJogLowSpeed: exclusive.Add(ManualOperationMode.YAxisJogHighSpeed); break;
               // case ManualOperationMode.YAxisJogHighSpeed: exclusive.Add(ManualOperationMode.YAxisJogLowSpeed); break;
                // Inside CheckInterlocks(ModeItem item) switch statement:

                // Add this case to handle ALL position moves (0 to 12)
                case var m when m >= ManualOperationMode.MoveToPos0 && m <= ManualOperationMode.MoveToPos12:
                    // Add all OTHER position modes to the exclusive list
                    for (int i = 0; i <= 12; i++)
                    {
                        var targetMode = (ManualOperationMode)Enum.Parse(typeof(ManualOperationMode), $"MoveToPos{i}");
                        if (targetMode != item.Mode)
                        {
                            exclusive.Add(targetMode);
                        }
                    }
                    break;

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
                    await _coreClient.WriteTagAsync(tStop, 0);
                   // stopItem.IsBlinking = true; // Wait for B (Stop Confirmed) to clear

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Stop Logic Error: {ex.Message}", LogType.Diagnostics);
            }
        }


        private async Task HandleStopLogicNew()
        {
            try
            {
                // 1. Get all Conveyor Items
                IsConvRevActive = false;
                IsConvFwdActive = false;
                IsConvLowActive = false;
                IsConvHighActive = false;

                await _coreClient.WriteTagAsync(TAG_READ_CONV_FWD, 0);
                await _coreClient.WriteTagAsync(TAG_READ_CONV_REV, 0);
                await _coreClient.WriteTagAsync(TAG_READ_CONV_LOW, 0);
                await _coreClient.WriteTagAsync(TAG_READ_CONV_HIGH, 0);
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
                if (!liveData .Any()) return;


                if (liveData.TryGetValue(TAG_READ_X_MINUS, out object xm)) IsJogXMinusActive = Convert.ToBoolean(xm);
                if (liveData.TryGetValue(TAG_READ_X_PLUS, out object xp)) IsJogXPlusActive = Convert.ToBoolean(xp);
                if (liveData.TryGetValue(TAG_READ_Y_MINUS, out object ym)) IsJogYMinusActive = Convert.ToBoolean(ym);
                if (liveData.TryGetValue(TAG_READ_Y_PLUS, out object yp)) IsJogYPlusActive = Convert.ToBoolean(yp);

                // 2.Tray Feedback(B Tags)
                if (liveData.TryGetValue(TAG_READ_TRAY_DOWN, out object td)) IsTrayDownActive = Convert.ToBoolean(td);
                if (liveData.TryGetValue(TAG_READ_TRAY_UP, out object tu)) IsTrayUpActive = Convert.ToBoolean(tu);

                // 3. Cylinder Feedback (B Tags)
                if (liveData.TryGetValue(TAG_READ_CYL_DOWN, out object cd)) IsCylDownActive = Convert.ToBoolean(cd);
                if (liveData.TryGetValue(TAG_READ_CYL_UP, out object cu)) IsCylUpActive = Convert.ToBoolean(cu);

                if (liveData.TryGetValue(TAG_READ_CONV_FWD, out object cf)) IsConvFwdActive = Convert.ToBoolean(cf);
                if (liveData.TryGetValue(TAG_READ_CONV_REV, out object cr)) IsConvRevActive = Convert.ToBoolean(cr);

                if (liveData.TryGetValue(TAG_READ_CONV_STOP, out object cs)) IsConvStopActive = Convert.ToBoolean(cs);

                if (liveData.TryGetValue(TAG_READ_CONV_LOW, out object cl)) IsConvLowActive = Convert.ToBoolean(cl);
                if (liveData.TryGetValue(TAG_READ_CONV_HIGH, out object ch)) IsConvHighActive = Convert.ToBoolean(ch);



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
                            item.IsActive = Convert.ToBoolean(bSignal);
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