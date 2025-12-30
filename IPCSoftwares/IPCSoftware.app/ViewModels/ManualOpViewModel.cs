
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{

    public class ManualOpViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly DispatcherTimer _feedbackTimer;

        // --- Tag Maps ---
        private readonly Dictionary<ManualOperationMode, int> _writeTags = new();
        private readonly Dictionary<ManualOperationMode, int> _readTags = new();

        // --- Collections ---
        public ObservableCollection<ModeItem> Modes { get; }

        // --- Commands ---
        public ICommand UnifiedOperationCommand { get; }
        public ICommand OriginCommand { get; }

        // --- Filtered Lists for UI ItemsControl ---
        public IEnumerable<ModeItem> GridPositionModes => Modes.Where(x => x.Group == "Move to Position" && x.Mode != ManualOperationMode.MoveToPos0);

        // --- Individual Properties for UI Binding (Reduced Logic) ---
        // These look up the ModeItem in the list dynamically to save state management code
        public bool IsTrayUpActive => GetState(ManualOperationMode.TrayLiftUp);
        public bool IsTrayDownActive => GetState(ManualOperationMode.TrayLiftDown);

        public bool IsCylUpActive => GetState(ManualOperationMode.PositioningCylinderUp);
        public bool IsCylDownActive => GetState(ManualOperationMode.PositioningCylinderDown);

        public bool IsConvFwdActive => GetState(ManualOperationMode.TransportConveyorForward);
        public bool IsConvRevActive => GetState(ManualOperationMode.TransportConveyorReverse);
        public bool IsConvStopActive => GetState(ManualOperationMode.TransportConveyorStop);
        public bool IsConvLowActive => GetState(ManualOperationMode.TransportConveyorLowSpeed);
        public bool IsConvHighActive => GetState(ManualOperationMode.TransportConveyorHighSpeed);

        public bool IsJogXMinusActive => GetState(ManualOperationMode.ManualXAxisJogBackward);
        public bool IsJogXPlusActive => GetState(ManualOperationMode.ManualXAxisJogForward);
        public bool IsJogYMinusActive => GetState(ManualOperationMode.ManualYAxisJogBackward);
        public bool IsJogYPlusActive => GetState(ManualOperationMode.ManualYAxisJogForward);

        public bool IsPos0Active => GetState(ManualOperationMode.MoveToPos0);

        // Helper to get state from the collection
        private bool GetState(ManualOperationMode mode) => Modes.FirstOrDefault(x => x.Mode == mode)?.IsActive ?? false;


        public ManualOpViewModel(IAppLogger logger, CoreClient coreClient) : base(logger)
        {
            _coreClient = coreClient;

            // 1. Initialize Modes List
            Modes = new ObservableCollection<ModeItem>(
                Enum.GetValues(typeof(ManualOperationMode))
                    .Cast<ManualOperationMode>()
                    .Select(m => new ModeItem { Mode = m, Group = GetGroupName(m) }));

            // 2. Map All Tags
            InitializeTags();

            // 3. Unified Command used by EVERY button
            UnifiedOperationCommand = new RelayCommand<string>(async (args) => await ExecuteOperationAsync(args));

            // 4. Origin Command
            OriginCommand = new RelayCommand(async () =>
            {
                await _coreClient.WriteTagAsync(113, 1);
                await Task.Delay(200);
                await _coreClient.WriteTagAsync(113, 0);
            });

            // 5. Feedback Timer
            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _feedbackTimer.Tick += FeedbackLoop_Tick;
            _feedbackTimer.Start();
        }

        private void InitializeTags()
        {
            int wStart = 40; // Write Start
            int rStart = 80; // Read Start

            // Helper to map tags quickly
            void Map(ManualOperationMode m, int offset)
            {
                _writeTags[m] = wStart + offset;
                _readTags[m] = rStart + offset;
            }

            // --- Manual Controls ---
            Map(ManualOperationMode.TrayLiftDown, 0);
            Map(ManualOperationMode.TrayLiftUp, 1);
            Map(ManualOperationMode.PositioningCylinderUp, 2);
            Map(ManualOperationMode.PositioningCylinderDown, 3);
            Map(ManualOperationMode.TransportConveyorForward, 4);
            Map(ManualOperationMode.TransportConveyorReverse, 5);
            Map(ManualOperationMode.TransportConveyorStop, 6);
            Map(ManualOperationMode.TransportConveyorLowSpeed, 7);
            Map(ManualOperationMode.TransportConveyorHighSpeed, 8);
            Map(ManualOperationMode.ManualXAxisJogForward, 9);
            Map(ManualOperationMode.ManualXAxisJogBackward, 10);
            // Skipping 11, 12 reserved for jogging speed if needed
            Map(ManualOperationMode.ManualYAxisJogBackward, 13);
            Map(ManualOperationMode.ManualYAxisJogForward, 14);

            // --- Positions 0 to 12 ---
            // These start at offset 20 (Tags 60/100)
            int posOffset = 17;
            for (int i = 0; i <= 12; i++)
            {
                if (Enum.TryParse($"MoveToPos{i}", out ManualOperationMode mode))
                {
                    Map(mode, posOffset + i);
                }
            }
        }

        private async Task ExecuteOperationAsync(string args)
        {
            if (string.IsNullOrEmpty(args)) return;
            var parts = args.Split('|');
            if (!Enum.TryParse(parts[0], out ManualOperationMode mode)) return;
            bool isPressed = bool.Parse(parts[1]);

            if (!_writeTags.TryGetValue(mode, out int tagId)) return;

            // Optional: Add Interlock logic here if needed (e.g., if(IsTrayUp && mode==TrayDown) return;)

            try
            {

                // Write 1 on Press, 0 on Release
                int value = isPressed ? 1 : 0;
                await _coreClient.WriteTagAsync(tagId, value);

                // UI Optimization: Immediately clear specific flags on Press
                //if (isPressed && mode == ManualOperationMode.TransportConveyorStop)
                //{
                //    await StopConveyorLogic(tagId);
                //    // Immediate UI reset logic if required by UX
                //}

                _logger.LogInfo($"OP: {mode} -> {value} (Tag {tagId})", LogType.Audit);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Op Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task StopConveyorLogic(int stopTagId)
        {
            // A. Visually turn off other buttons immediately
            // Note: This relies on the property change notifications to update the UI
            var fwd = Modes.First(x => x.Mode == ManualOperationMode.TransportConveyorForward);
            var rev = Modes.First(x => x.Mode == ManualOperationMode.TransportConveyorReverse);
            var low = Modes.First(x => x.Mode == ManualOperationMode.TransportConveyorLowSpeed);
            var high = Modes.First(x => x.Mode == ManualOperationMode.TransportConveyorHighSpeed);

            fwd.IsActive = false;
            rev.IsActive = false;
            low.IsActive = false;
            high.IsActive = false;

            // Trigger UI updates
            OnPropertyChanged(nameof(IsConvFwdActive));
            OnPropertyChanged(nameof(IsConvRevActive));
            OnPropertyChanged(nameof(IsConvLowActive));
            OnPropertyChanged(nameof(IsConvHighActive));

            // B. Send Stop Command to PLC (Pulse)
            await _coreClient.WriteTagAsync(stopTagId, 1);

            // C. Safety: Turn off the Motion/Speed Write Tags in PLC
            if (_writeTags.TryGetValue(ManualOperationMode.TransportConveyorForward, out int tFwd)) await _coreClient.WriteTagAsync(tFwd, 0);
            if (_writeTags.TryGetValue(ManualOperationMode.TransportConveyorReverse, out int tRev)) await _coreClient.WriteTagAsync(tRev, 0);
            if (_writeTags.TryGetValue(ManualOperationMode.TransportConveyorLowSpeed, out int tLow)) await _coreClient.WriteTagAsync(tLow, 0);
            if (_writeTags.TryGetValue(ManualOperationMode.TransportConveyorHighSpeed, out int tHigh)) await _coreClient.WriteTagAsync(tHigh, 0);

            // D. Finish Pulse
            await Task.Delay(100);
            await _coreClient.WriteTagAsync(stopTagId, 0);
        }

        private async void FeedbackLoop_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Read enough tags to cover all buttons (e.g., 80 to 150)
                var liveData = await _coreClient.GetIoValuesAsync(5);
                if (liveData == null || !liveData.Any()) return;

                foreach (var item in Modes)
                {
                    if (_readTags.TryGetValue(item.Mode, out int readTag))
                    {
                        if (liveData.TryGetValue(readTag, out object val))
                        {
                            bool newState = Convert.ToBoolean(val);
                            if (item.IsActive != newState)
                            {
                                item.IsActive = newState;
                                // Notify UI of individual property changes
                                OnPropertyChanged(nameof(IsTrayUpActive)); // Triggers generic UI update
                                OnPropertyChanged(nameof(IsTrayDownActive));
                                OnPropertyChanged(nameof(IsCylUpActive));
                                OnPropertyChanged(nameof(IsCylDownActive));
                                OnPropertyChanged(nameof(IsConvFwdActive));
                                OnPropertyChanged(nameof(IsConvRevActive));
                                OnPropertyChanged(nameof(IsConvStopActive));
                                OnPropertyChanged(nameof(IsConvLowActive));
                                OnPropertyChanged(nameof(IsConvHighActive));
                                OnPropertyChanged(nameof(IsJogXMinusActive));
                                OnPropertyChanged(nameof(IsJogXPlusActive));
                                OnPropertyChanged(nameof(IsJogYMinusActive));
                                OnPropertyChanged(nameof(IsJogYPlusActive));
                                OnPropertyChanged(nameof(IsPos0Active));
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private string GetGroupName(ManualOperationMode mode)
        {
            var memInfo = mode.GetType().GetMember(mode.ToString());
            var attr = memInfo[0].GetCustomAttributes(typeof(GroupAttribute), false).FirstOrDefault() as GroupAttribute;
            return attr?.Name ?? "Other";
        }

        public void Dispose() => _feedbackTimer.Stop();
    }

}