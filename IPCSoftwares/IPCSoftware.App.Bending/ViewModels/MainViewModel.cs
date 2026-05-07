using System;
using System.Collections.ObjectModel;
using System.Timers;
using System.Windows.Threading;
using IPCSoftware.App.Bending.Models;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.UI.CommonViews.ViewModels;
using Microsoft.Extensions.Logging;


namespace IPCSoftware.App.Bending.ViewModels
{
    /// <summary>
    /// Main ViewModel for the Bending Machine HMI
    /// Handles lot tracking, parameter display, and real-time simulation
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private SystemStateModel _systemState;
        private BindingParametersModel _bending1Parameters;
        private BindingParametersModel _bending2Parameters;
        private BindingParametersModel _bending3Parameters;
        private string _inputRobotLot;
        private string _outputRobotLot;
        private int _cycleNumber;
        private int _cycleTime;
        private bool _tt1Pos0Active;
        private bool _tt1Pos1Active;
        private bool _tt1Pos2Active;
        private bool _tt1Pos3Active;
        private bool _tt2Pos0Active;
        private bool _tt2Pos1Active;
        private bool _tt2Pos2Active;
        private int _tt1CurrentPos;
        private int _tt2CurrentPos;
        private string _tt1CurrentLot;
        private string _tt2CurrentLot;
        private System.Timers.Timer _simulationTimer;
        private int _timeCounter;
        private Dispatcher _dispatcher;

        public SystemStateModel SystemState
        {
            get { return _systemState; }
            set { SetProperty(ref _systemState, value); }
        }

        public BindingParametersModel Bending1Parameters
        {
            get { return _bending1Parameters; }
            set { SetProperty(ref _bending1Parameters, value); }
        }

        public BindingParametersModel Bending2Parameters
        {
            get { return _bending2Parameters; }
            set { SetProperty(ref _bending2Parameters, value); }
        }

        public BindingParametersModel Bending3Parameters
        {
            get { return _bending3Parameters; }
            set { SetProperty(ref _bending3Parameters, value); }
        }

        public string InputRobotLot
        {
            get { return _inputRobotLot; }
            set { SetProperty(ref _inputRobotLot, value); }
        }

        public string OutputRobotLot
        {
            get { return _outputRobotLot; }
            set { SetProperty(ref _outputRobotLot, value); }
        }

        public int CycleNumber
        {
            get { return _cycleNumber; }
            set { SetProperty(ref _cycleNumber, value); }
        }

        public int CycleTime
        {
            get { return _cycleTime; }
            set { SetProperty(ref _cycleTime, value); }
        }

        public bool TT1_Pos0_Active
        {
            get { return _tt1Pos0Active; }
            set { SetProperty(ref _tt1Pos0Active, value); }
        }

        public bool TT1_Pos1_Active
        {
            get { return _tt1Pos1Active; }
            set { SetProperty(ref _tt1Pos1Active, value); }
        }

        public bool TT1_Pos2_Active
        {
            get { return _tt1Pos2Active; }
            set { SetProperty(ref _tt1Pos2Active, value); }
        }

        public bool TT1_Pos3_Active
        {
            get { return _tt1Pos3Active; }
            set { SetProperty(ref _tt1Pos3Active, value); }
        }

        public bool TT2_Pos0_Active
        {
            get { return _tt2Pos0Active; }
            set { SetProperty(ref _tt2Pos0Active, value); }
        }

        public bool TT2_Pos1_Active
        {
            get { return _tt2Pos1Active; }
            set { SetProperty(ref _tt2Pos1Active, value); }
        }

        public bool TT2_Pos2_Active
        {
            get { return _tt2Pos2Active; }
            set { SetProperty(ref _tt2Pos2Active, value); }
        }

        public int TT1_CurrentPosition
        {
            get { return _tt1CurrentPos; }
            set { SetProperty(ref _tt1CurrentPos, value); }
        }

        public int TT2_CurrentPosition
        {
            get { return _tt2CurrentPos; }
            set { SetProperty(ref _tt2CurrentPos, value); }
        }

        public string TT1_CurrentLot
        {
            get { return _tt1CurrentLot; }
            set { SetProperty(ref _tt1CurrentLot, value); }
        }

        public string TT2_CurrentLot
        {
            get { return _tt2CurrentLot; }
            set { SetProperty(ref _tt2CurrentLot, value); }
        }

        public ObservableCollection<string> ScannedPartsList { get; set; }
        public ObservableCollection<string> ProcessEventsList { get; set; }

        public MainViewModel(IAppLogger Logger)
                : base(Logger)
        {
            InitializeViewModel();
            StartSimulation();
        }

        private void InitializeViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            SystemState = new SystemStateModel();
            Bending1Parameters = new BindingParametersModel();
            Bending2Parameters = new BindingParametersModel();
            Bending3Parameters = new BindingParametersModel();
            ScannedPartsList = new ObservableCollection<string>();
            ProcessEventsList = new ObservableCollection<string>();

            // Initialize scanned parts list with all 10 lots
            for (int i = 1; i <= 10; i++)
            {
                string lotId = i.ToString().PadLeft(3, '0');
                ScannedPartsList.Add($"LOT-{lotId}");
            }

            InputRobotLot = "---";
            OutputRobotLot = "---";
            TT1_CurrentLot = "---";
            TT2_CurrentLot = "---";
            _timeCounter = 0;

            UpdateDisplay();
        }

        private void StartSimulation()
        {
            _simulationTimer = new System.Timers.Timer(2000); // 2 second interval
            _simulationTimer.Elapsed += OnSimulationTick;
            _simulationTimer.AutoReset = true;
            _simulationTimer.Start();
        }

        private void OnSimulationTick(object sender, ElapsedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                _timeCounter += 2;
                CycleTime = _timeCounter;

                // Move lots every 4 seconds
                if (_timeCounter % 4 == 0)
                {
                    MoveLots();
                }

                UpdateDisplay();
            });
        }

        private void MoveLots()
        {
            // 1. Transfer from TT-1 Pos 3 to TT-2 Pos 0
            if (!string.IsNullOrEmpty(SystemState.TT1_Pos3))
            {
                SystemState.TT2_Pos0 = SystemState.TT1_Pos3;
                AddProcessEvent($"Transfer Done: LOT-{SystemState.TT1_Pos3} to TT-2");
                SystemState.TT1_Pos3 = null;
            }

            // 2. Move TT-2: Pos 1 → Pos 2 (Exit), Pos 0 → Pos 1 (Bending3)
            if (!string.IsNullOrEmpty(SystemState.TT2_Pos1))
            {
                SystemState.TT2_Pos2 = SystemState.TT2_Pos1;
                AddProcessEvent($"Exit: LOT-{SystemState.TT2_Pos1} completed");
                SystemState.CompletedLots.Add(SystemState.TT2_Pos1);
                SystemState.TT2_Pos1 = null;
            }

            if (!string.IsNullOrEmpty(SystemState.TT2_Pos0))
            {
                SystemState.TT2_Pos1 = SystemState.TT2_Pos0;
                AddProcessEvent($"Bending 3 Start: LOT-{SystemState.TT2_Pos0}");
                SystemState.TT2_Pos0 = null;
            }

            // 3. Move TT-1: Pos 2 → Pos 3, Pos 1 → Pos 2, Pos 0 → Pos 1
            if (!string.IsNullOrEmpty(SystemState.TT1_Pos2))
            {
                SystemState.TT1_Pos3 = SystemState.TT1_Pos2;
                SystemState.TT1_Pos2 = null;
            }

            if (!string.IsNullOrEmpty(SystemState.TT1_Pos1))
            {
                SystemState.TT1_Pos2 = SystemState.TT1_Pos1;
                AddProcessEvent($"Bending 2 Start: LOT-{SystemState.TT1_Pos1}");
                SystemState.TT1_Pos1 = null;
            }

            if (!string.IsNullOrEmpty(SystemState.TT1_Pos0))
            {
                SystemState.TT1_Pos1 = SystemState.TT1_Pos0;
                AddProcessEvent($"Bending 1 Start: LOT-{SystemState.TT1_Pos0}");
                SystemState.TT1_Pos0 = null;
            }

            // 4. New lot enters if available
            if (string.IsNullOrEmpty(SystemState.TT1_Pos0) && !string.IsNullOrEmpty(SystemState.NextLotToEnter))
            {
                SystemState.TT1_Pos0 = SystemState.NextLotToEnter;
                AddProcessEvent($"Entry: LOT-{SystemState.NextLotToEnter} scanned");

                int nextNum = int.Parse(SystemState.NextLotToEnter) + 1;
                SystemState.NextLotToEnter = nextNum <= 10 ? nextNum.ToString().PadLeft(3, '0') : null;
            }
        }

        private void UpdateDisplay()
        {
            // Update position indicators
            TT1_Pos0_Active = !string.IsNullOrEmpty(SystemState.TT1_Pos0);
            TT1_Pos1_Active = !string.IsNullOrEmpty(SystemState.TT1_Pos1);
            TT1_Pos2_Active = !string.IsNullOrEmpty(SystemState.TT1_Pos2);
            TT1_Pos3_Active = !string.IsNullOrEmpty(SystemState.TT1_Pos3);

            TT2_Pos0_Active = !string.IsNullOrEmpty(SystemState.TT2_Pos0);
            TT2_Pos1_Active = !string.IsNullOrEmpty(SystemState.TT2_Pos1);
            TT2_Pos2_Active = !string.IsNullOrEmpty(SystemState.TT2_Pos2);

            // Update current position displays
            if (TT1_Pos0_Active) TT1_CurrentPosition = 0;
            else if (TT1_Pos1_Active) TT1_CurrentPosition = 1;
            else if (TT1_Pos2_Active) TT1_CurrentPosition = 2;
            else if (TT1_Pos3_Active) TT1_CurrentPosition = 3;

            if (TT2_Pos0_Active) TT2_CurrentPosition = 0;
            else if (TT2_Pos1_Active) TT2_CurrentPosition = 1;
            else if (TT2_Pos2_Active) TT2_CurrentPosition = 2;

            TT1_CurrentLot = SystemState.TT1_Pos0 ?? SystemState.TT1_Pos1 ?? SystemState.TT1_Pos2 ?? SystemState.TT1_Pos3 ?? "---";
            TT2_CurrentLot = SystemState.TT2_Pos0 ?? SystemState.TT2_Pos1 ?? SystemState.TT2_Pos2 ?? "---";

            // Update parameter tiles
            UpdateParameterTiles();

            // Update robot displays
            InputRobotLot = !string.IsNullOrEmpty(SystemState.TT1_Pos0) ? $"LOT: {SystemState.TT1_Pos0}" : "LOT: ---";
            OutputRobotLot = !string.IsNullOrEmpty(SystemState.TT2_Pos2) ? $"LOT: {SystemState.TT2_Pos2}" : "LOT: ---";

            // Update cycle info
            CycleNumber = int.Parse(SystemState.NextLotToEnter ?? "11");
        }

        private void UpdateParameterTiles()
        {
            // Bending 1 (TT-1 Pos 1)
            if (!string.IsNullOrEmpty(SystemState.TT1_Pos1))
            {
                var lot = LotManager.GetLot(SystemState.TT1_Pos1);
                if (lot != null)
                {
                    Bending1Parameters.LotId = $"LOT: {lot.Id}";
                    Bending1Parameters.Temp1 = lot.Temperature[0];
                    Bending1Parameters.Temp2 = lot.Temperature[1];
                    Bending1Parameters.Temp3 = lot.Temperature[2];
                    Bending1Parameters.Temp4 = lot.Temperature[3];
                    Bending1Parameters.Pressure1 = lot.Pressure[0];
                    Bending1Parameters.Pressure2 = lot.Pressure[1];
                    Bending1Parameters.Pressure3 = lot.Pressure[2];
                    Bending1Parameters.Pressure4 = lot.Pressure[3];
                    Bending1Parameters.QRCode1 = lot.QRCodes.Count > 0 ? lot.QRCodes[0] : "";
                    Bending1Parameters.QRCode2 = lot.QRCodes.Count > 1 ? lot.QRCodes[1] : "";
                    Bending1Parameters.QRCode3 = lot.QRCodes.Count > 2 ? lot.QRCodes[2] : "";
                    Bending1Parameters.QRCode4 = lot.QRCodes.Count > 3 ? lot.QRCodes[3] : "";
                }
            }
            else
            {
                ResetParameterTile(Bending1Parameters);
            }

            // Bending 2 (TT-1 Pos 2)
            if (!string.IsNullOrEmpty(SystemState.TT1_Pos2))
            {
                var lot = LotManager.GetLot(SystemState.TT1_Pos2);
                if (lot != null)
                {
                    Bending2Parameters.LotId = $"LOT: {lot.Id}";
                    Bending2Parameters.Temp1 = lot.Temperature[0];
                    Bending2Parameters.Temp2 = lot.Temperature[1];
                    Bending2Parameters.Temp3 = lot.Temperature[2];
                    Bending2Parameters.Temp4 = lot.Temperature[3];
                    Bending2Parameters.Pressure1 = lot.Pressure[0];
                    Bending2Parameters.Pressure2 = lot.Pressure[1];
                    Bending2Parameters.Pressure3 = lot.Pressure[2];
                    Bending2Parameters.Pressure4 = lot.Pressure[3];
                    Bending2Parameters.QRCode1 = lot.QRCodes.Count > 0 ? lot.QRCodes[0] : "";
                    Bending2Parameters.QRCode2 = lot.QRCodes.Count > 1 ? lot.QRCodes[1] : "";
                    Bending2Parameters.QRCode3 = lot.QRCodes.Count > 2 ? lot.QRCodes[2] : "";
                    Bending2Parameters.QRCode4 = lot.QRCodes.Count > 3 ? lot.QRCodes[3] : "";
                }
            }
            else
            {
                ResetParameterTile(Bending2Parameters);
            }

            // Bending 3 (TT-2 Pos 1)
            if (!string.IsNullOrEmpty(SystemState.TT2_Pos1))
            {
                var lot = LotManager.GetLot(SystemState.TT2_Pos1);
                if (lot != null)
                {
                    Bending3Parameters.LotId = $"LOT: {lot.Id}";
                    Bending3Parameters.Temp1 = lot.Temperature[0];
                    Bending3Parameters.Temp2 = lot.Temperature[1];
                    Bending3Parameters.Temp3 = lot.Temperature[2];
                    Bending3Parameters.Temp4 = lot.Temperature[3];
                    Bending3Parameters.Pressure1 = lot.Pressure[0];
                    Bending3Parameters.Pressure2 = lot.Pressure[1];
                    Bending3Parameters.Pressure3 = lot.Pressure[2];
                    Bending3Parameters.Pressure4 = lot.Pressure[3];
                    Bending3Parameters.QRCode1 = lot.QRCodes.Count > 0 ? lot.QRCodes[0] : "";
                    Bending3Parameters.QRCode2 = lot.QRCodes.Count > 1 ? lot.QRCodes[1] : "";
                    Bending3Parameters.QRCode3 = lot.QRCodes.Count > 2 ? lot.QRCodes[2] : "";
                    Bending3Parameters.QRCode4 = lot.QRCodes.Count > 3 ? lot.QRCodes[3] : "";
                }
            }
            else
            {
                ResetParameterTile(Bending3Parameters);
            }
        }

        private void ResetParameterTile(BindingParametersModel parameters)
        {
            parameters.LotId = "LOT: ---";
            parameters.Temp1 = 0;
            parameters.Temp2 = 0;
            parameters.Temp3 = 0;
            parameters.Temp4 = 0;
            parameters.Pressure1 = 0;
            parameters.Pressure2 = 0;
            parameters.Pressure3 = 0;
            parameters.Pressure4 = 0;
            parameters.QRCode1 = "";
            parameters.QRCode2 = "";
            parameters.QRCode3 = "";
            parameters.QRCode4 = "";
        }

        private void AddProcessEvent(string eventText)
        {
            SystemState.ProcessEvents.Add(eventText);
            ProcessEventsList.Insert(0, eventText);

            // Keep only last 10 events
            if (ProcessEventsList.Count > 10)
            {
                ProcessEventsList.RemoveAt(ProcessEventsList.Count - 1);
            }
        }

        public void Shutdown()
        {
            _simulationTimer?.Stop();
            _simulationTimer?.Dispose();
        }
    }
}
