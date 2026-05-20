using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;        //Added after
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;            //Added after
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.ViewModels
{
    public class ServoCalibrationViewModel : BaseViewModel, IDisposable, INavigationalAware
    {
        private readonly IRecipeManagementService _recipeManagementService;
        private readonly IAeLimitService _aeLimitService;
        private readonly CoreClient _coreClient;
       // private readonly DispatcherTimer _liveDataTimer;
        private readonly SafePoller _liveDataTimer;
        private readonly IServoCalibrationService _servoService; // Injected Service
        private readonly IDialogService _dialog; // Injected Service
        private readonly IProductConfigurationService _productService;
        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;       //added after

        private bool _initialPlcLoadDone = false;
        private ProductSettingsModel _productSettings;
        private AeLimitSettings _aeLimitSettings;             //Added after

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
                    _dialog.ShowWarning(
                        "⚠️ UNSAVED CHANGES\n\n" +
                        "You must SAVE your changes before switching tabs.");

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

        public ICommand DeleteProgramCommand { get; }

        public ICommand SaveProgramCommand { get; }
        public ServoCalibrationViewModel(CoreClient coreClient,
            IServoCalibrationService servoService,
            IDialogService dialog,
             IProductConfigurationService productService,
             IRecipeManagementService recipeManagementService,
             IAeLimitService aeLimitService,
             IOptionsMonitor<ExternalSettings> settingMonitor,  //Added after

            IAppLogger logger)
             : base(logger)
        {
            _dialog = dialog;
            _coreClient = coreClient;
            _servoService = servoService; 
            _productService = productService;
            _recipeManagementService = recipeManagementService;
            _aeLimitService = aeLimitService;
            _settingsMonitor = settingMonitor;     

            TeachCommand = new RelayCommand<ServoPositionModel>(OnTeachPosition);
            WritePositionCommand = new RelayCommand<ServoPositionModel>(OnWritePositionManual);
            WriteParamCommand = new RelayCommand<ServoParameterItem>(OnWriteParameter);

            ConfirmXParamsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_ParamSave, "X Servo Params"));
            ConfirmYParamsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_ParamA2, "Y Servo Params"));
            ConfirmXCoordsCommand = new RelayCommand(WriteSelectedRecipeAsync);                                           //async () => await PulseBit(ConstantValues.Servo_CoordSave, "X Coordinates"));
            ConfirmYCoordsCommand = new RelayCommand(async () => await PulseBit(ConstantValues.Servo_XYOrigin, "Y Coordinates"));

            JogCommand = new RelayCommand<object>(async (args) => await OnJogAsync(args));

            AddProgramCommand = new RelayCommand(OnNewProgram);

            EditProgramCommand = new RelayCommand(OnSaveProgram);

            DeleteProgramCommand = new RelayCommand(OnDeleteProgram);

            SaveProgramCommand = new RelayCommand(OnAddProgram);

            //  AE Limit Commands
            AeLimitRefreshCommand = new RelayCommand(async () => await LoadAeLimitsAsync());
            AeLimitSaveCommand = new RelayCommand(OnSaveProgram);//async () => await SaveAeLimitsAsync());

            //  Product Settings Command
            //ProductSaveCommand = new RelayCommand(async () => await SaveProductSettingsAsync());


            InitializeParameters();
            // Load positions from JSON via Service
            _ = InitializePositionsAsync();

            InitializeAvailableProgramNumbers();

            //Initialize AE Limits Parametrs
            InitializeAeLimitParameters();

            //Load AE Limits and Product Settings
            _ = LoadAeLimitsAsync();
            _ = LoadProductSettingsAsync();

            //InitializePositions();
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(100),
                                    OnLiveDataTick  // Pass the method directly
                                  );
            _liveDataTimer.Start();

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

                // Load Limits from JSON
                _aeLimitSettings = await _aeLimitService.GetSettingsAsync();

                if (_aeLimitSettings?.Stations != null && _aeLimitSettings.Stations.Count > 0)
                {
                    var refStation = _aeLimitSettings.Stations[0];
                    AeMinX.NewValue = refStation.InspectionX.Lower;
                    AeMaxX.NewValue = refStation.InspectionX.Upper;
                    AeMinY.NewValue = refStation.InspectionY.Lower;
                    AeMaxY.NewValue = refStation.InspectionY.Upper;
                    AeMinZ.NewValue = refStation.InspectionAngle.Lower;
                    AeMaxZ.NewValue = refStation.InspectionAngle.Upper;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE Limit] Load failed: {ex.Message}", LogType.Diagnostics);
            }
        }

        public async Task SaveAeLimitsAsync()
        {
            try
            {
                // Save to JSON
                _aeLimitSettings = await _aeLimitService.GetSettingsAsync();
                if (_aeLimitSettings?.Stations != null)
                {
                    foreach (var station in _aeLimitSettings.Stations)
                    {
                        station.InspectionX.Lower = AeMinX.NewValue;
                        station.InspectionX.Upper = AeMaxX.NewValue;
                        station.InspectionY.Lower = AeMinY.NewValue;
                        station.InspectionY.Upper = AeMaxY.NewValue;
                        station.InspectionAngle.Lower = AeMinZ.NewValue;
                        station.InspectionAngle.Upper = AeMaxZ.NewValue;
                    }
                }
                await _aeLimitService.SaveSettingsAsync(_aeLimitSettings);

                // Write to PLC
                await _coreClient.WriteTagAsync(AeMinX.WriteTagId, AeMinX.NewValue);
                await _coreClient.WriteTagAsync(AeMaxX.WriteTagId, AeMaxX.NewValue);
                await _coreClient.WriteTagAsync(AeMinY.WriteTagId, AeMinY.NewValue);
                await _coreClient.WriteTagAsync(AeMaxY.WriteTagId, AeMaxY.NewValue);
                await _coreClient.WriteTagAsync(AeMinZ.WriteTagId, AeMinZ.NewValue);
                await _coreClient.WriteTagAsync(AeMaxZ.WriteTagId, AeMaxZ.NewValue);

                // Handshake (optional, based on your PLC logic)
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 1);
                await Task.Delay(200);
                await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 0);

                _dialog.ShowMessage("AE Limits Saved Successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE Limit] Save failed: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning("Failed to save AE Limits.");
            }
        }

        // ===================================================================
        // PRODUCT SETTINGS LOGIC
        // ===================================================================
        private async Task LoadProductSettingsAsync()
        {
            try
            {
                var config = await _productService.LoadAsync();
                ProductName = config.ProductName;
                ProductCode = config.ProductCode;
                SelectedItemCount = config.TotalItems;
                GridRows = config.GridRows > 0 ? config.GridRows : 4;
                GridColumns = config.GridColumns > 0 ? config.GridColumns : 3;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Product Settings] Load failed: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task SaveProductSettingsAsync()
        {
            try
            {
                // Validation
                if (GridRows * GridColumns < SelectedItemCount)
                {
                    _dialog.ShowWarning($"Grid Layout ({GridRows}x{GridColumns}) is too small for {SelectedItemCount} items.");
                    return;
                }

                var config = new ProductSettingsModel
                {
                    ProductName = ProductName,
                    ProductCode = ProductCode,
                    TotalItems = SelectedItemCount,
                    GridRows = GridRows,
                    GridColumns = GridColumns
                };

                await _productService.SaveAsync(config);

                // Write to PLC
                if (_coreClient.isConnected)
                {
                    await _coreClient.WriteTagAsync(ConstantValues.NO_OF_Station, SelectedItemCount);
                }

                _dialog.ShowMessage("Product Settings Saved Successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Product Settings] Save failed: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning("Failed to save Product Settings.");
            }
        }





        // ---  Initialize Available Program Numbers  ---
        private async  void InitializeAvailableProgramNumbers()
        {
            await RefreshProgramNumbersAsync();

        }

        // Add new helper method to refresh program numbers
        private async Task RefreshProgramNumbersAsync()
        {
            try
            {
                var savedRecipes = await _servoService.LoadRecipeAsync();
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

                    SelectedProgramCode = AvailableProgramCode.Last();
                    FreshProductCode = AvailableProgramCode.Last();
                    OnPropertyChanged(nameof(SelectedProgramCode));
                    OnPropertyChanged(nameof(SelectedProgramCode));
                    _nextProgramId = savedRecipes.Max(r => r.ProgramNo);

                    //Availabe Product Name List Initialize

                    SelectedProductName = AvailableProductName.Last();
                    FreshProductName    = AvailableProductName.Last();
                    OnPropertyChanged(nameof(SelectedProductName));
                    OnPropertyChanged(nameof(FreshProductName));


                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to refresh program numbers: {ex.Message}", LogType.Diagnostics);
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

        }

        // ---  Add Program Command Handler  ---
        private async void OnAddProgram()
        {
            try
            {

                //var userInput = _dialog.UserInput(
                //    "Product Code:",
                //    "", // Default value for Field1
                //    "Product Name:",
                //    ""  // Default value for Field2
                //);

                //if (!userInput.Confirmed)
                //{
                //    _logger.LogInfo("Add Program cancelled by user.", LogType.Audit);
                //    return;
                //}

                string enteredProductCode = FreshProductCode??SelectedProgramCode;// Use Selected Program Code if Fresh Code value not entered
                string enteredProductName = FreshProductName??SelectedProductName; //Use Selected Program Name if Fresh Name value not entered
                int enteredTotalItem = SelectedItemCount;
                int enteredGridRow = GridRows;
                int enteredGridCol = GridColumns;

                // Validation
                if (enteredGridRow * enteredGridCol < enteredTotalItem)
                {
                    _dialog.ShowWarning($"Grid Layout ({enteredGridRow}x{enteredGridCol}) is too small for {enteredTotalItem} items.");
                    return;
                }


                if (string.IsNullOrWhiteSpace(enteredProductCode))
                {
                    _dialog.ShowWarning("Product Code cannot be empty");
                    return;
                }

                if (string.IsNullOrEmpty(enteredProductName))
                {
                    _dialog.ShowWarning("Product Name cannot be empty");
                    return;
                }



                var savedRecipes = await _servoService.LoadRecipeAsync();
                bool productCodeExists = savedRecipes.Any(r =>
                    string.Equals(r.ProductCode, enteredProductCode, StringComparison.OrdinalIgnoreCase));

                bool productNameExists = savedRecipes.Any(r =>
                    string.Equals(r.ProductName, enteredProductName, StringComparison.OrdinalIgnoreCase));

                if (productCodeExists)
                {
                    _dialog.ShowWarning($"Recipe with Product Name '{enteredProductCode}' already exists");
                    return;
                }

                if (productNameExists)
                {
                    _dialog.ShowWarning($"Recipe with Product Name '{enteredProductName}' already exists");
                    return;
                }


                var savedPositions = Positions.ToList();
                var aeLimitSettings = await _aeLimitService.GetSettingsAsync();
                var firstStation = aeLimitSettings?.Stations?.FirstOrDefault();
                var productSetup = await _productService.LoadAsync();

                var newRecipe = new ServoRecipeModel
                {
                    ProgramNo = ++_nextProgramId,

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

                    // AE Limits 
                    Xmin = firstStation?.InspectionX?.Lower ?? 0,
                    Xmax = firstStation?.InspectionX?.Upper ?? 0,
                    Ymin = firstStation?.InspectionY?.Lower ?? 0,
                    Ymax = firstStation?.InspectionY?.Upper ?? 0,
                    AngleMin = firstStation?.InspectionAngle?.Lower ?? 0,
                    AngleMax = firstStation?.InspectionAngle?.Upper ?? 0,

                    // Product Setup -
                    ProductName = enteredProductName,
                    ProductCode = enteredProductCode,
                    TotalItems = enteredTotalItem,
                    GridRows = enteredGridRow,
                    GridColumns = enteredGridCol
                };
                var config = new ProductSettingsModel
                {
                    ProductName = enteredProductName,
                    ProductCode = enteredProductCode,
                    TotalItems = enteredTotalItem,
                    GridRows = enteredGridRow,
                    GridColumns = enteredGridCol
                };

                await _productService.SaveAsync(config);
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"Add Program Error: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning("Failed to add program. Please check logs.");
            }
        }

        // =------ Delete Program Command Handler  ------//

        private async void OnDeleteProgram()
        {
            try
            {
              
                //Check if at least 2 programs will remain after deletion
                if (AvailableProgramCode.Count <= 1) { _dialog.ShowWarning("Cannot delete. At least 1 programs must remain in the list."); return; }

                var savedRecipe = await _servoService.LoadRecipeAsync();
                var selectedRecipe = savedRecipe.FirstOrDefault(r => r.ProductCode == SelectedProgramCode);
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

        // =------ Edit Program Command Handler  ------//
        private async void OnSaveProgram()
        {
            try
            {
                // Find the recipe by ProductCode
                var savedRecipes = await _servoService.LoadRecipeAsync();
                var selectedRecipe = savedRecipes.FirstOrDefault(r => r.ProductCode == SelectedProgramCode);

                if (selectedRecipe == null)
                {
                    _dialog.ShowWarning("Selected program not found.");
                    return;
                }
                bool confirm = _dialog.ShowYesNo($" Do you want to edit Program {SelectedProgramCode}?", "Confirm Update Recipe");
                if (!confirm) return;
                //Collecting data from current positions 
                var savedPositions = Positions.ToList();

                //Collecting data from AE Limits
                var aeLimitSettings = await _aeLimitService.GetSettingsAsync();
                var firstStation = aeLimitSettings?.Stations?.FirstOrDefault();
                var productSetup = await _productService.LoadAsync();

                var UpdatedRecipe = new ServoRecipeModel
                {
                    ProgramNo = selectedRecipe.ProgramNo,

                    //Sequence Indexes (S1-S12)

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

                    // X Coordinate (X0-X12)
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
                    // Y Coordinate (Y0-Y12)
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
                    // AE Limits 

                    Xmin = firstStation?.InspectionX?.Lower ?? 0,
                    Xmax = firstStation?.InspectionX?.Upper ?? 0,
                    Ymin = firstStation?.InspectionY?.Lower ?? 0,
                    Ymax = firstStation?.InspectionY?.Upper ?? 0,
                    AngleMin = firstStation?.InspectionAngle?.Lower ?? 0,
                    AngleMax = firstStation?.InspectionAngle?.Upper ?? 0,

                    //Product Setup 

                    ProductName = productSetup?.ProductName ?? "0",
                    ProductCode = productSetup?.ProductCode ?? "0",
                    TotalItems = productSetup?.TotalItems ?? 1,
                    GridRows = productSetup?.GridRows ?? 1,
                    GridColumns = productSetup?.GridColumns ?? 1

                };
                var config = new ProductSettingsModel
                {
                    ProductName = productSetup?.ProductName ?? "0",
                    ProductCode = productSetup?.ProductCode ?? "0",
                    TotalItems = productSetup?.TotalItems ?? 1,
                    GridRows = productSetup?.GridRows ?? 1,
                    GridColumns = productSetup?.GridColumns ?? 1
                };

                await _productService.SaveAsync(config);

                bool success = await _recipeManagementService.UpdateRecipeAsync(UpdatedRecipe);


                if (success)
                {
                    _logger.LogInfo($"Recipe updated successfully: Program {SelectedProgramCode}", LogType.Audit);
                    _dialog.ShowMessage($"Program {SelectedProgramCode} updated successfully.");

                    await RefreshProgramNumbersAsync();

                }
                else
                {
                    _dialog.ShowWarning($"Failed to edit program {SelectedProgramCode}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to edit the program :{ex}", LogType.Error);
            }
        }

        public async void WriteSelectedRecipeAsync()
        {
            try
            {
                var savedRecipe = await _servoService.LoadRecipeAsync();
                var selectedRecipe = savedRecipe.FirstOrDefault(r => r.ProductCode == SelectedProgramCode);
                if (selectedRecipe.ProductCode == CurrentRunningProgram)
                {
                    //OnAddProgram();
                    await PulseBit(ConstantValues.Servo_CoordSave, "X Coordinates");
                   // await SaveAeLimitsAsync();
                    if (_coreClient.isConnected)
                    {
                        await _coreClient.WriteTagAsync(ConstantValues.NO_OF_Station, SelectedItemCount);

                    }
                    // await SaveProductSettingsAsync();


                    // Write to PLC
                    await _coreClient.WriteTagAsync(AeMinX.WriteTagId, selectedRecipe.Xmin);
                    await _coreClient.WriteTagAsync(AeMaxX.WriteTagId, selectedRecipe.Xmax);
                    await _coreClient.WriteTagAsync(AeMinY.WriteTagId, selectedRecipe.Ymin);
                    await _coreClient.WriteTagAsync(AeMaxY.WriteTagId, selectedRecipe.Ymax);
                    await _coreClient.WriteTagAsync(AeMinZ.WriteTagId, selectedRecipe.AngleMin);
                    await _coreClient.WriteTagAsync(AeMaxZ.WriteTagId, selectedRecipe.AngleMax);

                    // Handshake (optional, based on your PLC logic)
                    await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 1);
                    await Task.Delay(200);
                    await _coreClient.WriteTagAsync(ConstantValues.ACK_LIMIT.Write, 0);





                    HasUnsavedChanges = false;

                }

                else
                {
                    OnSaveProgram();
                    HasUnsavedChanges = false;
                }
            }
            catch(Exception ex)
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
            

       
       /* private async Task InitializePositionsAsync()
        {
            try
            {
                // Use the service to load positions (which includes SequenceIndex and Coordinates)
                var positions = await _servoService.LoadPositionsAsync();

                Positions.Clear();

                foreach (var pos in positions.OrderBy(p => p.PositionId))
                {
                    Positions.Add(pos);
                }
                // Ensure ordered by ID for UI consistency
               
            }
            catch (Exception ex)
            {
                _logger.LogError($"Init Positions Error: {ex.Message}", LogType.Diagnostics);
            }
        }
*/
        private async Task InitializePositionsAsync()
        {
            try
            {
                // 1. Load Product Config
            
                var prodConfig = await _productService.LoadAsync();
                int totalItems = prodConfig.TotalItems;
                AvailableSequences.Clear();
                for (int i = 1; i <= totalItems; i++)
                {
                    AvailableSequences.Add(i);
                }

                var savedPositions = await _servoService.LoadPositionsAsync();
                var SavedRecipe = await _servoService.LoadRecipeAsync();

                // 4. Populate List for UI
                Positions.Clear();

                // Ensure Position 0 (Home) exists
                var homePos = savedPositions.FirstOrDefault(p => p.PositionId == 0) ?? new ServoPositionModel { PositionId = 0, Name = "Position 0 (Home)", SequenceIndex = 0 };
                homePos.IsEnabled = true;
                Positions.Add(homePos);

                // Add only the number of items configured (1 to TotalItems)
                for (int i = 1; i <= totalItems; i++)
                {
                    var existingPos = savedPositions.FirstOrDefault(p => p.PositionId == i);

                    if (existingPos != null)
                    {
                        existingPos.IsEnabled = true;
                        Positions.Add(existingPos);
                    }
                    else
                    {
                        // Create fresh if not found in JSON (e.g., config increased)
                        Positions.Add(new ServoPositionModel
                        {
                            PositionId = i,
                            Name = $"Position {i}",
                            SequenceIndex = i,
                            X = 0,
                            Y = 0,
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

                        CurrentRunningProgram =  SelectedProgramCode ;//Convert.ToInt32(programVal);
                    }

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
                _dialog.ShowWarning(
                "⚠️ UNSAVED CHANGES\n" +
                "You must SAVE your changes before leaving this page.");

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
                    _dialog.ShowMessage("Recipe loaded sucessfully.");
                }
                else
                {
                    _dialog.ShowWarning("Failed to update value. Please check logs.");
                }
                await Task.Delay(200);


                await _coreClient.WriteTagAsync(tagId, 0);

                await _servoService.SavePositionsAsync(Positions.ToList());
                HasUnsavedChanges = false;

                _logger.LogInfo($"{description} Confirmed.", LogType.Audit);
            }
            catch (Exception ex) { _logger.LogError($"Confirm Error ({description}): {ex.Message}", LogType.Diagnostics); }
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