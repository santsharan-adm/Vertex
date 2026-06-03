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
            //LoadPageData(1);
            ManualPageList = new List<string>() { "Page 1","Page 2","Page 3", "Page 4", "Page 5", "Page 6" , "Page 7", "Page 8", "Page 9" , "Page 10", "Page 11", "Page 12" };
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

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button12345",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 1";               
               // return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 2)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                 new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Stop",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Forward",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Reverse",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply ChuckLR Open",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply ChuckLR Close",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Up",
                    CommandParameter = 6,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Mid",
                    CommandParameter = 7,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Down",
                    CommandParameter = 8,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayStopper Up",
                    CommandParameter = 9,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayStopper Down",
                    CommandParameter = 10,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Stop",
                    CommandParameter = 11,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Forward",
                    CommandParameter = 12,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Reverse",
                    CommandParameter = 13,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge ChuckLR Open",
                    CommandParameter = 14,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge ChuckLR Close",
                    CommandParameter = 15,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Up",
                    CommandParameter = 16,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Mid",
                    CommandParameter = 17,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Down",
                    CommandParameter = 18,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayStopper Up",
                    CommandParameter = 19,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayStopper Down",
                    CommandParameter = 20,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                 }
                };
                CurrentPage.Title = "Page 2";
               // return CurrentPage.SelectedButtons[1];
            }

            else if (pageNo == 3)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Stop",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Forward",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply Conveyor Reverse",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply ChuckLR Open",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply ChuckLR Close",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Up",
                    CommandParameter = 6,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Mid",
                    CommandParameter = 7,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayReleaseLR Down",
                    CommandParameter = 8,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayStopper Up",
                    CommandParameter = 9,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Supply TrayStopper Down",
                    CommandParameter = 10,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Stop",
                    CommandParameter = 11,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Forward",
                    CommandParameter = 12,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge Conveyor Reverse",
                    CommandParameter = 13,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge ChuckLR Open",
                    CommandParameter = 14,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge ChuckLR Close",
                    CommandParameter = 15,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Up",
                    CommandParameter = 16,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Mid",
                    CommandParameter = 17,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayReleaseLR Down",
                    CommandParameter = 18,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayStopper Up",
                    CommandParameter = 19,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Discharge TrayStopper Down",
                    CommandParameter = 20,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                };
                CurrentPage.Title = "Page 3";
               // return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 4)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                {
                    Content = "RB VacBreak1",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB Vacuum1",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacBreak2",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB Vacuum2",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacBreak3",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB Vacuum3",
                    CommandParameter = 6,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacBreak4",
                    CommandParameter = 7,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB Vacuum4",
                    CommandParameter = 8,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit1 Up",
                    CommandParameter = 9,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit1 Down",
                    CommandParameter = 10,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit2 Up",
                    CommandParameter = 11,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit2 Down",
                    CommandParameter = 12,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit3 Up",
                    CommandParameter = 13,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit3 Down",
                    CommandParameter = 14,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit4 Up",
                    CommandParameter = 15,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "RB VacUnit4 Down",
                    CommandParameter = 16,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                    new ManualOperationItemModel()
                    {
                        Content = "RB HomeReturn",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 4";
               // return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 5)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak1",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum1",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak2",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum2",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak3",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum3",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacBreak4",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB Vacuum4",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit1 Up",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit1 Down",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit2 Up",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit2 Down",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit3 Up",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit3 Down",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit4 Up",
                        CommandParameter = 15,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB VacUnit4 Down",
                        CommandParameter = 16,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "RB HomeReturn",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 5";
              //  return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 6)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                   new ManualOperationItemModel()
                {
                    Content = "AbnormalTray Stopper Up",
                    CommandParameter = 1,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "AbnormalTray Stopper Down",
                    CommandParameter = 2,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "AbnormalTray Stopper Up",
                    CommandParameter = 3,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "AbnormalTray Stopper Down",
                    CommandParameter = 4,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                new ManualOperationItemModel()
                {
                    Content = "Button5",
                    CommandParameter = 5,
                    IsEnabled = true,
                    IsVisible = true,
                    IsChecked = false
                },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 6";
               /// return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 7)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                  new ManualOperationItemModel()
                    {
                        Content = "TempTable VacBreak1",
                        CommandParameter = 1,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "TempTable Vacuum1",
                        CommandParameter = 2,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "TempTable VacBreak2",
                        CommandParameter = 3,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "TempTable Vacuum2",
                        CommandParameter = 4,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "TempTable VacBreak3",
                        CommandParameter = 5,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "TempTable Vacuum3",
                        CommandParameter = 6,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "TempTable VacBreak4",
                        CommandParameter = 7,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "TempTable Vacuum4",
                        CommandParameter = 8,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter = 9,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter = 10,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter = 11,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter = 12,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter = 13,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter = 14,
                        IsEnabled = true,
                        IsVisible = true,
                        IsChecked = false
                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 7";
              //  return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 8)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 8";
             //   return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 9)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 9";
           //     return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 10)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 10";
            //    return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 11)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 11";
            //    return CurrentPage.SelectedButtons[0];
            }

            else if (pageNo == 12)
            {

                CurrentPage.SelectedButtons = new List<ManualOperationItemModel>()
                {
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button2",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button3",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button4",
                        CommandParameter=4,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button5",
                        CommandParameter=5,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button6",
                        CommandParameter=6,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button7",
                        CommandParameter=7,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button8",
                        CommandParameter=8,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button9",
                        CommandParameter=9,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button10",
                        CommandParameter=10,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button11",
                        CommandParameter=11,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button12",
                        CommandParameter=12,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button13",
                        CommandParameter=13,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button14",
                        CommandParameter=14,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                     new ManualOperationItemModel()
                    {
                        Content = "Button15",
                        CommandParameter=15,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button16",
                        CommandParameter=16,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                          },
                    new ManualOperationItemModel()
                    {
                        Content = "Button17",
                        CommandParameter=17,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button18",
                        CommandParameter=18,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button19",
                        CommandParameter=19,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                   new ManualOperationItemModel()
                    {
                        Content = "Button20",
                        CommandParameter=20,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 12";
            //    return CurrentPage.SelectedButtons[0];
            }
            
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