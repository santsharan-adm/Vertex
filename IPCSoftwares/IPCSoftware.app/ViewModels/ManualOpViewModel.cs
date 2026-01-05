
using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
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
        private readonly SafePoller _feedbackTimer;
        private readonly INavigationService _nav;

        // --- Tag Maps ---
        private readonly Dictionary<ManualOperationMode, int> _writeTags = new();
        private readonly Dictionary<ManualOperationMode, int> _readTags = new();

        // --- Collections ---
        public ObservableCollection<ModeItem> Modes { get; }

        // --- Commands ---
        public ICommand UnifiedOperationCommand { get; }
        public ICommand OriginCommand { get; }
        public ICommand NavigateBackCommand { get; }

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


        public ManualOpViewModel(IAppLogger logger, CoreClient coreClient, INavigationService nav) : base(logger)
        {
            _coreClient = coreClient;
            _nav = nav;

            // 1. Initialize Modes List
            Modes = new ObservableCollection<ModeItem>(
                Enum.GetValues(typeof(ManualOperationMode))
                    .Cast<ManualOperationMode>()
                    .Select(m => new ModeItem { Mode = m, Group = GetGroupName(m) }));

            // 2. Map All Tags
            InitializeTags();

            // 3. Unified Command used by EVERY button
            UnifiedOperationCommand = new RelayCommand<string>(async (args) => await ExecuteOperationAsync(args));
            NavigateBackCommand = new RelayCommand(OnBackClick);

            // 4. Origin Command
            OriginCommand = new RelayCommand(async () =>
            {
                await _coreClient.WriteTagAsync(ConstantValues.Servo_XYOrigin, 1);
                await Task.Delay(200);
                await _coreClient.WriteTagAsync(ConstantValues.Servo_XYOrigin, 0);
            });

            // 5. Feedback Timer
            _feedbackTimer = new SafePoller ( TimeSpan.FromMilliseconds(100), FeedbackLoop_Tick);
            _feedbackTimer.Start();
        }

        private void InitializeTags()
        {
            // --- Manual Controls ---
            Map(ManualOperationMode.TrayLiftDown, ConstantValues.Manual_TrayDown);
            Map(ManualOperationMode.TrayLiftUp, ConstantValues.Manual_TrayUp);
            Map(ManualOperationMode.PositioningCylinderUp, ConstantValues.Manual_CylUp);
            Map(ManualOperationMode.PositioningCylinderDown, ConstantValues.Manual_CylDown);
            Map(ManualOperationMode.TransportConveyorForward, ConstantValues.Manual_ConvFwd);
            Map(ManualOperationMode.TransportConveyorReverse, ConstantValues.Manual_ConvRev);
            Map(ManualOperationMode.TransportConveyorStop, ConstantValues.Manual_ConvStop);
            Map(ManualOperationMode.TransportConveyorLowSpeed, ConstantValues.Manual_ConvLow);
            Map(ManualOperationMode.TransportConveyorHighSpeed, ConstantValues.Manual_ConvHigh);
            Map(ManualOperationMode.ManualXAxisJogForward, ConstantValues.Manual_XFwd);
            Map(ManualOperationMode.ManualXAxisJogBackward, ConstantValues.Manual_XRev);
            // Skipping 11, 12 reserved for jogging speed if needed
            Map(ManualOperationMode.ManualYAxisJogForward, ConstantValues.Manual_YFwd);
            Map(ManualOperationMode.ManualYAxisJogBackward, ConstantValues.Manual_YRev);

            // --- Positions 0 to 12 ---
            // These start at offset 20 (Tags 60/100)
            TagPair posOffset = ConstantValues.Manual_PosStart;
            for (int i = 0; i <= 12; i++)
            {
                if (Enum.TryParse($"MoveToPos{i}", out ManualOperationMode mode))
                {
                    posOffset = new TagPair
                    {
                        Write = ConstantValues.Manual_PosStart.Write + i,
                        Read = ConstantValues.Manual_PosStart.Read + i
                    };
                    Map(mode, posOffset);
                }
            }
        }

        private void  OnBackClick()
        {
            Dispose();
             _nav.NavigateMain<ModeOfOperation>();
        }   
        void Map(ManualOperationMode m, TagPair tag)
        {
            _writeTags[m] = tag.Write;
            _readTags[m] = tag.Read;
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

        private async Task FeedbackLoop_Tick()
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

        public void Dispose()
        {
            _feedbackTimer.Dispose();
        }
    }

}