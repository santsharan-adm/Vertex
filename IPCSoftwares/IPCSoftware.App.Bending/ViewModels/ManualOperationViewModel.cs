using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.ManualOperationMode;
using IPCSoftware.Shared.Models.Bending;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class ComboBoxItemManualPage
    {
        public string DisplayName { get; set; }
        public int Value { get; set; }
    }
    public class ManualOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private readonly Dictionary<string, ManualButtonOperation> _manualButtonOperations = new(StringComparer.OrdinalIgnoreCase);

        private SafePollerEx _manualOperationPoller;
        private bool _disposed;


        private int _manualPageVisibilityIndex;
        public int ManualPageListVisibility
        {
            get => _manualPageVisibilityIndex;
            set => SetProperty(ref _manualPageVisibilityIndex, value);

        }
        private List<ComboBoxItemManualPage> _comboBoxItemsManualPage;
        public List<ComboBoxItemManualPage> ComboBoxItemsManualPage
        {
            get => _comboBoxItemsManualPage;
            set => SetProperty(ref _comboBoxItemsManualPage, value);
        }

        private ComboBoxItemManualPage _selectedPage;
        public ComboBoxItemManualPage SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value);
        }
        private int _selectedPageIndex;
        public int SelectedPageIndex
        {
            get => _selectedPageIndex;
            set => SetProperty(ref _selectedPageIndex, value);
        }
        public ICommand SelectionChangedCommand { get; }
        public ICommand NextCommand { get; }

        public ICommand PreviousCommand { get; }
        public ICommand FirstCommand { get; }

        public ICommand LastCommand { get; }



        public ICommand ButtonCommand { get; }

        private bool _isManualButtonBusy;

        private bool _m02SupplyConveyorStopActive;
        public bool IsM02SupplyConveyorStopActive { get => _m02SupplyConveyorStopActive; set => SetProperty(ref _m02SupplyConveyorStopActive, value); }

        private bool _m02SupplyConveyorForwardActive;
        public bool IsM02SupplyConveyorForwardActive { get => _m02SupplyConveyorForwardActive; set => SetProperty(ref _m02SupplyConveyorForwardActive, value); }

        private bool _m02SupplyConveyorBackwardActive;
        public bool IsM02SupplyConveyorBackwardActive { get => _m02SupplyConveyorBackwardActive; set => SetProperty(ref _m02SupplyConveyorBackwardActive, value); }

        private bool _m02SupplyTrayChuckUnchuckActive;
        public bool IsM02SupplyTrayChuckUnchuckActive { get => _m02SupplyTrayChuckUnchuckActive; set => SetProperty(ref _m02SupplyTrayChuckUnchuckActive, value); }

        private bool _m02SupplyTrayChuckChuckActive;
        public bool IsM02SupplyTrayChuckChuckActive { get => _m02SupplyTrayChuckChuckActive; set => SetProperty(ref _m02SupplyTrayChuckChuckActive, value); }

        private bool _m02SupplyTrayReleaseDownActive;
        public bool IsM02SupplyTrayReleaseDownActive { get => _m02SupplyTrayReleaseDownActive; set => SetProperty(ref _m02SupplyTrayReleaseDownActive, value); }

        private bool _m02SupplyTrayReleaseCenterActive;
        public bool IsM02SupplyTrayReleaseCenterActive { get => _m02SupplyTrayReleaseCenterActive; set => SetProperty(ref _m02SupplyTrayReleaseCenterActive, value); }

        private bool _m02SupplyTrayReleaseUpActive;
        public bool IsM02SupplyTrayReleaseUpActive { get => _m02SupplyTrayReleaseUpActive; set => SetProperty(ref _m02SupplyTrayReleaseUpActive, value); }

        private bool _m02SupplyTrayStopperUpActive;
        public bool IsM02SupplyTrayStopperUpActive { get => _m02SupplyTrayStopperUpActive; set => SetProperty(ref _m02SupplyTrayStopperUpActive, value); }

        private bool _m02SupplyTrayStopperDownActive;
        public bool IsM02SupplyTrayStopperDownActive { get => _m02SupplyTrayStopperDownActive; set => SetProperty(ref _m02SupplyTrayStopperDownActive, value); }

        private bool _m02LiftConveyorStopActive;
        public bool IsM02LiftConveyorStopActive { get => _m02LiftConveyorStopActive; set => SetProperty(ref _m02LiftConveyorStopActive, value); }

        private bool _m02LiftConveyorForwardActive;
        public bool IsM02LiftConveyorForwardActive { get => _m02LiftConveyorForwardActive; set => SetProperty(ref _m02LiftConveyorForwardActive, value); }

        private bool _m02LiftConveyorBackwardActive;
        public bool IsM02LiftConveyorBackwardActive { get => _m02LiftConveyorBackwardActive; set => SetProperty(ref _m02LiftConveyorBackwardActive, value); }

        private bool _m02LiftTrayStopperUpActive;
        public bool IsM02LiftTrayStopperUpActive { get => _m02LiftTrayStopperUpActive; set => SetProperty(ref _m02LiftTrayStopperUpActive, value); }

        private bool _m02LiftTrayStopperDownActive;
        public bool IsM02LiftTrayStopperDownActive { get => _m02LiftTrayStopperDownActive; set => SetProperty(ref _m02LiftTrayStopperDownActive, value); }

        private bool _m02LiftTrayHolderBackwardActive;
        public bool IsM02LiftTrayHolderBackwardActive { get => _m02LiftTrayHolderBackwardActive; set => SetProperty(ref _m02LiftTrayHolderBackwardActive, value); }

        private bool _m02LiftTrayHolderForwardActive;
        public bool IsM02LiftTrayHolderForwardActive { get => _m02LiftTrayHolderForwardActive; set => SetProperty(ref _m02LiftTrayHolderForwardActive, value); }

        private bool _m02LiftUnitDownActive;
        public bool IsM02LiftUnitDownActive { get => _m02LiftUnitDownActive; set => SetProperty(ref _m02LiftUnitDownActive, value); }

        private bool _m02LiftUnitUpActive;
        public bool IsM02LiftUnitUpActive { get => _m02LiftUnitUpActive; set => SetProperty(ref _m02LiftUnitUpActive, value); }

        private bool _m02DischargeConveyorStopActive;
        public bool IsM02DischargeConveyorStopActive { get => _m02DischargeConveyorStopActive; set => SetProperty(ref _m02DischargeConveyorStopActive, value); }

        private bool _m02DischargeConveyorForwardActive;
        public bool IsM02DischargeConveyorForwardActive { get => _m02DischargeConveyorForwardActive; set => SetProperty(ref _m02DischargeConveyorForwardActive, value); }

        private bool _m02DischargeConveyorBackwardActive;
        public bool IsM02DischargeConveyorBackwardActive { get => _m02DischargeConveyorBackwardActive; set => SetProperty(ref _m02DischargeConveyorBackwardActive, value); }

        private bool _m02DischargeTrayChuckUnchunkActive;
        public bool IsM02DischargeTrayChuckUnchunkActive { get => _m02DischargeTrayChuckUnchunkActive; set => SetProperty(ref _m02DischargeTrayChuckUnchunkActive, value); }

        private bool _m02DischargeTrayChuckChukActive;
        public bool IsM02DischargeTrayChuckChukActive { get => _m02DischargeTrayChuckChukActive; set => SetProperty(ref _m02DischargeTrayChuckChukActive, value); }

        private bool _m02DischargeTrayReleaseDownActive;
        public bool IsM02DischargeTrayReleaseDownActive { get => _m02DischargeTrayReleaseDownActive; set => SetProperty(ref _m02DischargeTrayReleaseDownActive, value); }

        private bool _m02DischargeTrayReleaseCenterActive;
        public bool IsM02DischargeTrayReleaseCenterActive { get => _m02DischargeTrayReleaseCenterActive; set => SetProperty(ref _m02DischargeTrayReleaseCenterActive, value); }

        private bool _m02DischargeTrayReleaseUpActive;
        public bool IsM02DischargeTrayReleaseUpActive { get => _m02DischargeTrayReleaseUpActive; set => SetProperty(ref _m02DischargeTrayReleaseUpActive, value); }

        private bool _m02DischargeTrayStopperUpActive;
        public bool IsM02DischargeTrayStopperUpActive { get => _m02DischargeTrayStopperUpActive; set => SetProperty(ref _m02DischargeTrayStopperUpActive, value); }

        private bool _m02DischargeTrayStopperDownActive;
        public bool IsM02DischargeTrayStopperDownActive { get => _m02DischargeTrayStopperDownActive; set => SetProperty(ref _m02DischargeTrayStopperDownActive, value); }


        private BendingManualOperationModel _manualOperationModel = new();

        public BendingManualOperationModel ManualOperationModel
        {
            get => _manualOperationModel;
            set => SetProperty(ref _manualOperationModel, value);
        }
        ManualOperationPageModel _currentPage2;
        public ManualOperationPageModel CurrentPage
        {
            get => _currentPage2;
            set => SetProperty(ref _currentPage2, value);
        }

        public ManualOperationViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            NextCommand = new RelayCommand(ExecuteNext);

            PreviousCommand = new RelayCommand(ExecutePrevious);
            FirstCommand = new RelayCommand(ExecuteFirst);
            LastCommand = new RelayCommand(ExecuteLast);
            SelectionChangedCommand = new RelayCommand(ExecuteSelectionChanged);
            ButtonCommand = new RelayCommand<object>(async parameter => await ExecuteButtonActionAsync(parameter));
            LoadComboxIntems();
            InitializeManualButtonTags();
            //for (int i = 0; i < 24; i++)
            //{
            //    ManualPageListVisibility.Add(true);
            //}

            //ManualPageListVisibility[0] = true;

            //LoadPageData(1);
            //_currentPage = 1;
            //SelectedPage = _currentPage.ToString(ManualPageList[0]);


        }
        void LoadComboxIntems()
        {
            ComboBoxItemsManualPage = new List<ComboBoxItemManualPage>()
            {
                new ComboBoxItemManualPage()
                {
                    DisplayName = "Page 1 Ret To Origin",
                    Value = 1
                },
                new ComboBoxItemManualPage()
                {
                    DisplayName = "Page 2 M02",
                    Value = 2
                },
                new ComboBoxItemManualPage()
                {
                      DisplayName = "Page 3 M03",
                    Value = 3

                },
                new ComboBoxItemManualPage()
                {
                      DisplayName = "Page 4 M04",
                    Value = 4

                },
                  new ComboBoxItemManualPage()
                {
                      DisplayName = "Page 5 M05",
                    Value = 5

                },
                  new ComboBoxItemManualPage()
                {
                      DisplayName = "Page 6 M06",
                    Value = 6

                },
                  new ComboBoxItemManualPage()
                {
                      DisplayName = "Page 7 M07",
                    Value = 7

                },
                  new ComboBoxItemManualPage()
                {
                      DisplayName = "Page 8 M09",
                    Value = 8

                },
                     new ComboBoxItemManualPage()
                {
                      DisplayName = "Page 9 M10",
                    Value = 9

                },
                       new ComboBoxItemManualPage()
                       {
                      DisplayName = "Page 10 M12",
                    Value = 10

                },
                               new ComboBoxItemManualPage()
                       {
                      DisplayName = "Page 11 M15",
                    Value = 11

                }


            };
        }
    
       
        private void ExecuteSelectionChanged() 
        {
           // string a = SelectedPage;
            //int pageIndex = ManualPageList.IndexOf(SelectedPage);
            //LoadPageData(pageIndex+1);
        }
        private void ExecuteFirst()
        {
            SelectedPageIndex = 0;
            //SelectedPage = _currentPage.ToString(ManualPageList[_currentPage-1]);
            //LoadPageData(_currentPage);
        }

        private void ExecuteLast()
        {
            SelectedPageIndex = ComboBoxItemsManualPage.Count;
            //SelectedPage = _currentPage.ToString(ManualPageList[_currentPage-1]);
            //LoadPageData(_currentPage);
        }

        
        private void ExecuteNext()
        {
            if (SelectedPageIndex < (ComboBoxItemsManualPage.Count-1))
            {
                SelectedPageIndex++;
            }
            else
            {
                SelectedPageIndex = 0;
            }
        }
        private void ExecutePrevious()
        {
            if (SelectedPageIndex > 0)
            {
                SelectedPageIndex--;
            }
            else
            {
                SelectedPageIndex = ComboBoxItemsManualPage.Count-1;
            }
        }

        public void Initialize()
        {
            _manualOperationPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateManualOperationFromService,
                _logger,
                ex => _logger.LogError(
                    $"[ManualOperation] Poller Error : {ex.Message}",
                    LogType.Diagnostics),
                requestId: 0); // Update Actual RequestId
         
            _manualOperationPoller.Start();
        }

        private void InitializeManualButtonTags()
        {
            // PLC tag placeholders:
            // Replace null with the actual write/read PLC tag ids for each button as they become available.
            RegisterManualButton("M02SupplyConveyorStop", nameof(IsM02SupplyConveyorStopActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyConveyorForward", nameof(IsM02SupplyConveyorForwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyConveyorBackward", nameof(IsM02SupplyConveyorBackwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyTrayChuckUnchuck", nameof(IsM02SupplyTrayChuckUnchuckActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyTrayChuckChuck", nameof(IsM02SupplyTrayChuckChuckActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyTrayReleaseDown", nameof(IsM02SupplyTrayReleaseDownActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyTrayReleaseCenter", nameof(IsM02SupplyTrayReleaseCenterActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyTrayReleaseUp", nameof(IsM02SupplyTrayReleaseUpActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyTrayStopperUp", nameof(IsM02SupplyTrayStopperUpActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02SupplyTrayStopperDown", nameof(IsM02SupplyTrayStopperDownActive), writeTagId: null, readTagId: null);

            RegisterManualButton("M02LiftConveyorStop", nameof(IsM02LiftConveyorStopActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftConveyorForward", nameof(IsM02LiftConveyorForwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftConveyorBackward", nameof(IsM02LiftConveyorBackwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftTrayStopperUp", nameof(IsM02LiftTrayStopperUpActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftTrayStopperDown", nameof(IsM02LiftTrayStopperDownActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftTrayHolderBackward", nameof(IsM02LiftTrayHolderBackwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftTrayHolderForward", nameof(IsM02LiftTrayHolderForwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftUnitDown", nameof(IsM02LiftUnitDownActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02LiftUnitUp", nameof(IsM02LiftUnitUpActive), writeTagId: null, readTagId: null);

            RegisterManualButton("M02DischargeConveyorStop", nameof(IsM02DischargeConveyorStopActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeConveyorForward", nameof(IsM02DischargeConveyorForwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeConveyorBackward", nameof(IsM02DischargeConveyorBackwardActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeTrayChuckUnchunk", nameof(IsM02DischargeTrayChuckUnchunkActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeTrayChuckChuk", nameof(IsM02DischargeTrayChuckChukActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeTrayReleaseDown", nameof(IsM02DischargeTrayReleaseDownActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeTrayReleaseCenter", nameof(IsM02DischargeTrayReleaseCenterActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeTrayReleaseUp", nameof(IsM02DischargeTrayReleaseUpActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeTrayStopperUp", nameof(IsM02DischargeTrayStopperUpActive), writeTagId: null, readTagId: null);
            RegisterManualButton("M02DischargeTrayStopperDown", nameof(IsM02DischargeTrayStopperDownActive), writeTagId: null, readTagId: null);
        }

        private void RegisterManualButton(string commandParameter, string statePropertyName, int? writeTagId, int? readTagId)
        {
            _manualButtonOperations[commandParameter] = new ManualButtonOperation(
                commandParameter,
                statePropertyName,
                writeTagId,
                readTagId);
        }

        private ManualButtonOperation GetManualButtonOperation(string commandParameter)
        {
            if (_manualButtonOperations.TryGetValue(commandParameter, out var operation))
                return operation;

            var propertyName = $"Is{commandParameter}Active";
            operation = new ManualButtonOperation(commandParameter, propertyName, writeTagId: null, readTagId: null);
            _manualButtonOperations[commandParameter] = operation;

            return operation;
        }

        private async Task ExecuteButtonActionAsync(object parameter)
        {
            if (parameter == null)
                return;

            var commandText = parameter.ToString();
            if (string.IsNullOrWhiteSpace(commandText))
                return;

            var parts = commandText.Split('|');
            var commandParameter = parts[0];
            bool isPressed = false;
            var isMomentary = parts.Length == 2 && bool.TryParse(parts[1], out isPressed);
            var operation = GetManualButtonOperation(commandParameter);

            if (_isManualButtonBusy && !isMomentary)
                return;

            try
            {
                if (isMomentary)
                {
                    await WriteManualButtonAsync(operation, isPressed ? 1 : 0);
                    SetManualButtonState(operation, isPressed);
                    _logger.LogInfo($"[ManualOperation] {operation.CommandParameter} -> {(isPressed ? 1 : 0)}", LogType.Audit);
                    return;
                }

                _isManualButtonBusy = true;
                SetManualButtonState(operation, true);
                await WriteManualButtonAsync(operation, 1);
                _logger.LogInfo($"[ManualOperation] Pulse {operation.CommandParameter}", LogType.Audit);
                await Task.Delay(100);
                await WriteManualButtonAsync(operation, 0);
                SetManualButtonState(operation, false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ManualOperation] Button Error ({operation.CommandParameter}) : {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                if (!isMomentary)
                    _isManualButtonBusy = false;
            }
        }

        private async Task WriteManualButtonAsync(ManualButtonOperation operation, int value)
        {
            if (!operation.WriteTagId.HasValue)
            {
                _logger.LogWarning($"[ManualOperation] PLC write tag not configured for {operation.CommandParameter}.", LogType.Audit);
                return;
            }

            await _coreClient.WriteTagAsync(operation.WriteTagId.Value, value);
        }

        private void SetManualButtonState(ManualButtonOperation operation, bool isActive)
        {
            var property = GetType().GetProperty(operation.StatePropertyName);
            if (property?.CanWrite == true && property.PropertyType == typeof(bool))
            {
                property.SetValue(this, isActive);
                return;
            }

            OnPropertyChanged(operation.StatePropertyName);
        }

        private async Task UpdateManualOperationFromService(
            Dictionary<int, object> data)
        {
            try
            {
               
                if (data == null)
                    return;

                if (data.TryGetValue(0, out object modelObj)) // Update RequestId
                {
                    var model = Deserialize<BendingManualOperationModel>(modelObj);

                    if (model != null)
                    {
                        ManualOperationModel = model;
                    }
                }

                foreach (var operation in _manualButtonOperations.Values.Where(x => x.ReadTagId.HasValue))
                {
                    if (data.TryGetValue(operation.ReadTagId.Value, out object value))
                    {
                        SetManualButtonState(operation, Convert.ToBoolean(value));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"[ManualOperation] Update Error : {ex.Message}",
                    LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        private static T Deserialize<T>(object raw) where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(raw);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _manualOperationPoller?.Stop();
            _manualOperationPoller?.Dispose();
        }

        private sealed class ManualButtonOperation
        {
            public ManualButtonOperation(string commandParameter, string statePropertyName, int? writeTagId, int? readTagId)
            {
                CommandParameter = commandParameter;
                StatePropertyName = statePropertyName;
                WriteTagId = writeTagId;
                ReadTagId = readTagId;
            }

            public string CommandParameter { get; }
            public string StatePropertyName { get; }
            public int? WriteTagId { get; }
            public int? ReadTagId { get; }
        }
    }
}
