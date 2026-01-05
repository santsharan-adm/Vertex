using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.ViewModels
{
    public class ServoCalibrationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly DispatcherTimer _liveDataTimer;
        private readonly IServoCalibrationService _servoService; // Injected Service
        private readonly IDialogService _dialog; // Injected Service

        private bool _initialPlcLoadDone = false;


        // Start of Coordinate Registers (13 Positions: 0 to 12)
        private  int START_TAG_POS_X = ConstantValues.Servo_Pos_Start.X;
        private  int START_TAG_POS_Y = ConstantValues.Servo_Pos_Start.Y;

        // Visual Feedback Properties for Jog Buttons
        // These are set True ONLY when B-Tag is received from PLC
        private bool _isJogXMinusActive; public bool IsJogXMinusActive { get => _isJogXMinusActive; set => SetProperty(ref _isJogXMinusActive, value); }
        private bool _isJogXPlusActive; public bool IsJogXPlusActive { get => _isJogXPlusActive; set => SetProperty(ref _isJogXPlusActive, value); }
        private bool _isJogYMinusActive; public bool IsJogYMinusActive { get => _isJogYMinusActive; set => SetProperty(ref _isJogYMinusActive, value); }
        private bool _isJogYPlusActive; public bool IsJogYPlusActive { get => _isJogYPlusActive; set => SetProperty(ref _isJogYPlusActive, value); }


        // --- Properties ---
        private double _liveX;
        public double LiveX
        {
            get => _liveX;
            set => SetProperty(ref _liveX, value);
        }

        private double _liveY;
        public double LiveY
        {
            get => _liveY;
            set => SetProperty(ref _liveY, value);
        }

        public ObservableCollection<ServoPositionModel> Positions { get; } = new();

        // Split Parameters into X and Y Lists
        public ObservableCollection<ServoParameterItem> XParameters { get; } = new();
        public ObservableCollection<ServoParameterItem> YParameters { get; } = new();

        public List<int> AvailableSequences { get; } = Enumerable.Range(1, 12).ToList();
        private ServoPositionModel ClonePosition(ServoPositionModel p) => new ServoPositionModel { PositionId = p.PositionId, Name = p.Name, Description = p.Description, SequenceIndex = p.SequenceIndex, X = p.X, Y = p.Y };

        // Commands
        public ICommand TeachCommand { get; }
        public ICommand WritePositionCommand { get; }
        public ICommand WriteParamCommand { get; }

        // 4 Separate Confirm Commands
        public ICommand ConfirmXParamsCommand { get; }
        public ICommand ConfirmYParamsCommand { get; }
        public ICommand ConfirmXCoordsCommand { get; }
        public ICommand ConfirmYCoordsCommand { get; }
        public ICommand ConfirmParamsCommand { get; }

        // NEW: Jog Command (Takes [Direction, IsPressed])
        public ICommand JogCommand { get; }

        public ServoCalibrationViewModel(CoreClient coreClient, 
            IServoCalibrationService servoService,IDialogService dialog, IAppLogger logger)
             : base(logger)
        {
            _dialog = dialog;
            _coreClient = coreClient;
            _servoService = servoService; // Assign injected service

            TeachCommand = new RelayCommand<ServoPositionModel>(OnTeachPosition);
            WritePositionCommand = new RelayCommand<ServoPositionModel>(OnWritePositionManual);
            WriteParamCommand = new RelayCommand<ServoParameterItem>(OnWriteParameter);

            ConfirmXParamsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_ParamSave, "X Servo Params"));
            ConfirmYParamsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_ParamA2, "Y Servo Params"));
            ConfirmXCoordsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_CoordSave, "X Coordinates"));
            ConfirmYCoordsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_XYOrigin, "Y Coordinates"));

            JogCommand = new RelayCommand<object>(async (args) => await OnJogAsync(args));

            InitializeParameters();
            // Load positions from JSON via Service
            _ = InitializePositionsAsync();
            //InitializePositions();
                
            _liveDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _liveDataTimer.Tick += OnLiveDataTick;
            _liveDataTimer.Start();
           // UpdateCoord();
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

        private async Task OnJogAsync(object args)
        {
            
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
                       // if (IsJogXMinusActive) { _logger.LogWarning("Interlock: Cannot Jog X+ while X- is active.", LogType.Audit); return; }
                        writeTagId = ConstantValues.Manual_XFwd.Write;
                        break;
                    case JogDirection.XMinus:
                       // if (IsJogXPlusActive) { _logger.LogWarning("Interlock: Cannot Jog X- while X+ is active.", LogType.Audit); return; }
                        writeTagId = ConstantValues.Manual_XRev.Write ;
                        break;
                    case JogDirection.YPlus:
                       // if (IsJogYMinusActive) { _logger.LogWarning("Interlock: Cannot Jog Y+ while Y- is active.", LogType.Audit); return; }
                        writeTagId = ConstantValues.Manual_YFwd.Write ;
                        break;
                    case JogDirection.YMinus:
                      //  if (IsJogYPlusActive) { _logger.LogWarning("Interlock: Cannot Jog Y- while Y+ is active.", LogType.Audit); return; }
                        writeTagId = ConstantValues.Manual_YRev.Write;
                        break;
                }
            }
            else
            {
                // On Release, just get the tag to turn off
                switch (dir)
                {
                    case JogDirection.XPlus: writeTagId = ConstantValues.Manual_XFwd.Write; break;
                    case JogDirection.XMinus: writeTagId = ConstantValues.Manual_XRev.Write; break;
                    case JogDirection.YPlus: writeTagId = ConstantValues.Manual_YFwd.Write; break;
                    case JogDirection.YMinus: writeTagId = ConstantValues.Manual_YRev.Write; break;
                }
            }

            try
            {
                if (isPressed)
                {
                    _logger.LogInfo($"JOG START: {dir} (Tag {writeTagId})", LogType.Audit);
                    await _coreClient.WriteTagAsync(writeTagId, 1);
                   // await Task.Delay(1000);
                    //await _coreClient.WriteTagAsync(writeTagId, 0);
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

        private void InitializeParameters()
        {
            Map("Jog Low Speed", ConstantValues.Servo_JogSpeed_Low);
            Map("Origin Offset", ConstantValues.Servo_OffSet);
            Map("Move Speed", ConstantValues.Servo_Move_Speed);
            Map("Acceleration", ConstantValues.Servo_Accel);
            Map("Deceleration", ConstantValues.Servo_DeAccel);
        }

        private void Map(string name, XYPair pair)
        {
            // Helper to create the item (Target-typed new)
            ServoParameterItem Create(int tag) => new() { Name = name, ReadTagId = tag, WriteTagId = tag };

            XParameters.Add(Create(pair.X));
            YParameters.Add(Create(pair.Y));
        }


       
        private async Task InitializePositionsAsync()
        {
            try
            {
                // Use the service to load positions (which includes SequenceIndex and Coordinates)
                var positions = await _servoService.LoadPositionsAsync();

                Positions.Clear();
                // Ensure ordered by ID for UI consistency
                foreach (var pos in positions.OrderBy(p => p.PositionId))
                {
                    Positions.Add(pos);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Init Positions Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void OnLiveDataTick(object? sender, EventArgs e)
        {
            _liveDataTimer.Stop();
            try
            {
                // Request IO Packet (ID 5 assumed to cover all tags)
                var data = await _coreClient.GetIoValuesAsync(5);

                if (data != null)
                {
                    // 1. Update Jog Status (B-Tags)
                    // Visual feedback depends strictly on these values
                    if (data.TryGetValue(ConstantValues.Manual_XRev.Read, out object xm)) IsJogXMinusActive = Convert.ToBoolean(xm);
                    if (data.TryGetValue(ConstantValues.Manual_XFwd.Read, out object xp)) IsJogXPlusActive = Convert.ToBoolean(xp);
                    if (data.TryGetValue(ConstantValues.Manual_YRev.Read, out object ym)) IsJogYMinusActive = Convert.ToBoolean(ym);
                    if (data.TryGetValue(ConstantValues.Manual_YFwd.Read, out object yp)) IsJogYPlusActive = Convert.ToBoolean(yp);

                    // 1. Update Live Position
                    if (data.TryGetValue(ConstantValues.Servo_Live.X  , out object xVal)) LiveX = Convert.ToDouble(xVal);
                    if (data.TryGetValue(ConstantValues.Servo_Live.Y, out object yVal)) LiveY = Convert.ToDouble(yVal);

                    // 2. Update X Parameters
                    foreach (var param in XParameters)
                    {
                        if (data.TryGetValue(param.ReadTagId, out object val))
                            param.CurrentValue = Convert.ToDouble(val);
                    }

                    // 3. Update Y Parameters
                    foreach (var param in YParameters)
                    {
                        if (data.TryGetValue(param.ReadTagId, out object val))
                            param.CurrentValue = Convert.ToDouble(val);
                    }

                    // 4. Initial Load of Stored Positions (ONCE ONLY)
                    if (!_initialPlcLoadDone)
                    {
                        bool anyDataRead = false;
                        for (int i = 0; i < Positions.Count; i++)
                        {
                            int xTag = START_TAG_POS_X + i;
                            int yTag = START_TAG_POS_Y + i;

                            if (data.TryGetValue(xTag, out object valX))
                            {
                                Positions[i].X = Convert.ToDouble(valX);
                                anyDataRead = true;
                            }
                            if (data.TryGetValue(yTag, out object valY))
                            {
                                Positions[i].Y = Convert.ToDouble(valY);
                                anyDataRead = true;
                            }
                        }

                        // Only mark as done if we actually got some data (connection is valid)
                        if (anyDataRead) _initialPlcLoadDone = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Live Data Error: {ex.Message}");
            }
            finally
            {
                // CRITICAL FIX 1: Restart timer only after processing is done
                _liveDataTimer.Start();
            }
        }
        


        private async void UpdateCoord()
        {
            var data = await _coreClient.GetIoValuesAsync(5);
            // 4. Update Position List (Read stored values from PLC)
            for (int i = 0; i < Positions.Count; i++)
            {
                int xTag = START_TAG_POS_X + i;
                int yTag = START_TAG_POS_Y + i;

                if (data.TryGetValue(xTag, out object valX)) Positions[i].X = Convert.ToDouble(valX);
                if (data.TryGetValue(yTag, out object valY)) Positions[i].Y = Convert.ToDouble(valY);
            }
        }



        private async void OnTeachPosition(ServoPositionModel position)
        {
            if (position == null) return;

            try
            {
                int xTag = START_TAG_POS_X + position.PositionId;
                int yTag = START_TAG_POS_Y + position.PositionId;

                _logger.LogInfo($"Teaching Pos {position.PositionId}: X={LiveX}, Y={LiveY}", LogType.Audit);

                // Write X and Y directly to PLC registers
                await _coreClient.WriteTagAsync(xTag, LiveX);
                await _coreClient.WriteTagAsync(yTag, LiveY);

               // var newPos = ClonePosition(position);
               // newPos.X = LiveX;
               // newPos.Y = LiveY;
               // ReplacePositionInList(position, newPos);

               // Optimistic UI Update
                position.X = LiveX;
                position.Y = LiveY;
                int index = Positions.IndexOf(position);
                if (index != -1)
                {
                    Positions[index] = position;
                }
                _dialog.ShowMessage("Values updated sucessfully.");
                _initialPlcLoadDone = false;
               // UpdateCoord();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Teach Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void OnWritePositionManual(ServoPositionModel position)
        {
            if (position == null) return;
            try
            {
                // The 'position' object already has the new values because 
                // the TextBox binding (UpdateSourceTrigger=LostFocus) updated it.

                int xTag = START_TAG_POS_X + position.PositionId;
                int yTag = START_TAG_POS_Y + position.PositionId;

                _logger.LogInfo($"Manually Writing Pos {position.PositionId}: X={position.X:F2}, Y={position.Y:F2}", LogType.Audit);

                await _coreClient.WriteTagAsync(xTag, position.X);
                await _coreClient.WriteTagAsync(yTag, position.Y);
                _dialog.ShowMessage("Values updated sucessfully.");
                _initialPlcLoadDone = false;
              //  UpdateCoord();
                // Optional: Flash success or log
            }
            catch (Exception ex) { _logger.LogError($"Manual Write Error: {ex.Message}", LogType.Diagnostics); }
        }


   

        private async void OnWriteParameter(ServoParameterItem param)
        {
            if (param == null) return;
            try
            {
                _logger.LogInfo($"Writing {param.Name} -> {param.NewValue}", LogType.Audit);
                await _coreClient.WriteTagAsync(param.WriteTagId, param.NewValue);
                _dialog.ShowMessage("Value updated sucessfully.");


            }
            catch (Exception ex) { _logger.LogError($"Write Param Error: {ex.Message}", LogType.Diagnostics); }
        }

        private async Task PulseBit(int tagId, string description)
        {
            try
            {
                if (description.Contains("Coordinates"))
                {
                    // --- VALIDATION LOGIC START ---
                    var userSequences = Positions.Where(p => p.PositionId != 0).Select(p => p.SequenceIndex).ToList();

                    // 1. Check for Duplicates
                    if (userSequences.Distinct().Count() != userSequences.Count)
                    {
                        _dialog.ShowWarning("Validation Failed: Duplicate sequence numbers detected. Each position must have a unique number from 1 to 12.");
                        return;
                    }

                    // 2. Check Range (Just in case)
                    if (userSequences.Any(s => s < 1 || s > 12))
                    {
                        _dialog.ShowWarning("Validation Failed: Sequence numbers must be between 1 and 12.");
                        return;
                    }
                }

                _logger.LogInfo($"Confirming {description}...", LogType.Audit);

                // Pulse 1 -> 0
                await _coreClient.WriteTagAsync(tagId, 1);
                await Task.Delay(2000);

                await _coreClient.WriteTagAsync(tagId, 0);

                await _servoService.SavePositionsAsync(Positions.ToList());

                _logger.LogInfo($"{description} Confirmed.", LogType.Audit);
            }
            catch (Exception ex) { _logger.LogError($"Confirm Error ({description}): {ex.Message}", LogType.Diagnostics); }
        }

        public void Dispose()
        {
            _liveDataTimer.Stop();
        }
    }

    public enum JogDirection
    {
        XPlus,
        XMinus,
        YPlus,
        YMinus
    }
}