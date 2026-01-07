using IPCSoftware.App.Helpers;
using IPCSoftware.App.NavServices;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public enum OperationMode
    {
        Auto,
        DryRun,
        Manual,
        CycleStop,
        MassRTO
    }

    public class ModeButtonItem : ObservableObjectVM
    {
        public OperationMode Mode { get; set; }
        public string Name { get; set; }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            // Only notify if value actually changes to prevent UI flickering
            set { if (_isEnabled != value) SetProperty(ref _isEnabled, value); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private bool _isBlinking;
        public bool IsBlinking
        {
            get => _isBlinking;
            set => SetProperty(ref _isBlinking, value);
        }



        public string BaseColor { get; set; }
        public string ActiveColor { get; set; }
    }


    public class ModeOfOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly INavigationService _navService;
        private readonly SafePoller _feedbackTimer;
        // Add this at the top of your class
        private readonly List<OperationMode> _activePulseModes = new List<OperationMode>();

        private readonly Dictionary<OperationMode, int> _writeTags = new();
        private readonly Dictionary<OperationMode, int> _statusTags = new(); // For Blinking/Color
        private readonly Dictionary<OperationMode, int> _enableTags = new(); // For IsEnabled

        public ObservableCollection<ModeButtonItem> ModeButtons { get; } = new ObservableCollection<ModeButtonItem>();
        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new();

        private bool _isMachineHome;
        public bool IsMachineHome { get => _isMachineHome; set => SetProperty(ref _isMachineHome, value); }

        public ICommand UnifiedOperationCommand { get; }
         
        public ModeOfOperationViewModel(IAppLogger logger, CoreClient coreClient, INavigationService navService) : base(logger)
        {
            _coreClient = coreClient;
            _navService = navService;

            InitializeTags();
            InitializeButtons();

            UnifiedOperationCommand = new RelayCommand<string>(async (args) => await ExecuteOperationAsync(args));

            _feedbackTimer = new SafePoller (TimeSpan.FromMilliseconds(100), FeedbackLoop_Tick);
            _feedbackTimer.Start();
        }

        private void InitializeTags()
        {
            // Write
            Map(OperationMode.Auto, ConstantValues.Mode_Auto, ConstantValues.Mode_Auto_Enable);
            Map(OperationMode.DryRun, ConstantValues.Mode_DryRun, ConstantValues.Mode_DryRun_Enable);
            Map(OperationMode.CycleStop, ConstantValues.Mode_CycleStop, ConstantValues.Mode_CycleStop_Enable);
            Map(OperationMode.MassRTO, ConstantValues.Mode_MassRTO, ConstantValues.Mode_MassRTO_Enable);
        }


        void Map(OperationMode m, TagPair tag, int enableTag)
        {
            _writeTags[m] = tag.Write;
            _statusTags[m] = tag.Read;
            _enableTags[m] = enableTag;
        }


        private void InitializeButtons()
        {
            var list = new List<ModeButtonItem>
            {
                new ModeButtonItem { Mode = OperationMode.Auto,      Name = "Auto Run",     BaseColor = "#008B8B", ActiveColor = "#00FFFF" },
                new ModeButtonItem { Mode = OperationMode.DryRun,    Name = "Dry Run",      BaseColor = "#8B008B", ActiveColor = "#FF00FF" },
                // Manual is usually always enabled unless you have a specific tag for it
                new ModeButtonItem { Mode = OperationMode.Manual,    Name = "Manual",       BaseColor = "#607D8B", ActiveColor = "#4CAF50", IsEnabled = true },
                new ModeButtonItem { Mode = OperationMode.CycleStop, Name = "Cycle Stop",   BaseColor = "#5D4037", ActiveColor = "#D7CCC8" },
                new ModeButtonItem { Mode = OperationMode.MassRTO,   Name = "Machine Home", BaseColor = "#795548", ActiveColor = "#FF5722" }
            };
            foreach (var item in list) ModeButtons.Add(item);
        }

        private async Task ExecuteOperationAsync(string args)
        {
            if (string.IsNullOrEmpty(args)) return;
            var parts = args.Split('|');
            if (!Enum.TryParse(parts[0], out OperationMode mode)) return;
            bool isPressed = bool.Parse(parts[1]);

            try
            {
                // 1. Manual Navigation
                if (mode == OperationMode.Manual)
                {
                    // Logic: Only navigate if the button is Enabled
                    var btn = GetBtn(OperationMode.Manual);
                    if (isPressed && btn.IsEnabled)
                    {
                        _navService.NavigateMain<ManualOperationView>();
                    }
                    return;
                }

                // 2. PLC Write (Only if button is Enabled)
                var buttonItem = GetBtn(mode);
                if (buttonItem.IsEnabled && _writeTags.TryGetValue(mode, out int tagId))
                {
                    int value = isPressed ? 1 : 0;
                    await _coreClient.WriteTagAsync(tagId, value);

                    if (isPressed) AddAudit($"Operator Pressed: {mode}");
                }

                /* if ((buttonItem.IsEnabled || _activePulseModes.Contains(mode))
             && _writeTags.TryGetValue(mode, out int tagId))
                 {
                     if (isPressed)
                     {
                         // --- LOCK: Prevent Feedback loop from disabling this button ---
                         if (!_activePulseModes.Contains(mode))
                         {
                             _activePulseModes.Add(mode);
                         }

                         try
                         {
                             // A. Send 1
                             await _coreClient.WriteTagAsync(tagId, 1);
                             AddAudit($"Operator Pressed: {mode}");

                             // B. Wait (Pulse Duration)
                             await Task.Delay(250);

                             // C. Send 0 (Crucial Step)
                             await _coreClient.WriteTagAsync(tagId, 0);
                         }
                         finally
                         {
                             // --- UNLOCK: Allow Feedback loop to take over again ---
                             if (_activePulseModes.Contains(mode))
                             {
                                 _activePulseModes.Remove(mode);
                             }
                         }
                     }
                 }*/


            }
            catch (Exception ex)
            {
                _logger.LogError($"Op Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task FeedbackLoop_Tick()
        {
            try
            {
                // Read range covering 11-14, 477-484 (Write, Enable, Status)
                // Assuming CoreClient can handle disjointed reads or you just read a large block.
                // If tags are far apart, you might need two calls or a block read. 
                var liveData = await _coreClient.GetIoValuesAsync(5);
                //Debug.Assert((liveData != null) && liveData.Count() > 0);
                if (liveData.Count < 1) return;

                Application.Current.Dispatcher.Invoke(() =>
                {

                    bool isAutoRunning = false;
                    bool isDryRunning = false;
                    bool isStopRunning = false;
                    bool isRTORunning = false;

                    foreach (var btn in ModeButtons)
                    {
                        // A. Update BLINKING/ACTIVE Status (from 481-483)
                        if (_statusTags.TryGetValue(btn.Mode, out int statusTagId))
                        {
                            if (liveData.TryGetValue(statusTagId, out object? val))
                            {
                                bool signal = Convert.ToBoolean(val);
                                btn.IsBlinking = signal;
                                btn.IsActive = signal;
                                if (btn.Mode == OperationMode.Auto) isAutoRunning = signal;
                                if (btn.Mode == OperationMode.DryRun) isDryRunning = signal;
                                if (btn.Mode == OperationMode.MassRTO) isRTORunning = signal;
                                if (btn.Mode == OperationMode.CycleStop) isStopRunning = signal;
                            }
                        }


                        //if (_activePulseModes.Contains(btn.Mode))
                        //{
                        //    // If we are currently pulsing this button (Sending 1... Waiting... Sending 0),
                        //    // FORCE it to stay Enabled. Ignore the PLC for a moment.
                        //    btn.IsEnabled = true;
                        //}
                        //else
                        //{
                        //    // Normal behavior: Let PLC decide
                        //    if (_enableTags.TryGetValue(btn.Mode, out int enableTagId))
                        //    {
                        //        if (liveData.TryGetValue(enableTagId, out object? val))
                        //        {
                        //            btn.IsEnabled = Convert.ToBoolean(val);
                        //        }
                        //    }
                        //}

                        bool writeTagStatus = false;

                        // B. Update ENABLE/DISABLE State (from 477-480)
                        if (_enableTags.TryGetValue(btn.Mode, out int enableTagId))
                        {
                            if (_writeTags.TryGetValue(btn.Mode, out int writeTabId))
                            {
                                if (liveData.TryGetValue(writeTabId, out object? writeVal))
                                {
                                    writeTagStatus = Convert.ToBoolean(writeVal);
                                }
                            }
                            if (liveData.TryGetValue(enableTagId, out object? val))
                            {
                                // PLC Logic dictates Enabled State directly
                                btn.IsEnabled = writeTagStatus || Convert.ToBoolean(val);
                            }
                        }
                        // Note: 'Manual' mode IsEnabled is skipped here as it has no tag in _enableTags map. 
                        // It stays True (default) or you can add logic if needed.
                    }

                    var manualBtn = GetBtn(OperationMode.Manual);

                    // If EITHER Auto OR Dry is running (True), Manual must be Disabled.
                    // If BOTH are stopped (False), Manual is Enabled.
                    bool isSystemBusy = isAutoRunning || isDryRunning || isStopRunning || isRTORunning;

                    manualBtn.IsEnabled = !isSystemBusy;

                    _enableTags.TryGetValue(OperationMode.MassRTO, out int homeLampId);
                    // C. Update Home Lamp
                    if (liveData.TryGetValue(homeLampId, out object? homeVal))
                    {
                        var homeLamp = Convert.ToBoolean(homeVal);
                        IsMachineHome = !homeLamp;
                    }
                });
            }   
            catch { }
        }

        private ModeButtonItem GetBtn(OperationMode mode) => ModeButtons.FirstOrDefault(b => b.Mode == mode) ?? new ModeButtonItem();

        private void AddAudit(string message)
        {
            if (AuditLogs.Count > 0 && AuditLogs.Last().Message == message && (DateTime.Now - DateTime.Parse(AuditLogs.Last().Time)).TotalSeconds < 2) return;
            if (AuditLogs.Count > 50) AuditLogs.RemoveAt(0);
            AuditLogs.Add(new AuditLogModel { Time = DateTime.Now.ToString("HH:mm:ss"), Message = message });
        }


        public void Dispose()
        {
                _feedbackTimer.Dispose();
        }
    }
}

  