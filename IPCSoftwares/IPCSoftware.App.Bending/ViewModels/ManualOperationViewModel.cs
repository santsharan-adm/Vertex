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
            LoadComboxIntems();
            //for (int i = 0; i < 24; i++)
            //{
            //    ManualPageListVisibility.Add(true);
            //}

            //ManualPageListVisibility[0] = true;

            //LoadPageData(1);
            //_currentPage = 1;
            //SelectedPage = _currentPage.ToString(ManualPageList[0]);


            ButtonCommand = new RelayCommand<string>(ExecuteButtonAction);


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
    
        private void ExecuteButtonAction(string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return;

            switch (parameter)
            {
                // PAGE 2: M02 - INPUT Module (SUPPLY)

                case "M02SupplyConveyorStop":
                    break;
                case "M02SupplyConveyorForward":
                    break;
                case "M02SupplyConveyorBackward":
                    break;
                case "M02SupplyTrayChuckUnchuck":
                    break;
                case "M02SupplyTrayChuckChuck":
                    break;
                case "M02SupplyTrayReleaseDown":
                    break;
                case "M02SupplyTrayReleaseCenter":
                    break;
                case "M02SupplyTrayReleaseUp":
                    break;
                case "M02SupplyTrayStopperUp":
                    break;
                case "M02SupplyTrayStopperDown":
                    break;
                case "M02LiftConveyorStop":
                    break;
                case "M02LiftConveyorForward":
                    break;
                case "M02LiftConveyorBackward":
                    break;
                case "M02LiftTrayStopperUp":
                    break;
                case "M02LiftTrayStopperDown":
                    break;
                case "M02LiftTrayHolderBackward":
                    break;
                case "M02LiftTrayHolderForward":
                    break;
                case "M02LiftUnitDown":
                    break;
                case "M02LiftUnitUp":
                    break;
                case "M02DischargeConveyorStop":
                    break;
                case "M02DischargeConveyorForward":
                    break;
                case "M02DischargeConveyorBackward":
                    break;
                case "M02DischargeTrayChuckUnchunk":
                    break;
                case "M02DischargeTrayChuckChuk":
                    break;
                case "M02DischargeTrayReleaseDown":
                    break;
                case "M02DischargeTrayReleaseCenter":
                    break;
                case "M02DischargeTrayReleaseUp":
                    break;
                case "M02DischargeTrayStopperUp":
                    break;
                case "M02DischargeTrayStopperDown":
                    break;

                //// PAGE 3: M03 - INPUT Module


                //case "M03SupplyConveyorStop":
                //    break;
                //case "M03SupplyConveyorForward":
                //    break;
                //case "M03SupplyConveyorBackward":
                //    break;
                //case "M03SupplyTrayChuckUnchuck":
                //    break;
                //case "M03SupplyTrayChuckChuck":
                //    break;
                //case "M03SupplyTrayReleaseDown":
                //    break;
                //case "M03SupplyTrayReleaseCenter":
                //    break;
                //case "M03SupplyTrayReleaseUp":
                //    break;
                //case "M03SupplyTrayStopperUp":
                //    break;
                //case "M03SupplyTrayStopperDown":
                //    break;
                //case "M03LiftConveyorStop":
                //    break;
                //case "M03LiftConveyorForward":
                //    break;
                //case "M03LiftConveyorBackward":
                //    break;
                //case "M03LiftTrayStopperUp":
                //    break;
                //case "M03LiftTrayStopperDown":
                //    break;
                //case "M03LiftTrayHolderBackward":
                //    break;
                //case "M03LiftTrayHolderForward":
                //    break;
                //case "M03LiftUnitDown":
                //    break;
                //case "M03LiftUnitUp":
                //    break;
                //case "M03DischargeConveyorStop":
                //    break;
                //case "M03DischargeConveyorForward":
                //    break;
                //case "M03DischargeConveyorBackward":
                //    break;
                //case "M03DischargeTrayChuckUnchuck":
                //    break;
                //case "M03DischargeTrayChuckChuck":
                //    break;
                //case "M03DischargeTrayReleaseDown":
                //    break;
                //case "M03DischargeTrayReleaseCenter":
                //    break;
                //case "M03DischargeTrayReleaseUp":
                //    break;
                //case "M03DischargeTrayStopperUp":
                //    break;
                //case "M03DischargeTrayStopperDown":
                //    break;

                //// PAGE 4: M04 - INDEX 1 UNIT

                //case "M04Index1CoordinateStop":
                //    break;
                //case "M04Index1CoordinateForward":
                //    break;
                //case "M04Index1CoordinateBackward":
                //    break;
                //case "M04Index1ActionReturnTo0":
                //    break;
                //case "M04Index1ActionRotate1Pitch":
                //    break;
                //case "M02InputModuleIndex1ActionOff":
                //    break;
                //case "M02InputModuleIndex1ActionOn":
                //    break;
                //case "M02InputModuleIndex1SectionOff":
                //    break;
                //case "M02InputModuleIndex1SectionOn":
                //    break;
                //case "M02InputModuleIndex2SectionOff":
                //    break;
                //case "M02InputModuleIndex2SectionOn":
                //    break;
                //case "M02InputModuleIndex3SectionOff":
                //    break;
                //case "M02InputModuleIndex3SectionOn":
                //    break;

                //// PAGE 5: M05 - BENDING 1,2


                //case "M052ClampUp":
                //    break;
                //case "M052ClampDown":
                //    break;
                //case "M05PunchDown":
                //    break;
                //case "M05PunchUp":
                //    break;
                //case "M05HeaterOff":
                //    break;
                //case "M05HeaterOn":
                //    break;
                //case "M052NdHeaterOff":
                //    break;
                //case "M052NdHeaterOn":
                //    break;
                //case "M053RdHeaterOff":
                //    break;
                //case "M053RdHeaterOn":
                //    break;
                //case "M054ThHeaterOff":
                //    break;
                //case "M054ThHeaterOn":
                //    break;
                //case "M052DieUp":
                //    break;
                //case "M052DieDown":
                //    break;
                //case "M052PunchDown":
                //    break;
                //case "M052PunchUp":
                //    break;
                //case "M051StHeaterOff":
                //    break;
                //case "M051StHeaterOn":
                //    break;

                //// PAGE 6: M06 - BENDING 3

                //case "M06ClampUp":
                //    break;
                //case "M06ClampDown":
                //    break;
                //case "M06PunchUp":
                //    break;
                //case "M06PunchDown":
                //    break;
                //case "M061StHeaterOff":
                //    break;
                //case "M061StHeaterOn":
                //    break;
                //case "M062NdHeaterOff":
                //    break;
                //case "M062NdHeaterOn":
                //    break;
                //case "M063RdHeaterOff":
                //    break;
                //case "M063RdHeaterOn":
                //    break;
                //case "M064ThHeaterOff":
                //    break;
                //case "M064ThHeaterOn":
                //    break;

                //// PAGE 7 & 8: M07 & M08 - INDEX 3 & LINEAR REMOVAL


                //case "M07Index3ActionRrturnTo0":
                //    break;
                //case "M07Index3ActionRotate1Pitch":
                //    break;
                //case "M071StSectionOff":
                //    break;
                //case "M071StSectionOn":
                //    break;
                //case "M072NdSectionOff":
                //    break;
                //case "M072NdSectionOn":
                //    break;
                //case "M073rdSectionOff":
                //    break;
                //case "M073rdSectionOn":
                //    break;
                //case "M08ClampUp":
                //    break;
                //case "M08ClampDown":
                //    break;
                //case "M08ChuckUnitBackward":
                //    break;
                //case "M08ChuckUnitForward":
                //    break;
                //case "M08ChuckUnitUp":
                //    break;
                //case "M08ChuckUnitDown":
                //    break;
                //case "M08UnChuck":
                //    break;
                //case "M08Chuck":
                //    break;
                //case "M08RemovalUnitDown":
                //    break;
                //case "M08RemovalUnitUp":
                //    break;
                //case "M08WasteSectionOff":
                //    break;
                //case "M08WasteSectionOn":
                //    break;

                //// PAGE 9: M09 - WORK FLIP


                //case "M09FlipUnitUp":
                //    break;
                //case "M09FlipUnitDown":
                //    break;
                //case "M09FlipUnitHome":
                //    break;
                //case "M09FlipUnitRotate":
                //    break;
                //case "M091STSectionOff":
                //    break;
                //case "M091STSectionOn":
                //    break;
                //case "M092NDSectionOff":
                //    break;
                //case "M092NDSectionOn":
                //    break;
                //case "M093RDSectionOff":
                //    break;
                //case "M093RDSectionOn":
                //    break;
                //case "M094THSectionOff":
                //    break;
                //case "M094THSectionOn":
                //    break;

                //// PAGE 10: M10 - IN ROBOT & M15 NG SHOOTER
                //case "M15InTrayStopperUP":
                //    break;
                //case "M15InTrayStopperDOWN":
                //    break;
                //case "M10OutTrayStopperUP":
                //    break;
                //case "M10OutTrayStopperDOWN":
                //    break;
                //case "M101STSectionOFF":
                //    break;
                //case "M101STSectionON":
                //    break;
                //case "M102NDSectionOFF":
                //    break;
                //case "M102NDSectionON":
                //    break;
                //case "M103RDSectionOFF":
                //    break;
                //case "M103RDSectionON":
                //    break;
                //case "M104THSectionOFF":
                //    break;
                //case "M104THSectionON":
                //    break;

               
                //// PAGE 11: M12 - INDEX 2
             
                //case "M12Index2ActionHome":
                //    break;
                //case "M12Index2ActionRotate":
                //    break;
                //case "M121StSectionDown":
                //    break;
                //case "M121StSectionUp":
                //    break;
                //case "M122NdSectionUp":
                //    break;
                //case "M122NdSectionDown":
                //    break;
                //case "M12Index1ActionOff":
                //    break;
                //case "M12Index1ActionOn":
                //    break;
                //case "M121StSectionOff":
                //    break;
                //case "M121StSectionOn":
                //    break;
                //case "M123RdSectionOff":
                //    break;
                //case "M123RdSectionOn":
                //    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"Unhandled Parameter: {parameter}");
                    break;
            }
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
    }
}