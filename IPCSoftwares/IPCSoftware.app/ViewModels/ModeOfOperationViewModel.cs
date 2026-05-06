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
        private readonly List<OperationMode> _activePulseModes = new List<OperationMode>();
        private readonly IServoCalibrationService _servoService; // Added by Rishabh -Date 06-05-2026
        private readonly IDialogService _dialog;

        private readonly Dictionary<OperationMode, int> _writeTags = new();
        private readonly Dictionary<OperationMode, int> _statusTags = new();
        private readonly Dictionary<OperationMode, int> _enableTags = new();

        public ObservableCollection<ModeButtonItem> ModeButtons { get; } = new ObservableCollection<ModeButtonItem>();
        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new();

        // Added by Rishabh -Date -06-05-2026
        private ObservableCollection<RecipeItem> _recipeList;
        private RecipeItem _lastConfirmedRecipe;
        public ObservableCollection<RecipeItem> RecipeList
        {
            get => _recipeList;
            set => SetProperty(ref _recipeList, value);
        }

        private RecipeItem _selectedRecipe;
        public RecipeItem SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                var newRecipe = value;
                if (SetProperty(ref _selectedRecipe, value) && value != null)
                {
                    OnRecipeSelectionChanged(newRecipe , _lastConfirmedRecipe);
                }
            }
        }

        public bool IsRecipeSelectionEnabled => !GetBtn(OperationMode.Auto).IsEnabled;

        private bool _isMachineHome;
        public bool IsMachineHome { get => _isMachineHome; set => SetProperty(ref _isMachineHome, value); }

        public ICommand UnifiedOperationCommand { get; }

        public ModeOfOperationViewModel(IAppLogger logger, CoreClient coreClient, INavigationService navService, IServoCalibrationService servoService , IDialogService dialog) : base(logger)
        {
            _coreClient = coreClient;
            _navService = navService;
            _servoService = servoService; // Added by rishabh - Date 06-05-2026
            _dialog =  dialog;
            InitializeTags();
            InitializeButtons();
            _= InitializeRecipesAsync(); // Initialize Recipe List

            UnifiedOperationCommand = new RelayCommand<string>(async (args) => await ExecuteOperationAsync(args));

            _feedbackTimer = new SafePoller(TimeSpan.FromMilliseconds(100), FeedbackLoop_Tick);
            _feedbackTimer.Start();
        }

        //Added by Rishabh -Date -06-05-2026 , Initialize Recipe List from ServoConfigService
        private async Task InitializeRecipesAsync()
        {
            try
            {
                var savedRecipes = await _servoService.LoadRecipeAsync();

                RecipeList = new ObservableCollection<RecipeItem>(
                    savedRecipes.Select(r => new RecipeItem
                    {
                        ProgramNo = r.ProgramNo,
                        Name = $"Progam {r.ProgramNo}" 
                    })
                );

                // Set default selection to first recipe
                SelectedRecipe = RecipeList.FirstOrDefault();
                _selectedRecipe = RecipeList.FirstOrDefault();
                _lastConfirmedRecipe = _selectedRecipe; // Track initial selection
                OnPropertyChanged(nameof(SelectedRecipe));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load recipes: {ex.Message}", LogType.Diagnostics);

                // Fallback to hardcoded list if file read fails
                RecipeList = new ObservableCollection<RecipeItem>
                {
                    new RecipeItem { ProgramNo = 1, Name = "Program 1" },
                    new RecipeItem { ProgramNo = 2, Name = "Program 2" },
                    new RecipeItem { ProgramNo = 3, Name = "Program 3" },
                    new RecipeItem { ProgramNo = 4, Name = "Program 4" },
                    new RecipeItem { ProgramNo = 5, Name = "Program 5" }
                };

                SelectedRecipe = RecipeList.FirstOrDefault();
            }
        }

        // // Added by Rishabh -Date -06-05-2026 ,  Handle Recipe Selection Change
        private async void OnRecipeSelectionChanged(RecipeItem newRecipe , RecipeItem previousRecipe)
        {
            try
            {
                if (previousRecipe != null && newRecipe.ProgramNo == previousRecipe.ProgramNo) { return; } // No change in selection
                bool confirm = _dialog.ShowYesNo($"Are you sure you want to load {newRecipe.Name} ?", "Confirmation");
                if (confirm)
                {
                    _lastConfirmedRecipe = newRecipe;
                    _logger.LogInfo($"Recipe Selected: Program {newRecipe.ProgramNo} - {newRecipe.Name}", LogType.Audit);
                    AddAudit($"Program Number Changed: {newRecipe.Name}");
                }
                else
                {
                    _selectedRecipe = previousRecipe;
                    OnPropertyChanged(nameof(SelectedRecipe));
                }

                //  Write selected recipe/program number to PLC
                //  await _coreClient.WriteTagAsync(RECIPE_TAG_ID, recipe.ProgramNo);

                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Recipe Selection Error: {ex.Message}", LogType.Diagnostics);
                _selectedRecipe = previousRecipe;
                OnPropertyChanged(nameof(SelectedRecipe));
            }
        }

        private void InitializeTags()
        {
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
            new ModeButtonItem { Mode = OperationMode.Auto,      Name = "Auto Run",     BaseColor = "#00BCFE", ActiveColor = "#00BCFE" },
            new ModeButtonItem { Mode = OperationMode.DryRun,    Name = "Dry Run",      BaseColor = "#B200A1", ActiveColor = "#7E1A74" },
            new ModeButtonItem { Mode = OperationMode.Manual,    Name = "Manual",       BaseColor = "#607D8B", ActiveColor = "#607D8B", IsEnabled = true },
            new ModeButtonItem { Mode = OperationMode.CycleStop, Name = "Cycle Stop",   BaseColor = "#FE8848", ActiveColor = "#FE8848" },
            new ModeButtonItem { Mode = OperationMode.MassRTO,   Name = "Machine Home", BaseColor = "#01AE4C", ActiveColor = "#01AE4C" }
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
                if (mode == OperationMode.Manual)
                {
                    var btn = GetBtn(OperationMode.Manual);
                    if (isPressed && btn.IsEnabled)
                    {
                        _navService.NavigateMain<ManualOperationView>();
                    }
                    return;
                }

                var buttonItem = GetBtn(mode);
                if (buttonItem.IsEnabled && _writeTags.TryGetValue(mode, out int tagId))
                {
                    int value = isPressed ? 1 : 0;
                    await _coreClient.WriteTagAsync(tagId, value);

                    if (isPressed) AddAudit($"Operator Pressed: {mode}");
                }
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
                var liveData = await _coreClient.GetIoValuesAsync(5);
                if (liveData.Count < 1) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    bool isAutoRunning = false;
                    bool isDryRunning = false;
                    bool isStopRunning = false;
                    bool isRTORunning = false;

                    foreach (var btn in ModeButtons)
                    {
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

                        bool writeTagStatus = false;

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
                                btn.IsEnabled = writeTagStatus || Convert.ToBoolean(val);
                            }
                        }
                    }

                    var manualBtn = GetBtn(OperationMode.Manual);
                    bool isSystemBusy = isAutoRunning || isDryRunning || isStopRunning || isRTORunning;
                    manualBtn.IsEnabled = !isSystemBusy;

                    // Notify UI that Recipe Selection Enabled State May Have Changed
                    OnPropertyChanged(nameof(IsRecipeSelectionEnabled));

                    _enableTags.TryGetValue(OperationMode.MassRTO, out int homeLampId);
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

    // Added by Rishabh -Date -06-05-2026 ,  Recipe Item Model
    public class RecipeItem
    {
        public int ProgramNo { get; set; }
        public string Name { get; set; }
    }
}

  