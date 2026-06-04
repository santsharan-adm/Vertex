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
    public class ManualOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;

        private SafePollerEx _manualOperationPoller;
        private bool _disposed;


        private List<string> _manualPageList;
        public List<string> ManualPageList
        {
            get => _manualPageList;
            set => SetProperty(ref _manualPageList, value);

        }

        private String _selectedPage;
        public String SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value);
        }
        public ICommand SelectionChangedCommand { get; }
        public ICommand NextCommand { get; }

        public ICommand PreviousCommand { get; }
        public ICommand FirstCommand { get; }

        public ICommand LastCommand { get; }

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
            ManualPageList = new List<string>() { "Page 1","Page 2","Page 3", "Page 4", "Page 5", "Page 6" , "Page 7", "Page 8", "Page 9" , "Page 10", "Page 11", "Page 12",
                                                   "Page 13","Page 14","Page 15", "Page 16", "Page 17", "Page 18" , "Page 19", "Page 20", "Page 21" , "Page 22", "Page 23", "Page 24"};
            LoadPageData(1);
            _currentPage = 1;
            SelectedPage = _currentPage.ToString(ManualPageList[0]);


        }
        private void ExecuteSelectionChanged() 
        {
           // string a = SelectedPage;
            int pageIndex = ManualPageList.IndexOf(SelectedPage);
            LoadPageData(pageIndex+1);
        }
        private void ExecuteFirst()
        {
            _currentPage = 1;
            SelectedPage = _currentPage.ToString(ManualPageList[_currentPage-1]);
            LoadPageData(_currentPage);
        }

        private void ExecuteLast()
        {
            _currentPage = ManualPageList.Count;
            SelectedPage = _currentPage.ToString(ManualPageList[_currentPage-1]);
            LoadPageData(_currentPage);
        }

        private void LoadPageData(int pageNo)
        {
            if (pageNo < 1)
                pageNo = ManualPageList.Count();
            if (pageNo > ManualPageList.Count)
                pageNo = 1;
            CurrentPage = new ManualOperationPageModel();
            if (pageNo == 1)
            {

                LoadPage1();   
               // return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 2)
            {

               LoadPage2();
               // return CurrentPage.SelectedButtons[1];
            }

            else if (pageNo == 3)
            {

                LoadPage3();
               // return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 4)
            {

                LoadPage4();
               // return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 5)
            {

                LoadPage5();
              //  return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 6)
            {

                LoadPage6();
               /// return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 7)
            {

                LoadPage7();
              //  return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 8)
            {

                LoadPage8();             //   return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 9)
            {

                LoadPage9();
           //     return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 10)
            {

                LoadPage10();
            //    return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 11)
            {

                LoadPage11();
            //    return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 12)
            {

                LoadPage12();
            //    return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 13)
            {
                LoadPage13();
            }
            
            else if (pageNo == 14)
            {
                LoadPage14();
            }

            else if (pageNo == 15)
            {
                LoadPage15();
            }

            else if (pageNo == 16)
            {
                LoadPage16();
            }

            else if (pageNo == 17)
            {
                LoadPage17();
            }

            else if (pageNo == 18)
            {
                LoadPage18();
            }

            else if (pageNo == 19)
            {
                LoadPage19();
            }

            else if (pageNo == 20)
            {
                LoadPage20();
            }

            else if (pageNo == 21)
            {
                LoadPage21();
            }

            else if (pageNo== 22)
            {
                LoadPage22();
            }

            else if (pageNo == 23)
            {
                LoadPage23();
            }

            else if (pageNo == 24)
            {
                LoadPage24();
            }
        }

        void LoadPage1()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "IN1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "IN15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "IN16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "IN17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "IN18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "IN19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "IN20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 1 Back To Origin";
        }

        void LoadPage2()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                 new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Stop",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Forward",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Reverse",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply ChuckLR Open",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply ChuckLR Close",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Up",
                    CommandParameter = 6,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Mid",
                    CommandParameter = 7,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Down",
                    CommandParameter = 8,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayStopper Up",
                    CommandParameter = 9,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayStopper Down",
                    CommandParameter = 10,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Stop",
                    CommandParameter = 11,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Forward",
                    CommandParameter = 12,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Reverse",
                    CommandParameter = 13,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge ChuckLR Open",
                    CommandParameter = 14,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge ChuckLR Close",
                    CommandParameter = 15,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Up",
                    CommandParameter = 16,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Mid",
                    CommandParameter = 17,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Down",
                    CommandParameter = 18,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayStopper Up",
                    CommandParameter = 19,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayStopper Down",
                    CommandParameter = 20,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                 }
                };
            CurrentPage.Title = "Page 2 M02";
        }

        void LoadPage3()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                {
                    Content = "Lift_Conveyor_Stop",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift Conveyor Forward",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift Conveyor Reverse",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift TrayStopper Up",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift TrayStopper Down",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift TrayStopper Back",
                    CommandParameter = 6,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift TrayPress Forward",
                    CommandParameter = 7,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift Pos1",
                    CommandParameter = 8,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift Pos2",
                    CommandParameter = 9,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Lift Pos3",
                    CommandParameter = 10,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                    {
                        Content = "JOG Minus",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "JOG Plus",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "MachineHome",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB 1 DigitalZero",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "RB 2 DigitalZero",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB 3 DigitalZero",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "RB 4 DigitalZero",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 3 M02";
        }

        void LoadPage4()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                 new ManualOperationItemModel()
                    {
                        Content = "Supply Conveyor Stop",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply Conveyor Forward",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply Conveyor Reverse",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply ChuckLR Open",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply ChuckLR Close",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply TrayReleaseLR Up",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply TrayReleaseLR Mid",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply TrayReleaseLR Down",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply TrayStopper Up",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Supply TrayStopper Down",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge Conveyor Stop",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge Conveyor Forward",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge Conveyor Reverse",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge ChuckLR Open",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge ChuckLR Close",
                        CommandParameter = 15,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge TrayReleaseLR Up",
                        CommandParameter = 16,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge TrayReleaseLR Mid",
                        CommandParameter = 17,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge TrayReleaseLR Down",
                        CommandParameter = 18,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge TrayStopper Up",
                        CommandParameter = 19,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Discharge TrayStopper Down",
                        CommandParameter = 20,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                };
            CurrentPage.Title = "Page 4 M03";
        }

        void LoadPage5()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "Lift Conveyor Stop",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift Conveyor Forward",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift Conveyor Reverse",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift TrayStopper Up",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift TrayStopper Down",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift TrayPress Back",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift TrayPress Forward",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift Pos1",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift Pos2",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Lift Pos3",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "JOG Minus",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "JOG Plus",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Machine Home",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB 1 DigitalZero",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB 2 DigitalZero",
                        CommandParameter = 15,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB 3 DigitalZero",
                        CommandParameter = 16,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB 4 DigitalZero",
                        CommandParameter = 17,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter = 18,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter = 19,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter = 20,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                };
            CurrentPage.Title = "Page 5 M03";
        }

        void LoadPage6()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                 new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak1",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum1",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak2",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum2",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak3",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum3",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak4",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum4",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit1 Up",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit1 Down",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit2 Up",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit2 Down",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit3 Up",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit3 Down",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit4 Up",
                        CommandParameter = 15,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit4 Down",
                        CommandParameter = 16,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB HomeReturn",
                        CommandParameter = 17,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter = 18,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter = 19,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter = 20,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                };
            CurrentPage.Title = "Page 6 M10";
        }

        void LoadPage7()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                 new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak1",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum1",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak2",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum2",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak3",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum3",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak4",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum4",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit1 Up",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit1 Down",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit2 Up",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit2 Down",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit3 Up",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit3 Down",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit4 Up",
                        CommandParameter = 15,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit4 Down",
                        CommandParameter = 16,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB HomeReturn",
                        CommandParameter = 17,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter = 18,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter = 19,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter = 20,
                        IsEnabled = false,
                        IsVisible = Visibility.Hidden,
                        IsChecked = false
                    },
                };
            CurrentPage.Title = "Page 7 M11";
        }

        void LoadPage8()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                   new ManualOperationItemModel()
                    {
                        Content = "AbnormalTray Stopper Up",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "AbnormalTray Stopper Down",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "AbnormalTray Stopper Up",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "AbnormalTray Stopper Down",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 8 M15";
        }

        void LoadPage9()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                {
                    Content = "TempTable VacBreak1",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "TempTable Vacuum1",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "TempTable VacBreak2",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "TempTable Vacuum2",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "TempTable VacBreak3",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "TempTable Vacuum3",
                    CommandParameter = 6,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "TempTable VacBreak4",
                    CommandParameter = 7,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "TempTable Vacuum4",
                    CommandParameter = 8,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Button9",
                    CommandParameter = 9,
                    IsEnabled = false,
                    IsVisible = Visibility.Hidden,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Button10",
                    CommandParameter = 10,
                    IsEnabled = false,
                    IsVisible = Visibility.Hidden,
                    IsChecked = false
                },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=false,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 9 M16";
        }

        void LoadPage10()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Hidden,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 10";
        }

        void LoadPage11()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 11";
        }

        void LoadPage12()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 12";
        }

        void LoadPage13()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                   new ManualOperationItemModel()
                {
                    Content = "IN1",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN2",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN3",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN4",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN5",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN6",
                    CommandParameter = 6,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN7",
                    CommandParameter = 7,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN8",
                    CommandParameter = 8,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN9",
                    CommandParameter = 9,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN10",
                    CommandParameter = 10,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN11",
                    CommandParameter = 11,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN12",
                    CommandParameter = 12,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN13",
                    CommandParameter = 13,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN14",
                    CommandParameter = 14,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN15",
                    CommandParameter = 15,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN16",
                    CommandParameter = 16,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN17",
                    CommandParameter = 17,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN18",
                    CommandParameter = 18,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN19",
                    CommandParameter = 19,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "IN20",
                    CommandParameter = 20,
                    IsEnabled = true,
                    IsVisible = Visibility.Visible,
                    IsChecked = false
                }
                };
            CurrentPage.Title = "Page 13";
        }

        void LoadPage14()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "Index1 Return Odeg",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index1 One Pitch Rotate",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST Vac Break",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST Vacuum",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST Vac Break",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST Vacuum",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST Vac Break",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST Vacuum",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST Vac Break",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST Vacuum",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index1 JOG Minus",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index1 JOG Plus",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index1 Machine Home",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 14 M04";
        }

        void LoadPage15()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "1 Punch Down",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 Punch Up",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 Punch Down",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 Punch Up",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 1ST Heater OFF",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 1ST Heater ON",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 2ST Heater OFF",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 2ST Heater ON",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 3ST Heater OFF",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 3ST Heater ON",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 4ST Heater OFF",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1 4ST Heater ON",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 1ST Heater OFF",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 1ST Heater ON",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 2ST Heater OFF",
                        CommandParameter = 15,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 2ST Heater ON",
                        CommandParameter = 16,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 3ST Heater OFF",
                        CommandParameter = 17,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 3ST Heater ON",
                        CommandParameter = 18,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 4ST Heater OFF",
                        CommandParameter = 19,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2 4ST Heater ON",
                        CommandParameter = 20,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                };
            CurrentPage.Title = "Page 15 M05";
        }

        void LoadPage16()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                   new ManualOperationItemModel()
                    {
                        Content = "1ST DigitalZero",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST DigitalZero",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST DigitalZero",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST DigitalZero",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST DigitalZero",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST DigitalZero",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST DigitalZero",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST DigitalZero",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 16 M05";
        }

        void LoadPage17()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                   new ManualOperationItemModel()
                    {
                        Content = "1ST Heater OFF",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST Heater ON",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST Heater OFF",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST Heater ON",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST Heater OFF",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST Heater ON",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST Heater OFF",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST Heater ON",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST DigitalZero",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST DigitalZero",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST DigitalZero",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST DigitalZero",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 17 M06";
        }

        void LoadPage18()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "Index2 Home",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 Rotate",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 ReceiveUnit Down",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 ReceiveUnit Up",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 VacUnit Up",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 VacUnit Down",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac1 Break",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac1 Vacuum",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac2 Break",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac2 Vacuum",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac3 Break",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac3 Vacuum",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac4 Break",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Vac4 Vacuum",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 JOG Minus",
                        CommandParameter = 15,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 JOG Plus",
                        CommandParameter = 16,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index2 MachineHome",
                        CommandParameter = 17,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter = 18,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter = 19,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter = 20,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                };
            CurrentPage.Title = "Page 18 M12";
        }

        void LoadPage19()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                 new ManualOperationItemModel()
                    {
                        Content = "Index1 ReturnOdeg",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index1 OnePitchRotate",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST VacBreak",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST Vacuum",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST VacBreak",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST Vacuum",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST VacBreak",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST Vacuum",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index3 JOG Minus",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index3 JOG Plus",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Index3 MachineHome",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 19 M07";
        }

        void LoadPage20()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "Liner Clamp Up",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner Clamp Down",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner ChuckUnit Back",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner ChuckUnit Forward",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner ChuckUnit Down",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner ChuckUnit Up",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner Chuck Open",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner Chuck Close",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner PeelUnit Down",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner PeelUnit Up",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner WasteSuction OFF",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Liner WasteSuction ON",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 20 M08";
        }

        void LoadPage21()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "Flip RotateUnit Up",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Flip RotateUnit Down",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Flip RotateUnit Return",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Flip RotateUnit Rotate",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST VacBreak",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "1ST Vacuum",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST VacBreak",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "2ST Vacuum",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST VacBreak",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "3ST Vacuum",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST VacBreak",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "4ST Vacuum",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = Visibility.Visible,
                        IsChecked = false
                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 21 M09";
        }

        void LoadPage22()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 22";
        }

        void LoadPage23()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 23";
        }

        void LoadPage24()
        {
            CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=Visibility.Visible,
                        IsChecked=false

                    },
                };
            CurrentPage.Title = "Page 24";
        }

        int _currentPage;
        private void ExecuteNext()
        {
            try
            {
                // Next Screen Navigation
                _currentPage++;
                SelectedPage = _currentPage.ToString(ManualPageList[_currentPage - 1]);
                LoadPageData(_currentPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"[ManualOperation] Next Navigation Error : {ex.Message}",
                    LogType.Diagnostics);
            }
        }
        private void ExecutePrevious()
        {
            try
            {
                // Previous Screen Navigation
                _currentPage--;
                SelectedPage = _currentPage.ToString(ManualPageList[_currentPage - 1]);
                LoadPageData(_currentPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"[ManualOperation] Previous Navigation Error : {ex.Message}",
                    LogType.Diagnostics);
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