using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class ServoParameterItem : ObservableObjectVM
    {
        public string Name { get; set; }
        public int ReadTagId { get; set; }
        public int WriteTagId { get; set; }

        private double _currentValue;
        public double CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }

        private double _newValue;
        public double NewValue
        {
            get => _newValue;
            set => SetProperty(ref _newValue, value);
        }
    }

    public class ServoCalibrationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly DispatcherTimer _liveDataTimer;

        // --- TAG CONFIGURATION ---
        // Control Bits (110-113)
        private const int TAG_PARAM_A1 = 110; // Confirm X Servo Params
        private const int TAG_PARAM_A2 = 111; // Confirm Y Servo Params
        private const int TAG_PARAM_A3 = 112; // Confirm X Coordinates
        private const int TAG_PARAM_A4 = 113; // Confirm Y Coordinates

        // Live Positions (B1, B2)
        private const int TAG_LIVE_X = 154;
        private const int TAG_LIVE_Y = 155;

        // Start of Coordinate Registers (13 Positions: 0 to 12)
        private const int START_TAG_POS_X = 128;
        private const int START_TAG_POS_Y = 141;

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

        // Commands
        public ICommand TeachCommand { get; }
        public ICommand WriteParamCommand { get; }

        // 4 Separate Confirm Commands
        public ICommand ConfirmXParamsCommand { get; }
        public ICommand ConfirmYParamsCommand { get; }
        public ICommand ConfirmXCoordsCommand { get; }
        public ICommand ConfirmYCoordsCommand { get; }

        public ServoCalibrationViewModel(CoreClient coreClient, IAppLogger logger)
             : base(logger)
        {
            _coreClient = coreClient;

            TeachCommand = new RelayCommand<ServoPositionModel>(OnTeachPosition);
            WriteParamCommand = new RelayCommand<ServoParameterItem>(OnWriteParameter);

            ConfirmXParamsCommand = new RelayCommand(async () => await PulseBit(TAG_PARAM_A1, "X Servo Params"));
            ConfirmYParamsCommand = new RelayCommand(async () => await PulseBit(TAG_PARAM_A2, "Y Servo Params"));
            ConfirmXCoordsCommand = new RelayCommand(async () => await PulseBit(TAG_PARAM_A3, "X Coordinates"));
            ConfirmYCoordsCommand = new RelayCommand(async () => await PulseBit(TAG_PARAM_A4, "Y Coordinates"));

            InitializeParameters();
            InitializePositions();

            _liveDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _liveDataTimer.Tick += OnLiveDataTick;
            _liveDataTimer.Start();
        }

        private void InitializeParameters()
        {
            // X Axis Params (114-120) -> Bind to XParameters
            XParameters.Add(new ServoParameterItem { Name = "Jog Low Speed", ReadTagId = 114, WriteTagId = 114 });
            XParameters.Add(new ServoParameterItem { Name = "Jog High Speed", ReadTagId = 115, WriteTagId = 115 });
            XParameters.Add(new ServoParameterItem { Name = "Inching Dist", ReadTagId = 116, WriteTagId = 116 });
            XParameters.Add(new ServoParameterItem { Name = "Origin Offset", ReadTagId = 117, WriteTagId = 117 });
            XParameters.Add(new ServoParameterItem { Name = "Move Speed", ReadTagId = 118, WriteTagId = 118 });
            XParameters.Add(new ServoParameterItem { Name = "Acceleration", ReadTagId = 119, WriteTagId = 119 });
            XParameters.Add(new ServoParameterItem { Name = "Deceleration", ReadTagId = 120, WriteTagId = 120 });

            // Y Axis Params (121-127) -> Bind to YParameters
            YParameters.Add(new ServoParameterItem { Name = "Jog Low Speed", ReadTagId = 121, WriteTagId = 121 });
            YParameters.Add(new ServoParameterItem { Name = "Jog High Speed", ReadTagId = 122, WriteTagId = 122 });
            YParameters.Add(new ServoParameterItem { Name = "Inching Dist", ReadTagId = 123, WriteTagId = 123 });
            YParameters.Add(new ServoParameterItem { Name = "Origin Offset", ReadTagId = 124, WriteTagId = 124 });
            YParameters.Add(new ServoParameterItem { Name = "Move Speed", ReadTagId = 125, WriteTagId = 125 });
            YParameters.Add(new ServoParameterItem { Name = "Acceleration", ReadTagId = 126, WriteTagId = 126 });
            YParameters.Add(new ServoParameterItem { Name = "Deceleration", ReadTagId = 127, WriteTagId = 127 });
        }

        private void InitializePositions()
        {
            Positions.Clear();
            for (int i = 0; i <= 12; i++)
            {
                Positions.Add(new ServoPositionModel
                {
                    PositionId = i,
                    Name = i == 0 ? "Home Position" : $"Position {i}",
                    X = 0,
                    Y = 0
                });
            }
        }

        private async void OnLiveDataTick(object sender, EventArgs e)
        {
            try
            {
                // Request IO Packet (ID 5 assumed to cover all tags)
                var data = await _coreClient.GetIoValuesAsync(5);

                if (data != null)
                {
                    // 1. Update Live Position
                    if (data.TryGetValue(TAG_LIVE_X, out object xVal)) LiveX = Convert.ToDouble(xVal);
                    if (data.TryGetValue(TAG_LIVE_Y, out object yVal)) LiveY = Convert.ToDouble(yVal);

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

                    // 4. Update Position List (Read stored values from PLC)
                    for (int i = 0; i < Positions.Count; i++)
                    {
                        int xTag = START_TAG_POS_X + i;
                        int yTag = START_TAG_POS_Y + i;

                        if (data.TryGetValue(xTag, out object valX)) Positions[i].X = Convert.ToDouble(valX);
                        if (data.TryGetValue(yTag, out object valY)) Positions[i].Y = Convert.ToDouble(valY);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Live Data Error: {ex.Message}");
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

                // Optimistic UI Update
                position.X = LiveX;
                position.Y = LiveY;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Teach Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void OnWriteParameter(ServoParameterItem param)
        {
            if (param == null) return;
            try
            {
                _logger.LogInfo($"Writing {param.Name} -> {param.NewValue}", LogType.Audit);
                await _coreClient.WriteTagAsync(param.WriteTagId, param.NewValue);
            }
            catch (Exception ex) { _logger.LogError($"Write Param Error: {ex.Message}", LogType.Diagnostics); }
        }

        private async Task PulseBit(int tagId, string description)
        {
            try
            {
                _logger.LogInfo($"Confirming {description}...", LogType.Audit);

                // Pulse 1 -> 0
                await _coreClient.WriteTagAsync(tagId, 1);
                await Task.Delay(100);
                //await _coreClient.WriteTagAsync(tagId, 0);

                _logger.LogInfo($"{description} Confirmed.", LogType.Audit);
            }
            catch (Exception ex) { _logger.LogError($"Confirm Error ({description}): {ex.Message}", LogType.Diagnostics); }
        }

        public void Dispose()
        {
            _liveDataTimer.Stop();
        }
    }
}