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

        private List<String> _selectedPage;
        public List<String> SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value);
        }




        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }

        private BendingManualOperationModel _manualOperationModel = new();

        public BendingManualOperationModel ManualOperationModel
        {
            get => _manualOperationModel;
            set => SetProperty(ref _manualOperationModel, value);
        }
        public ManualOperationPageModel CurrentPage { get; set; }

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

            ManualPageList = new List<string>() { "Page 1","Page 2","Page 3", "Page 4", "Page 5", "Page 6" , "Page 7", "Page 8", "Page 9" , "Page 10", "Page 11", "Page 12" };
           
         }

        private ManualOperationItemModel LoadPageData(int pageNo)
        {
            CurrentPage = new ManualOperationPageModel();
            if (pageNo == 1)
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
                CurrentPage.Title = "Page 1";               
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 2)
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
                CurrentPage.Title = "Page 2";
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 3)
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
                CurrentPage.Title = "Page 3";
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 4)
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
                CurrentPage.Title = "Page 4";
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 5)
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
                CurrentPage.Title = "Page 5";
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 6)
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
                CurrentPage.Title = "Page 6";
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 7)
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
                CurrentPage.Title = "Page 7";
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 8)
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
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 9)
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
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 10)
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
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 11)
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
                return CurrentPage.SelectedButtons[0];
            }

            if (pageNo == 12)
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
                return CurrentPage.SelectedButtons[0];
            }

            return new ManualOperationItemModel();
        }

        int _currentPage;
        private void ExecuteNext()
        {
            // Next Screen Navigation
            _currentPage++;
                LoadPageData(_currentPage);
            var sanjeev = SelectedPage;
        }

        private void ExecutePrevious()
        {
            // Previous Screen Navigation
            _currentPage--;
            LoadPageData(_currentPage);
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