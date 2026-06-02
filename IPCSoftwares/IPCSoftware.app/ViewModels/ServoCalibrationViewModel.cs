using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService;
using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;        //Added after
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;            //Added after
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.ViewModels
{
    public class ServoCalibrationViewModel : BaseViewModel, IDisposable, INavigationalAware
    {
        private readonly IRecipeManagementService _recipeManagementService;
        //private readonly IAeLimitService _aeLimitService;
        private readonly CoreClient _coreClient;
       // private readonly DispatcherTimer _liveDataTimer;
        private readonly SafePoller _liveDataTimer;
        private readonly IServoCalibrationService _servoService; // Injected Service
        private readonly IDialogService _dialog; // Injected Service
        private readonly IRecipeApplicationService _recipeService;
        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;       //added after

        private bool _initialPlcLoadDone = false;
        //private ProductSettingsModel _productSettings;
        private AeLimitSettings _aeLimitSettings;             //Added after
        private readonly string _appSettingsPath; // For saving units

        int _lastProgramAdded;

        // ===================================================================
        // TAB 3: AE LIMIT PROPERTIES (from AeLimitViewModel)
        // ===================================================================
        public AeLimitParameterItem AeMinX { get; private set; }
        public AeLimitParameterItem AeMaxX { get; private set; }
        public AeLimitParameterItem AeMinY { get; private set; }
        public AeLimitParameterItem AeMaxY { get; private set; }
        public AeLimitParameterItem AeMinZ { get; private set; }
        public AeLimitParameterItem AeMaxZ { get; private set; }

        private string _aeUnitX;
        public string AeUnitX { get => _aeUnitX; set => SetProperty(ref _aeUnitX, value); }

        private string _aeUnitY;
        public string AeUnitY { get => _aeUnitY; set => SetProperty(ref _aeUnitY, value); }

        private string _aeUnitAngle;
        public string AeUnitAngle { get => _aeUnitAngle; set => SetProperty(ref _aeUnitAngle, value); }

        public ICommand AeLimitRefreshCommand { get; }
        public ICommand AeLimitSaveCommand { get; }


        // ===================================================================
        // TAB 4: PRODUCT SETTINGS PROPERTIES (from ProductSettingsViewModel)
        // ===================================================================
        private string _productName;
        public string ProductName { get => _productName; set => SetProperty(ref _productName, value); }

        private string _productCode;
        public string ProductCode { get => _productCode; set => SetProperty(ref _productCode, value); }

        private int _selectedItemCount;
        public int SelectedItemCount { get => _selectedItemCount; set => SetProperty(ref _selectedItemCount, value); }

        

        private int _gridRows;
        public int GridRows { get => _gridRows; set => SetProperty(ref _gridRows, value); } 

        private int _gridColumns;
        public int GridColumns { get => _gridColumns; set => SetProperty(ref _gridColumns, value); }

        public ObservableCollection<int> ItemCounts { get; } = new ObservableCollection<int>(Enumerable.Range(1, 12));
        public ObservableCollection<int> ColumnOptions { get; } = new ObservableCollection<int>(Enumerable.Range(1, 3));
        public ObservableCollection<int> RowOptions { get; } = new ObservableCollection<int>(Enumerable.Range(1, 4));

        //public ICommand ProductSaveCommand { get; }



        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                SetProperty(ref _hasUnsavedChanges, value);
                // Optional: RaiseCanExecuteChanged on your Save commands here
            }
        }


        // Start of Coordinate Registers (13 Positions: 0 to 12)
        private  int START_TAG_POS_X = ConstantValues.Servo_Pos_Start.X;
        private  int START_TAG_POS_Y = ConstantValues.Servo_Pos_Start.Y;

        // Visual Feedback Properties for Jog Buttons
        // These are set True ONLY when B-Tag is received from PLC
        private bool _isJogXMinusActive; public bool IsJogXMinusActive { get => _isJogXMinusActive; set => SetProperty(ref _isJogXMinusActive, value); }
        private bool _isJogXPlusActive; public bool IsJogXPlusActive { get => _isJogXPlusActive; set => SetProperty(ref _isJogXPlusActive, value); }
        private bool _isJogYMinusActive; public bool IsJogYMinusActive { get => _isJogYMinusActive; set => SetProperty(ref _isJogYMinusActive, value); }
        private bool _isJogYPlusActive; public bool IsJogYPlusActive { get => _isJogYPlusActive; set => SetProperty(ref _isJogYPlusActive, value); }
        //

        int _nextProgramId;

        
        // Available Program Code for ComboBox
        private ObservableCollection<string> _availableProgramCode = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableProgramCode
        {
            get =>  _availableProgramCode;
            set => SetProperty(ref _availableProgramCode, value);
        }
        // Entered Fresh Program Code for ComboBox

        private string _freshProgramCode;
        public string FreshProductCode
        {
            get => _freshProgramCode;
            set => SetProperty(ref _freshProgramCode, value);
        }

        // Selected Program Code (for adding new programs)
        private string _selectedProgramCode;
        public string SelectedProgramCode
        {
            get => _selectedProgramCode;
            set => SetProperty(ref _selectedProgramCode, value);
        }

        // Entered Fresh Product Name 
        private string _freshProductName;
        public  string FreshProductName
        {
            get => _freshProductName;
            set => SetProperty(ref _freshProductName, value);
        }

        // Select Program Code (for Adding new product)
        private string _selectedProgramName;
        public string SelectedProductName
        {   get => _selectedProgramName;
            set => SetProperty(ref _selectedProgramName, value);
        }

        // Available Program Name

        private ObservableCollection<string> _availableProgramName = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableProductName
        {
            get => _availableProgramName;
            set => SetProperty(ref _availableProgramName, value);
        }

        // Current Running Program (reads from PLC Tag 544)
        private string _currentRunningProgram;
        public string CurrentRunningProgram
        {
            get => _currentRunningProgram;
            set => SetProperty(ref _currentRunningProgram, value);
        }

        private Visibility _isCancelButtonVisible = Visibility.Collapsed; // Default visible
        public Visibility IsCancelButtonVisible
        {
            get => _isCancelButtonVisible;
            set => SetProperty(ref _isCancelButtonVisible, value);
        }


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

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex == value) return;

                // Same check as Navigation
                if (HasUnsavedChanges)
                {
                    _dialog.ShowWarning("⚠️ UNSAVED CHANGES You must SAVE your changes before switching tabs.");


                    // Force Tab back to original
                    OnPropertyChanged(nameof(SelectedTabIndex));
                    return;
                }
                SetProperty(ref _selectedTabIndex, value);
            }
        }

        public ObservableCollection<ServoPositionModel> Positions { get; } = new();

        // Split Parameters into X and Y Lists
        public ObservableCollection<ServoParameterItem> XParameters { get; } = new();
        public ObservableCollection<ServoParameterItem> YParameters { get; } = new();

       // public List<int> AvailableSequences { get; } = Enumerable.Range(1, 12).ToList();
        public ObservableCollection<int> AvailableSequences { get; } = new ObservableCollection<int>();

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

        public ICommand AddProgramCommand { get; }
        public ICommand EditProgramCommand { get; }
        public ICommand CancelProgramCommand { get; }
        public ICommand DeleteProgramCommand { get; }
        public ICommand SaveProgramCommand { get; }

        public ICommand SelectionChangedCommand { get; }
        public ServoCalibrationViewModel(CoreClient coreClient,
            IServoCalibrationService servoService,
            IDialogService dialog,
             IRecipeApplicationService recipeService,
             IRecipeManagementService recipeManagementService,
             //IAeLimitService aeLimitService,
             IOptionsMonitor<ExternalSettings> settingMonitor,  //Added after
            // IPlcRecipeWriter plcRecipeWriter,   //Added after

            IAppLogger logger)
             : base(logger)
        {
            _dialog = dialog;
            _coreClient = coreClient;
            _servoService = servoService;
            _recipeService = recipeService;
            _recipeManagementService = recipeManagementService;
            //_aeLimitService = aeLimitService;
            _settingsMonitor = settingMonitor;
            _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            TeachCommand = new RelayCommand<ServoPositionModel>(OnTeachPosition);
            WritePositionCommand = new RelayCommand<ServoPositionModel>(OnWritePositionManual);
            WriteParamCommand = new RelayCommand<ServoParameterItem>(OnWriteParameter);

            ConfirmXParamsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_ParamSave, "X Servo Params"));
            ConfirmYParamsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_ParamA2, "Y Servo Params"));
            ConfirmXCoordsCommand = new RelayCommand(async () =>       WriteSelectedRecipeAsync());                                           //async () => await PulseBit(ConstantValues.Servo_CoordSave, "X Coordinates"));
            ConfirmYCoordsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_XYOrigin, "Y Coordinates"));

            JogCommand = new RelayCommand<object>(async (args) => await OnJogAsync(args));

            AddProgramCommand = new RelayCommand(OnNewProgram);

            EditProgramCommand = new RelayCommand(async () => await  OnSaveProgram());

            CancelProgramCommand = new RelayCommand(OnCancelProgram);

            DeleteProgramCommand = new RelayCommand(OnDeleteProgram);

            SaveProgramCommand = new RelayCommand(OnAddProgram);

            SelectionChangedCommand = new RelayCommand(async () => await OnProgramSelectionChangedAsync());

            //  AE Limit Commands
           // AeLimitRefreshCommand = new RelayCommand(async () =>  LoadAeLimitsFromRecipe());
           // AeLimitSaveCommand = new RelayCommand(OnSaveProgram);//async () => await SaveAeLimitsAsync());
            AeLimitSaveCommand = new RelayCommand(async () => WriteSelectedRecipeAsync());

            //  Product Settings Command
            //ProductSaveCommand = new RelayCommand(async () => await SaveProductSettingsAsync());
            IsCancelButtonVisible = Visibility.Collapsed;
            InitializeParameters();

            _ = Task.Run(async () =>
            {
                await InitializeAvailableProgramNumbers();
                await InitializePositionsAsync();                
                InitializeAeLimitParameters();
                await LoadAeLimitsAsync();
               // await LoadProductSettingsAsync();
            });

            //InitializePositions();
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(100),
                                    OnLiveDataTick  // Pass the method directly
                                  );
            _liveDataTimer.Start();

        }

        /// Initialize Servo Postion Models
        List<ServoRecipeModel> savedRecipes = new List<ServoRecipeModel>();

        private async Task InitializePositionsAsync()
        {
            try
            {
                // 1. Load Product Config
                //  var savedPositions = await _servoService.LoadPositionsAsync(); ///From ServoCalibration.Json

                // savedRecipes = await _servoService.LoadRecipeAsync();       // From Recipe.csv

                if (savedRecipes == null || !savedRecipes.Any())
                {
                    _logger.LogWarning("No recipes found. Cannot initialize positions.", LogType.Diagnostics);
                    return;
                }

                //==============///=======================//
                var lastSavedRecipe = savedRecipes.Last();
                int totalItems = lastSavedRecipe.TotalItems;

                //var prodConfig = await _productService.LoadAsync();
                //int totalItems = prodConfig.TotalItems;

                AvailableSequences.Clear();
                for (int i = 1; i <= totalItems; i++)
                {
                    AvailableSequences.Add(i);
                }

                var recipePositionData = new Dictionary<int, (double X, double Y, int Seq, string Name)>
                {
                    { 0, (lastSavedRecipe.X0, lastSavedRecipe.Y0, 0, "Position 0 (Home)") },
                    { 1, (lastSavedRecipe.X1, lastSavedRecipe.Y1, lastSavedRecipe.S1, "Position 1") },
                    { 2, (lastSavedRecipe.X2, lastSavedRecipe.Y2, lastSavedRecipe.S2, "Position 2") },
                    { 3, (lastSavedRecipe.X3, lastSavedRecipe.Y3, lastSavedRecipe.S3, "Position 3") },
                    { 4, (lastSavedRecipe.X4, lastSavedRecipe.Y4, lastSavedRecipe.S4, "Position 4") },
                    { 5, (lastSavedRecipe.X5, lastSavedRecipe.Y5, lastSavedRecipe.S5, "Position 5") },
                    { 6, (lastSavedRecipe.X6, lastSavedRecipe.Y6, lastSavedRecipe.S6, "Position 6") },
                    { 7, (lastSavedRecipe.X7, lastSavedRecipe.Y7, lastSavedRecipe.S7, "Position 7") },
                    { 8, (lastSavedRecipe.X8, lastSavedRecipe.Y8, lastSavedRecipe.S8, "Position 8") },
                    { 9, (lastSavedRecipe.X9, lastSavedRecipe.Y9, lastSavedRecipe.S9, "Position 9") },
                    { 10, (lastSavedRecipe.X10, lastSavedRecipe.Y10, lastSavedRecipe.S10, "Position 10") },
                    { 11, (lastSavedRecipe.X11, lastSavedRecipe.Y11, lastSavedRecipe.S11, "Position 11") },
                    { 12, (lastSavedRecipe.X12, lastSavedRecipe.Y12, lastSavedRecipe.S12, "Position 12") }
                };

                // 4. Populate List for UI
                Positions.Clear();
                // Add Position 0 (Home) - Always enabled
                if (recipePositionData.TryGetValue(0, out var homeData))
                {
                    Positions.Add(new ServoPositionModel
                    {
                        PositionId = 0,
                        Name = homeData.Name,
                        SequenceIndex = homeData.Seq,
                        X = homeData.X,
                        Y = homeData.Y,
                        IsEnabled = true
                    });
                }

                // Add only the number of items configured (1 to TotalItems)
                for (int i = 1; i <= totalItems; i++)
                {
                    if (recipePositionData.TryGetValue(i, out var posData))
                    {
                        Positions.Add(new ServoPositionModel
                        {
                            PositionId = i,
                            Name = posData.Name,
                            SequenceIndex = posData.Seq,
                            X = posData.X,
                            Y = posData.Y,
                            IsEnabled = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Init Positions Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        // ===================================================================
        // AE LIMIT LOGIC
        // ===================================================================
        private void InitializeAeLimitParameters()
        {
            AeMinX = new AeLimitParameterItem { Name = "Min X", ReadTagId = ConstantValues.MIN_X.Read, WriteTagId = ConstantValues.MIN_X.Write };
            AeMaxX = new AeLimitParameterItem { Name = "Max X", ReadTagId = ConstantValues.MAX_X.Read, WriteTagId = ConstantValues.MAX_X.Write };
            AeMinY = new AeLimitParameterItem { Name = "Min Y", ReadTagId = ConstantValues.MIN_Y.Read, WriteTagId = ConstantValues.MIN_Y.Write };
            AeMaxY = new AeLimitParameterItem { Name = "Max Y", ReadTagId = ConstantValues.MAX_Y.Read, WriteTagId = ConstantValues.MAX_Y.Write };
            AeMinZ = new AeLimitParameterItem { Name = "Min Angle", ReadTagId = ConstantValues.MIN_Z.Read, WriteTagId = ConstantValues.MIN_Z.Write };
            AeMaxZ = new AeLimitParameterItem { Name = "Max Angle", ReadTagId = ConstantValues.MAX_Z.Read, WriteTagId = ConstantValues.MAX_Z.Write };
        }

        private async Task LoadAeLimitsAsync()
        {
            try
            {
                // Load Units from Config
                var extConfig = _settingsMonitor.CurrentValue;
                AeUnitX = !string.IsNullOrEmpty(extConfig.InspectionXUnit) ? extConfig.InspectionXUnit : "mm";
                AeUnitY = !string.IsNullOrEmpty(extConfig.InspectionYUnit) ? extConfig.InspectionYUnit : "mm";
                AeUnitAngle = !string.IsNullOrEmpty(extConfig.InspectionAngleUnit) ? extConfig.InspectionAngleUnit : "deg";

                var lastRecipeSelected =  _recipeService.GetRecipefromSelection();
                var refStation = lastRecipeSelected.GetAwaiter().GetResult();
                
               
                    //var refStation = _aeLimitSettings.Stations[0];
                    AeMinX.NewValue = refStation.Xmin;
                    AeMaxX.NewValue = refStation.Xmax;
                    AeMinY.NewValue = refStation.Ymin;
                    AeMaxY.NewValue = refStation.Ymax;
                    AeMinZ.NewValue = refStation.AngleMin;
                    AeMaxZ.NewValue = refStation.AngleMin;
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE Limit] Load failed: {ex.Message}", LogType.Diagnostics);
            }
        }

      

        private async Task<bool> WaitForPlcConfirmationAsync()
        {
            int timeoutMs = 5000; // 5 Seconds Timeout
            int delayMs = 200;
            int elapsed = 0;

            while (elapsed < timeoutMs)
            {
                var data = await _coreClient.GetIoValuesAsync(5);
                if (data != null && data.TryGetValue(ConstantValues.ACK_LIMIT.Read, out object val))
                {
                    bool isComplete = false;
                    if (val is bool bVal) isComplete = bVal;
                    else if (val is int iVal) isComplete = (iVal > 0);

                    if (isComplete) return true;
                }
                await Task.Delay(delayMs);
                elapsed += delayMs;
            }
            return false;
        }



        // ---  Initialize Available Program Numbers  ---
        private async Task InitializeAvailableProgramNumbers()
        {
            await RefreshProgramNumbersAsync();
            SelectedItemCount = savedRecipes.Last().TotalItems;
            GridRows = savedRecipes.Last().GridRows;
            GridColumns = savedRecipes.Last().GridColumns;

        }

        // Add new helper method to refresh program numbers
        private async Task RefreshProgramNumbersAsync()
        {
            try
            {
                //Reload savedRecipes from file ( in case of modification by another)
                await LoadRecipesAsync();

                AvailableProgramCode.Clear();
                AvailableProductName.Clear();
                
                foreach (var recipe in savedRecipes.OrderBy(r => r.ProgramNo))
                {
                    AvailableProgramCode.Add(recipe.ProductCode);
                    AvailableProductName.Add(recipe.ProductName);
                }

                if (AvailableProgramCode.Any())
                {
                    //Availabe Product Code List Initialize

                    SelectedProgramCode = AvailableProgramCode.First();
                    FreshProductCode = AvailableProgramCode.First();
                    OnPropertyChanged(nameof(SelectedProgramCode));
                    OnPropertyChanged(nameof(SelectedProgramCode));
                    _nextProgramId = savedRecipes.Max(r => r.ProgramNo);

                    //Availabe Product Name List Initialize

                    SelectedProductName = AvailableProductName.First();
                    FreshProductName    = AvailableProductName.First();
                    OnPropertyChanged(nameof(SelectedProductName));
                    OnPropertyChanged(nameof(FreshProductName));


                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to refresh program numbers: {ex.Message}", LogType.Diagnostics);
            }
        }

        //Load All Recipes from Recipe.csv 
        private async Task LoadRecipesAsync() 
        {
            try
            {
                savedRecipes = await _servoService.LoadRecipeAsync();
                if (savedRecipes == null || !savedRecipes.Any())
                {
                    _logger.LogWarning("No Saved Recipes found", LogType.Error);
                    savedRecipes = new List<ServoRecipeModel>();
                }
                else
                {
                    _logger.LogInfo($"Loaded {savedRecipes.Count} recipes", LogType.Diagnostics);
                }
            }

            catch(Exception ex)
            {
                _logger.LogError($"Failed to load recipes: {ex.Message}", LogType.Error);
                savedRecipes = new List<ServoRecipeModel>(); // Fallback to empty list

            }
        }
        //-- New Program Command Handler (Prepares the form for new entry) ---
        private void OnNewProgram()
        {
            FreshProductCode = string.Empty;
            FreshProductName = string.Empty;
            SelectedProgramCode = null;
            SelectedProductName = null;
            SelectedItemCount = 1;
                GridRows = 1;
                GridColumns = 1;
            OnPropertyChanged(nameof(FreshProductCode));
            OnPropertyChanged(nameof(FreshProductName));
            OnPropertyChanged(nameof(SelectedProgramCode));
            OnPropertyChanged(nameof(SelectedProductName));
            HasUnsavedChanges = true;
            IsCancelButtonVisible = Visibility.Visible;

        }

        // ---  Add Program Command Handler  ---
        private async void OnAddProgram()
        {
            try
            {
                bool confirm = _dialog.ShowYesNo($"Do you want to Add Program {FreshProductCode}?", "Confirm Add Recipe");
                if (!confirm) return;

                string enteredProductCode = FreshProductCode ?? SelectedProgramCode;
                string enteredProductName = FreshProductName ?? SelectedProductName;
                int enteredTotalItem = SelectedItemCount;
                int enteredGridRow = GridRows;
                int enteredGridCol = GridColumns;

                if (!ValidateRecipeInput(enteredProductCode, enteredProductName, enteredGridRow, enteredGridCol, enteredTotalItem))
                    return;

                // ADD-specific: duplicate check
                if (savedRecipes.Any(r => string.Equals(r.ProductCode, enteredProductCode, StringComparison.OrdinalIgnoreCase)))
                {
                    _dialog.ShowWarning($"Recipe with Product Code '{enteredProductCode}' already exists");
                    return;
                }
                if (savedRecipes.Any(r => string.Equals(r.ProductName, enteredProductName, StringComparison.OrdinalIgnoreCase)))
                {
                    _dialog.ShowWarning($"Recipe with Product Name '{enteredProductName}' already exists");
                    return;
                }

                // ADD-specific: assign new program number
                var newRecipe = await BuildRecipeAsync(++_nextProgramId, enteredProductCode, enteredProductName, enteredTotalItem, enteredGridRow, enteredGridCol);


                _lastProgramAdded = newRecipe.ProgramNo;
                bool success = await _recipeManagementService.AddRecipeAsync(newRecipe);

                if (success)
                {
                    _logger.LogInfo($"Recipe Added: Program {_lastProgramAdded} - {enteredProductCode}", LogType.Audit);
                    _dialog.ShowMessage($"Program {_lastProgramAdded} with Product Code '{enteredProductCode}' added successfully.");
                    await RefreshProgramNumbersAsync();
                }
                else
                {
                    _dialog.ShowWarning($"Failed to add program {_lastProgramAdded}.");
                }

                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Add Program Error: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning("Failed to add program. Please check logs.");
            }
        }

        // -- - Cancel Program Command Handler (Resets the form to selected program) ---
        private void OnCancelProgram()
        {
            HasUnsavedChanges = false;
            SelectedProgramCode = AvailableProgramCode.Last();
            FreshProductCode = AvailableProgramCode.Last();
            OnPropertyChanged(nameof(SelectedProgramCode));
            OnPropertyChanged(nameof(SelectedProgramCode));
            _nextProgramId = savedRecipes.Max(r => r.ProgramNo);

            //Availabe Product Name List Initialize

            SelectedProductName = AvailableProductName.Last();
            FreshProductName = AvailableProductName.Last();
            OnPropertyChanged(nameof(SelectedProductName));
            OnPropertyChanged(nameof(FreshProductName));
        }

        // =------ Delete Program Command Handler  ------//

        private async void OnDeleteProgram()
        {
            try
            {
              
                //Check if at least 2 programs will remain after deletion
                if (AvailableProgramCode.Count <= 1) { _dialog.ShowWarning("Cannot delete. At least 1 programs must remain in the list."); return; }

                //var savedRecipe = await _servoService.LoadRecipeAsync();
                var selectedRecipe = savedRecipes.FirstOrDefault(r => r.ProductCode == SelectedProgramCode);
                if (selectedRecipe == null)
                {
                    _dialog.ShowWarning("Selected program not found.");
                    return;
                }

                //Check if the selected program is currently running
                if (selectedRecipe.ProductCode == CurrentRunningProgram) { _dialog.ShowWarning("Cannot remove running program"); return; }

                bool confirm = _dialog.ShowYesNo($" Do you want to remove Program {SelectedProgramCode}?", "Confirm Delete Recipe");
                

                if (!confirm) return;              
                             
                              
                await _recipeManagementService.DeleteRecipeAsync(selectedRecipe.ProgramNo);
                _logger.LogInfo($"Program {SelectedProgramCode} deleted successfully.", LogType.Audit);
                _dialog.ShowMessage($"Program {SelectedProgramCode} deleted successfully.");
                await RefreshProgramNumbersAsync();
                

            }

            catch(Exception ex)
            {
                _logger.LogError($"Failed to Delete the program: {ex}",LogType.Error);
            }
        }


        // =------ Edit/Save Program Command Handler  ------//
        private async Task OnSaveProgram()
        {
            try
            {
                string enteredProductCode = SelectedProgramCode ?? FreshProductCode;
                string enteredProductName = SelectedProductName ?? FreshProductName;
                int enteredTotalItem = SelectedItemCount;
                int enteredGridRow = GridRows;
                int enteredGridCol = GridColumns;

                if (!ValidateRecipeInput(enteredProductCode, enteredProductName, enteredGridRow, enteredGridCol, enteredTotalItem))
                    return;

                // SAVE-specific: must find existing recipe
                var selectedRecipe = savedRecipes.FirstOrDefault(r =>
                    r.ProductCode == (string.IsNullOrEmpty(SelectedProgramCode) ? FreshProductCode : SelectedProgramCode));

                if (selectedRecipe == null)
                {
                    _dialog.ShowWarning("Selected program not found.");
                    return;
                }

                bool confirm = _dialog.ShowYesNo($"Do you want to edit Program {SelectedProgramCode}?", "Confirm Update Recipe");
                if (!confirm) return;

                // SAVE-specific: keep original ProgramNo
                var updatedRecipe = await BuildRecipeAsync(selectedRecipe.ProgramNo, enteredProductCode, enteredProductName, enteredTotalItem, enteredGridRow, enteredGridCol);

                bool success = await _recipeManagementService.UpdateRecipeAsync(updatedRecipe);

                if (success)
                {
                    _logger.LogInfo($"Recipe updated successfully: Program {SelectedProgramCode}", LogType.Audit);
                    _dialog.ShowMessage($"Program {SelectedProgramCode} updated successfully.");
                    
                }
                else
                {
                    _dialog.ShowWarning($"Failed to edit program {SelectedProgramCode}");
                }

                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to edit the program: {ex}", LogType.Error);
            }
        }
        // Called from within ModeOfOperationViewModel (uses recipe.ProductCode)
        public async Task<Dictionary<int,bool>>  WriteSelectedRecipeAsync(ServoRecipeModel recipe)
        {
            var dict = new Dictionary<int, bool>();
            try
            {
               
               // var savedRecipe = await _servoService.LoadRecipeAsync();
                var selectedRecipe = savedRecipes.FirstOrDefault(r => r.ProductCode == recipe.ProductCode);

                bool seqconfirm = await PulseBitFromRecipe(recipe, ConstantValues.Servo_CoordSave, "X Coordinates");

                if (!seqconfirm)
                {
                    _logger.LogWarning($"PLC did not acknowledge coordinate save for Product Code {SelectedProgramCode}", LogType.Audit);                  
                    _dialog.ShowWarning("PLC did not acknowledge coordinate save. Please check connection.");
                    dict.Add(1, false);
                    
                }
                else { dict.Add(1, true); }


                if (_coreClient.isConnected)
                {
                    
                    bool confirmItemwrite = await _coreClient.WriteTagAsync(ConstantValues.NO_OF_Station, recipe.TotalItems);
              
                    
                    if (!confirmItemwrite) { _dialog.ShowWarning("Error Writing Total Item in Plc"); dict.Add(2, false); return dict; }
                    else { dict.Add(2, true); }
                    // Write Ae Limits to PLC
                    bool XminConf =  await _coreClient.WriteTagAsync(AeMinX.WriteTagId, recipe.Xmin);
                    bool XmaxConf = await _coreClient.WriteTagAsync(AeMaxX.WriteTagId, recipe.Xmax);
                    bool YminConf = await _coreClient.WriteTagAsync(AeMinY.WriteTagId, recipe.Ymin);
                    bool YmaxConf = await _coreClient.WriteTagAsync(AeMaxY.WriteTagId, recipe.Ymax);
                    bool ZminConf = await _coreClient.WriteTagAsync(AeMinZ.WriteTagId, recipe.AngleMin);
                    bool ZmaxConf = await _coreClient.WriteTagAsync(AeMaxZ.WriteTagId, recipe.AngleMax);

                    if( !(XminConf && XmaxConf && YminConf && YmaxConf && ZminConf && ZmaxConf)) 
                    {
                        _dialog.ShowWarning("Error Writing AE Limits in Plc");
                        dict.Add(3, false); 
                        
                    }
                    else { dict.Add(3, true); }
                        
                    // ---. Handshake Logic ---
                    // Set Transfer Start (DM10301.0) -> 1
                    _logger.LogInfo("[AE UI] Setting Transfer Start...", LogType.Audit);
                    await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 1);
                    // Wait for Confirmation (DM10480.0)
                    bool transferComplete = await WaitForPlcConfirmationAsync();

                    // Reset Start Bit -> 0
                    await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 0);

                    if (transferComplete)
                    {
                        _logger.LogInfo("[AE UI] PLC Confirmation Received.", LogType.Audit);
                        // _dialog.ShowMessage("Limits Saved & Transferred Successfully!");
                        dict.Add(4, true);
                    }
                    else
                    {
                        _logger.LogWarning("[AE UI] PLC Transfer Timeout.", LogType.Diagnostics);
                        _dialog.ShowWarning("Settings Saved, but PLC Confirmation timed out.\nPlease check PLC status.");
                        dict.Add(4, false);
                        
                    }

                    
                    
                }
              
                return dict;
 
            }
            catch(Exception ex)
            {
                _logger.LogError($"Failed to load Product Code {SelectedProgramCode} : {ex}", LogType.Error);
                dict = new Dictionary<int, bool>();
                return dict;
            }
        }

        // Called from within ServoCalibrationViewModel (uses SelectedProgramCode)
        public async void WriteSelectedRecipeAsync()
        {
            try
            {
                await  OnSaveProgram();
                // var savedRecipe = await _servoService.LoadRecipeAsync();
                var savedRecipe = await _servoService.LoadRecipeAsync();
                var selectedRecipe = savedRecipe.FirstOrDefault(r => r.ProductCode == SelectedProgramCode);

                if (selectedRecipe == null)
                {
                    _dialog.ShowWarning("Selected recipe not found.");
                    return;
                }
                if (selectedRecipe.ProductCode == CurrentRunningProgram)
                {
                    //Save csv before write operation
                    
                    bool seqconfirm = await PulseBitFromRecipe(selectedRecipe, ConstantValues.Servo_CoordSave, "X Coordinates");
                    if (_coreClient.isConnected)
                    {
                        await _coreClient.WriteTagAsync(ConstantValues.NO_OF_Station, SelectedItemCount);

                        // Write Ae Limits to PLC
                        await _coreClient.WriteTagAsync(AeMinX.WriteTagId, selectedRecipe.Xmin);
                        await _coreClient.WriteTagAsync(AeMaxX.WriteTagId, selectedRecipe.Xmax);
                        await _coreClient.WriteTagAsync(AeMinY.WriteTagId, selectedRecipe.Ymin);
                        await _coreClient.WriteTagAsync(AeMaxY.WriteTagId, selectedRecipe.Ymax);
                        await _coreClient.WriteTagAsync(AeMinZ.WriteTagId, selectedRecipe.AngleMin);
                        await _coreClient.WriteTagAsync(AeMaxZ.WriteTagId, selectedRecipe.AngleMax);

                        // ---. Handshake Logic ---
                        // Set Transfer Start (DM10301.0) -> 1
                        _logger.LogInfo("[AE UI] Setting Transfer Start...", LogType.Audit);
                        await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 1);
                        // Wait for Confirmation (DM10480.0)
                        bool transferComplete = await WaitForPlcConfirmationAsync();

                        // Reset Start Bit -> 0
                        await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 0);

                        if (transferComplete)
                        {
                            _logger.LogInfo("[AE UI] PLC Confirmation Received.", LogType.Audit);
                           // _dialog.ShowMessage("Limits Saved & Transferred Successfully!");
                        }
                        else
                        {
                            _logger.LogWarning("[AE UI] PLC Transfer Timeout.", LogType.Diagnostics);
                            _dialog.ShowWarning("Settings Saved, but PLC Confirmation timed out.\nPlease check PLC status.");
                        }

 
                    }
                }

                await RefreshProgramNumbersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load Product Code {SelectedProgramCode} : {ex}", LogType.Error);
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

        private async Task OnLiveDataTick()
        {
         
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

                    // --- Read Current Running Program from PLC (Tag 544) ---
                    if (data.TryGetValue(ConstantValues.ProgramNumber, out object programVal))
                    {
                        int currentProgramNo = Convert.ToInt32(programVal);
                       // var savedRecipe = await _servoService.LoadRecipeAsync();
                        var selectedRecipe = savedRecipes.FirstOrDefault(r => r.ProgramNo == currentProgramNo);
                       if (selectedRecipe != null)
                        {
                            CurrentRunningProgram = selectedRecipe.ProductCode;
                        }
                        else
                        {
                            CurrentRunningProgram = "*";
                        }
                    }
                    IsCancelButtonVisible = HasUnsavedChanges ? Visibility.Visible : Visibility.Collapsed;
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
          
        }
        
        private async void OnTeachPosition(ServoPositionModel position)
        {
            if (position == null) return;

            try
            {
                bool confirm = _dialog.ShowYesNo("Are you sure you want to update?", "Confirmation");

                if (!confirm)
                {
                    return;
                }

                    int xTag = START_TAG_POS_X + position.PositionId;
                int yTag = START_TAG_POS_Y + position.PositionId;

                _logger.LogInfo($"Teaching Pos {position.PositionId}: X={LiveX}, Y={LiveY}", LogType.Audit);

                // 1. Capture individual results
                bool successX = await _coreClient.WriteTagAsync(xTag, LiveX);
                bool successY = await _coreClient.WriteTagAsync(yTag, LiveY);

                // 2. Check if BOTH succeeded
                if (successX && successY)
                {
                    HasUnsavedChanges = true;
                    _dialog.ShowMessage("Values updated successfully.");
                }
                else
                {
                    // Handle partial or total failure
                    if (!successX && !successY)
                    {
                       
                        _dialog.ShowWarning("Failed to update X and Y. Please check logs.");
                    }
                    else
                    {
                        HasUnsavedChanges = true;
                        _dialog.ShowWarning($"Partial update: X={(successX ? "OK" : "Fail")}, Y={(successY ? "OK" : "Fail")}. Please check logs");
                    }
                }

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
                bool confirm = _dialog.ShowYesNo("Are you sure you want to update?", "Confirmation");

                if (!confirm)
                {
                    return;
                }
                // The 'position' object already has the new values because 
                // the TextBox binding (UpdateSourceTrigger=LostFocus) updated it.

                int xTag = START_TAG_POS_X + position.PositionId;
                int yTag = START_TAG_POS_Y + position.PositionId;

                _logger.LogInfo($"Manually Writing Pos {position.PositionId}: X={position.X:F2}, Y={position.Y:F2}", LogType.Audit);

             

                // 1. Capture individual results
                bool successX = await _coreClient.WriteTagAsync(xTag, position.X);
                bool successY = await _coreClient.WriteTagAsync(yTag, position.Y);

                // 2. Check if BOTH succeeded
                if (successX && successY)
                {
                    HasUnsavedChanges = true;
                    _dialog.ShowMessage("Values updated successfully.");
                }
                else
                {
                    // Handle partial or total failure
                    if (!successX && !successY)
                    {
                        _dialog.ShowWarning("Failed to update X and Y. Please check logs.");
                    }
                    else
                    {
                        HasUnsavedChanges = true;
                        _dialog.ShowWarning($"Partial update: X={(successX ? "OK" : "Fail")}, Y={(successY ? "OK" : "Fail")}. Please check logs");
                    }
                }

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
                bool confirm = _dialog.ShowYesNo("Are you sure you want to update?", "Confirm");

                if (!confirm)
                {
                    return;
                }
                _logger.LogInfo($"Writing {param.Name} -> {param.NewValue}", LogType.Audit);
                if (await _coreClient.WriteTagAsync(param.WriteTagId, param.NewValue))
                {
                    HasUnsavedChanges = true;
                    _dialog.ShowMessage("Value updated sucessfully.");
                }
                else
                {
                    _dialog.ShowWarning("Failed to update value. Please check logs.");
                }


            }
            catch (Exception ex) { _logger.LogError($"Write Param Error: {ex.Message}", LogType.Diagnostics); }
        }

        public bool OnNavigatingFrom()
        {
            if (HasUnsavedChanges)
            {
                // STRICT MESSAGE: No option to discard. User must go back and Save.
       /*         _dialog.ShowWarning(
                    "⚠️ UNSAVED CHANGES DETECTED\n\n" +
                    "You have written new values to the machine.\n" +
                    "You CANNOT leave this page until you press the 'SAVE' button to confirm them.\n\n" +
                    "Please Save your changes.");
*/
                _dialog.ShowWarning( "⚠️ UNSAVED CHANGES. You must SAVE your changes before leaving this page.");


                return false; // BLOCK NAVIGATION
            }
            return true; // Allow
        }
        public async Task PulseBit(int tagId, string description)
        {
            try
            {
                if (description.Contains("Coordinates"))
                {
                    // --- VALIDATION LOGIC START ---

                    var userSequences = Positions
                        .Where(p => p.PositionId != 0 )
                        .Select(p => p.SequenceIndex)
                        .ToList();

                    // 1. Check for Duplicates
                    if (userSequences.Distinct().Count() != userSequences.Count)
                    {
                        _dialog.ShowWarning("Validation Failed: Duplicate sequence numbers detected. Each position must have a unique number from 1 to 12.");
                        return;
                    }

                    // 2. Check Range (Just in case)
                    int maxSeq = Positions.Count - 1;
                    if (userSequences.Any(s => s < 1 || s > maxSeq))
                    {
                        _dialog.ShowWarning("Validation Failed: Sequence numbers must be between 1 and 12.");
                        return;
                    }

                    _logger.LogInfo("[Servo] Writing Sequence Map to PLC...", LogType.Audit);
                    foreach (var pos in Positions.Where(p => p.PositionId != 0))
                    {
                        // Tag Address Calculation: Base + (PositionIndex - 1)
                        // This assumes PLC sequence tags are sequential: 540, 541, 542...
                        // Ensure PLC memory supports 'maxSeq' registers.
                        int targetTagId = ConstantValues.Servo_Seq_Start + (pos.PositionId - 1);

                        await _coreClient.WriteTagAsync(targetTagId, pos.SequenceIndex);
                    }

                   /* for (int i = 1; i <= 12; i++)
                    {
                        // 1. Find which PositionId is assigned to Sequence 'i'
                        var positionAtThisStep = Positions.FirstOrDefault(p => p.PositionId == i);

                        // 2. Get the Value (Position ID) - Default to 0 if not found
                        int seqIdToWrite = positionAtThisStep != null ? positionAtThisStep.SequenceIndex : 0;

                        // 3. Calculate Tag ID (Start + Offset)
                        // Seq 1 writes to BaseTag + 0
                        // Seq 2 writes to BaseTag + 1 ...
                        int targetTagId = ConstantValues.Servo_Seq_Start + (i - 1);

                        // 4. Write to PLC
                        await _coreClient.WriteTagAsync(targetTagId, seqIdToWrite);
                    }*/
                    _logger.LogInfo("[Servo] Sequence Map Written Successfully.", LogType.Audit);

                }

                _logger.LogInfo($"Confirming {description}...", LogType.Audit);

                // Pulse 1 -> 0
              //  await _coreClient.WriteTagAsync(tagId, 1);

                if (await _coreClient.WriteTagAsync(tagId, 1))
                {
                    _dialog.ShowMessage("Value Updated sucessfully.");
                }
                else
                {
                    _dialog.ShowWarning("Failed to update value. Please check logs.");
                }
                await Task.Delay(200);


                await _coreClient.WriteTagAsync(tagId, 0);

              //  await _servoService.SavePositionsAsync(Positions.ToList());
                HasUnsavedChanges = false;

                _logger.LogInfo($"{description} Confirmed.", LogType.Audit);
            }
            catch (Exception ex) { _logger.LogError($"Confirm Error ({description}): {ex.Message}", LogType.Diagnostics); }
        }

        public async Task<bool> PulseBitFromRecipe(ServoRecipeModel recipe, int tagId, string description)
        {
            if (recipe == null)
            {
                _dialog.ShowWarning("No recipe selected. Cannot write to PLC.");
                return false;
            }

            try
            {
                if (description.Contains("Coordinates"))
                {
                    // Extract sequence indexes from recipe (S1-S12)
                    var sequenceMap = new Dictionary<int, int>
                        {
                            { 1, recipe.S1 },
                            { 2, recipe.S2 },
                            { 3, recipe.S3 },
                            { 4, recipe.S4 },
                            { 5, recipe.S5 },
                            { 6, recipe.S6 },
                            { 7, recipe.S7 },
                            { 8, recipe.S8 },
                            { 9, recipe.S9 },
                            { 10, recipe.S10 },
                            { 11, recipe.S11 },
                            { 12, recipe.S12 }
                        };

                    // Filter active sequences (only up to TotalItems)
                    int activeItemCount = recipe.TotalItems;
                    var activeSequences = sequenceMap
                        .Where(kvp => kvp.Key <= activeItemCount)
                        .Select(kvp => kvp.Value)
                        .ToList();

                    //  Validate sequence numbers
                    // Check for duplicates
                    if (activeSequences.Distinct().Count() != activeSequences.Count)
                    {
                        _dialog.ShowWarning("Validation Failed: Duplicate sequence numbers detected in recipe.");
                        return false;
                    }

                    // Check range (1 to TotalItems)
                    if (activeSequences.Any(s => s < 1 || s > activeItemCount))
                    {
                        _dialog.ShowWarning($"Validation Failed: Sequence numbers must be between 1 and {activeItemCount}.");
                        return false;
                    }

                    //  Write Sequence Indexes to PLC 
                    _logger.LogInfo($"[Recipe] Writing Sequence Map from recipe '{recipe.ProductCode}' to PLC...", LogType.Audit);

                    for (int positionId = 1; positionId <= activeItemCount; positionId++)
                    {
                        int sequenceIndex = sequenceMap[positionId];
                        int targetTagId = ConstantValues.Servo_Seq_Start + (positionId - 1);

                        await _coreClient.WriteTagAsync(targetTagId, sequenceIndex);
                        _logger.LogInfo($"[Recipe] Position {positionId} -> Sequence {sequenceIndex} (Tag {targetTagId})", LogType.Diagnostics);
                    }

                    //  Write X & Y Coordinates to PLC
                    _logger.LogInfo($"[Recipe] Writing Coordinates from recipe '{recipe.ProductCode}' to PLC...", LogType.Audit);

                    // X Coordinates (X0-X12)
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 0, recipe.X0);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 1, recipe.X1);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 2, recipe.X2);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 3, recipe.X3);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 4, recipe.X4);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 5, recipe.X5);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 6, recipe.X6);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 7, recipe.X7);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 8, recipe.X8);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 9, recipe.X9);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 10, recipe.X10);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 11, recipe.X11);
                    await _coreClient.WriteTagAsync(START_TAG_POS_X + 12, recipe.X12);

                    // Y Coordinates (Y0-Y12)
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 0, recipe.Y0);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 1, recipe.Y1);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 2, recipe.Y2);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 3, recipe.Y3);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 4, recipe.Y4);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 5, recipe.Y5);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 6, recipe.Y6);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 7, recipe.Y7);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 8, recipe.Y8);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 9, recipe.Y9);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 10, recipe.Y10);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 11, recipe.Y11);
                    await _coreClient.WriteTagAsync(START_TAG_POS_Y + 12, recipe.Y12);

                    _logger.LogInfo("[Recipe] Coordinates Written Successfully.", LogType.Audit);
                }

                //  Pulse Confirmation Bit (e.g., Servo_CoordSave)
                _logger.LogInfo($"Confirming {description} for recipe '{recipe.ProductCode}'...", LogType.Audit);

                if (await _coreClient.WriteTagAsync(tagId, 1))
                {
                    //_dialog.ShowMessage($"Recipe '{recipe.ProductCode}' loaded successfully.");
                    _logger.LogInfo($"Recipe '{recipe.ProductCode}' loaded successfully.", LogType.Audit);

                }
                else
                {
                    _dialog.ShowWarning("Failed to pulse confirmation bit. Please check PLC connection.");
                    return false;
                }

                await Task.Delay(200); // Wait for PLC to process
                await _coreClient.WriteTagAsync(tagId, 0); // Reset pulse bit

                _logger.LogInfo($"{description} for recipe '{recipe.ProductCode}' Confirmed.", LogType.Audit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"PulseBitFromRecipe Error ({description}): {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning($"Failed to load recipe '{recipe?.ProductCode}'. Check logs for details.");
                 return false;
            }
        }
                
        private async Task OnProgramSelectionChangedAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedProgramCode))
                {
                    _logger.LogWarning("No program code selected", LogType.Audit);
                    return;
                }

                // Load all saved recipes
               // var savedRecipes = await _servoService.LoadRecipeAsync();
                var selectedRecipe = savedRecipes.FirstOrDefault(r => r.ProductCode == SelectedProgramCode);

                if (selectedRecipe == null)
                {
                    _dialog.ShowWarning($"Recipe with Product Code '{SelectedProgramCode}' not found.");
                    return;
                }

                _logger.LogInfo($"Loading recipe: {SelectedProgramCode}", LogType.Audit);

                // ===================================================================
                // TAB 2: LOAD SERVO COORDINATES & SEQUENCES
                // ===================================================================
                await LoadServoCoordinatesFromRecipe(selectedRecipe);

                // ===================================================================
                // TAB 3: LOAD AE LIMITS
                // ===================================================================
                LoadAeLimitsFromRecipe(selectedRecipe);

                // ===================================================================
                // TAB 4: LOAD PRODUCT SETTINGS
                // ===================================================================
                LoadProductSettingsFromRecipe(selectedRecipe);

                _logger.LogInfo($"Recipe '{SelectedProgramCode}' loaded successfully into all tabs.", LogType.Audit);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load recipe details: {ex.Message}", LogType.Error);
                _dialog.ShowWarning("Failed to load recipe. Check logs for details.");
            }
        }

        // ===================================================================
        // TAB 2: LOAD SERVO COORDINATES FROM RECIPE
        // ===================================================================
        private async Task LoadServoCoordinatesFromRecipe(ServoRecipeModel recipe)
        {
            try
            {
                int totalItems = recipe.TotalItems;

                AvailableSequences.Clear();
                for(int i=1; i <= totalItems; i++)
                {
                    AvailableSequences.Add(i);
                }
                // Update Positions collection with recipe data
                var recipePositionData = new Dictionary<int, (double X, double Y, int Seq, string Name)>
                {
                    { 0, (recipe.X0, recipe.Y0, 0, "Position 0 (Home)") },
                    { 1, (recipe.X1, recipe.Y1, recipe.S1, "Position 1") },
                    { 2, (recipe.X2, recipe.Y2, recipe.S2, "Position 2") },
                    { 3, (recipe.X3, recipe.Y3, recipe.S3, "Position 3") },
                    { 4, (recipe.X4, recipe.Y4, recipe.S4, "Position 4") },
                    { 5, (recipe.X5, recipe.Y5, recipe.S5, "Position 5") },
                    { 6, (recipe.X6, recipe.Y6, recipe.S6, "Position 6") },
                    { 7, (recipe.X7, recipe.Y7, recipe.S7, "Position 7") },
                    { 8, (recipe.X8, recipe.Y8, recipe.S8, "Position 8") },
                    { 9, (recipe.X9, recipe.Y9, recipe.S9, "Position 9") },
                    { 10, (recipe.X10, recipe.Y10, recipe.S10, "Position 10") },
                    { 11, (recipe.X11, recipe.Y11, recipe.S11, "Position 11") },
                    { 12, (recipe.X12, recipe.Y12, recipe.S12, "Position 12") }
                };

                Positions.Clear();
                //Add Position 0 - always enabled
                if (recipePositionData.TryGetValue(0, out var homeData))
                {
                    Positions.Add(new ServoPositionModel
                    {
                        PositionId = 0,
                        Name = homeData.Name,
                        SequenceIndex = homeData.Seq,
                        X = homeData.X,
                        Y = homeData.Y,
                        IsEnabled = true
                    });

                }
               //Add positions1 to TotalItems (only active postions from recipe
               for (int i =1; i <= totalItems; i++)
                {
                    if(recipePositionData.TryGetValue(i, out var posData))
                    {
                        Positions.Add(new ServoPositionModel
                        {
                            PositionId = i,
                            Name = posData.Name,
                            SequenceIndex = posData.Seq,
                            X = posData.X,
                            Y = posData.Y,
                            IsEnabled = true
                        });

                                            }
                }
                _logger.LogInfo($"Loaded {Positions.Count} positions from recipe '{recipe.ProductCode}'", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load servo coordinates: {ex.Message}", LogType.Error);
            }
        }

        // ===================================================================
        // TAB 3: LOAD AE LIMITS FROM RECIPE
        // ===================================================================
        private void LoadAeLimitsFromRecipe(ServoRecipeModel recipe)
        {
            try
            {
                AeMinX.NewValue = recipe.Xmin;
                AeMaxX.NewValue = recipe.Xmax;
                AeMinY.NewValue = recipe.Ymin;
                AeMaxY.NewValue = recipe.Ymax;
                AeMinZ.NewValue = recipe.AngleMin;
                AeMaxZ.NewValue = recipe.AngleMax;

                _logger.LogInfo("AE Limits loaded from recipe", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load AE limits: {ex.Message}", LogType.Error);
            }
        }

        // ===================================================================
        // TAB 4: LOAD PRODUCT SETTINGS FROM RECIPE
        // ===================================================================
        private void LoadProductSettingsFromRecipe(ServoRecipeModel recipe)
        {
            try
            {
                ProductName = recipe.ProductName;
                ProductCode = recipe.ProductCode;
                SelectedItemCount = recipe.TotalItems;
                GridRows = recipe.GridRows > 0 ? recipe.GridRows : 4;
                GridColumns = recipe.GridColumns > 0 ? recipe.GridColumns : 3;

                // Update FreshProduct fields as well (for edit mode)
                FreshProductName = recipe.ProductName;
                FreshProductCode = recipe.ProductCode;

                _logger.LogInfo($"Product settings loaded: {ProductName} ({ProductCode})", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load product settings: {ex.Message}", LogType.Error);
            }
        }


        public void Dispose()
        {
            try
            {
                _liveDataTimer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        // SHARED HELPER: Validate recipe input fields
        // Returns true if valid, false + shows warning if not
        // ===================================================================
        private bool ValidateRecipeInput(string productCode, string productName, int gridRows, int gridCols, int totalItems)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                _dialog.ShowWarning("Product Code cannot be empty");
                return false;
            }
            if (string.IsNullOrWhiteSpace(productName))
            {
                _dialog.ShowWarning("Product Name cannot be empty");
                return false;
            }
            if (gridRows * gridCols < totalItems)
            {
                _dialog.ShowWarning($"Grid Layout ({gridRows}x{gridCols}) is too small for {totalItems} items.");
                return false;
            }
            return true;
        }

        // ===================================================================
        // SHARED HELPER: Build a ServoRecipeModel from current UI state
        // programNo is passed in by caller (new ID or existing ID)
        // ===================================================================
        private async Task<ServoRecipeModel> BuildRecipeAsync(int programNo, string productCode, string productName, int totalItems, int gridRows, int gridCols)
        {
            var savedPositions = Positions.ToList();
            // --- 1. Save Unit Settings to appsettings.json ---


            var json = File.ReadAllText(_appSettingsPath);
            var jsonObj = JObject.Parse(json);
            if (jsonObj["External"] == null) jsonObj["External"] = new JObject();

            var ext = jsonObj["External"];
            ext["InspectionXUnit"] = AeUnitX;
            ext["InspectionYUnit"] = AeUnitY;
            ext["InspectionAngleUnit"] = AeUnitAngle;

            File.WriteAllText(_appSettingsPath, jsonObj.ToString());
            _logger.LogInfo("[AE UI] Units saved to appsettings.json.", LogType.Audit);

            return new ServoRecipeModel
            {
                ProgramNo = programNo,

                // Sequence Indexes (S1-S12)
                S1 = savedPositions.FirstOrDefault(p => p.PositionId == 1)?.SequenceIndex ?? 0,
                S2 = savedPositions.FirstOrDefault(p => p.PositionId == 2)?.SequenceIndex ?? 0,
                S3 = savedPositions.FirstOrDefault(p => p.PositionId == 3)?.SequenceIndex ?? 0,
                S4 = savedPositions.FirstOrDefault(p => p.PositionId == 4)?.SequenceIndex ?? 0,
                S5 = savedPositions.FirstOrDefault(p => p.PositionId == 5)?.SequenceIndex ?? 0,
                S6 = savedPositions.FirstOrDefault(p => p.PositionId == 6)?.SequenceIndex ?? 0,
                S7 = savedPositions.FirstOrDefault(p => p.PositionId == 7)?.SequenceIndex ?? 0,
                S8 = savedPositions.FirstOrDefault(p => p.PositionId == 8)?.SequenceIndex ?? 0,
                S9 = savedPositions.FirstOrDefault(p => p.PositionId == 9)?.SequenceIndex ?? 0,
                S10 = savedPositions.FirstOrDefault(p => p.PositionId == 10)?.SequenceIndex ?? 0,
                S11 = savedPositions.FirstOrDefault(p => p.PositionId == 11)?.SequenceIndex ?? 0,
                S12 = savedPositions.FirstOrDefault(p => p.PositionId == 12)?.SequenceIndex ?? 0,

                // X Coordinates (X0-X12)
                X0 = savedPositions.FirstOrDefault(p => p.PositionId == 0)?.X ?? 0,
                X1 = savedPositions.FirstOrDefault(p => p.PositionId == 1)?.X ?? 0,
                X2 = savedPositions.FirstOrDefault(p => p.PositionId == 2)?.X ?? 0,
                X3 = savedPositions.FirstOrDefault(p => p.PositionId == 3)?.X ?? 0,
                X4 = savedPositions.FirstOrDefault(p => p.PositionId == 4)?.X ?? 0,
                X5 = savedPositions.FirstOrDefault(p => p.PositionId == 5)?.X ?? 0,
                X6 = savedPositions.FirstOrDefault(p => p.PositionId == 6)?.X ?? 0,
                X7 = savedPositions.FirstOrDefault(p => p.PositionId == 7)?.X ?? 0,
                X8 = savedPositions.FirstOrDefault(p => p.PositionId == 8)?.X ?? 0,
                X9 = savedPositions.FirstOrDefault(p => p.PositionId == 9)?.X ?? 0,
                X10 = savedPositions.FirstOrDefault(p => p.PositionId == 10)?.X ?? 0,
                X11 = savedPositions.FirstOrDefault(p => p.PositionId == 11)?.X ?? 0,
                X12 = savedPositions.FirstOrDefault(p => p.PositionId == 12)?.X ?? 0,

                // Y Coordinates (Y0-Y12)
                Y0 = savedPositions.FirstOrDefault(p => p.PositionId == 0)?.Y ?? 0,
                Y1 = savedPositions.FirstOrDefault(p => p.PositionId == 1)?.Y ?? 0,
                Y2 = savedPositions.FirstOrDefault(p => p.PositionId == 2)?.Y ?? 0,
                Y3 = savedPositions.FirstOrDefault(p => p.PositionId == 3)?.Y ?? 0,
                Y4 = savedPositions.FirstOrDefault(p => p.PositionId == 4)?.Y ?? 0,
                Y5 = savedPositions.FirstOrDefault(p => p.PositionId == 5)?.Y ?? 0,
                Y6 = savedPositions.FirstOrDefault(p => p.PositionId == 6)?.Y ?? 0,
                Y7 = savedPositions.FirstOrDefault(p => p.PositionId == 7)?.Y ?? 0,
                Y8 = savedPositions.FirstOrDefault(p => p.PositionId == 8)?.Y ?? 0,
                Y9 = savedPositions.FirstOrDefault(p => p.PositionId == 9)?.Y ?? 0,
                Y10 = savedPositions.FirstOrDefault(p => p.PositionId == 10)?.Y ?? 0,
                Y11 = savedPositions.FirstOrDefault(p => p.PositionId == 11)?.Y ?? 0,
                Y12 = savedPositions.FirstOrDefault(p => p.PositionId == 12)?.Y ?? 0,

            
                Xmin = AeMinX.NewValue,
                Xmax = AeMaxX.NewValue,
                Ymin = AeMinY.NewValue,
                Ymax = AeMaxY.NewValue,
                AngleMin = AeMinZ.NewValue,
                AngleMax = AeMaxZ.NewValue,

                // Product Setup
                ProductName = productName,
                ProductCode = productCode,
                TotalItems = totalItems,
                GridRows = gridRows,
                GridColumns = gridCols
            };

                           

        }



    }


    // ADDED: Helper class for AE Limit Parameters
    public class AeLimitParameterItem : ObservableObjectVM
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


    public enum JogDirection
    {
        XPlus,
        XMinus,
        YPlus,
        YMinus
    }
}