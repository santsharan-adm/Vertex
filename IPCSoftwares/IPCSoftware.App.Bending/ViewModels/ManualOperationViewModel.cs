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
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class ManualOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;

        private SafePollerEx _manualOperationPoller;
        private bool _disposed;

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

            
        }

        ManualOperationItemModel LoadPageData(int pageNo)
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
                        Content = "Button1",
                        CommandParameter=2,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=3,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                    new ManualOperationItemModel()
                    {
                        Content = "Button1",
                        CommandParameter=1,
                        IsEnabled=true,
                        IsVisible=true,
                        IsChecked=false

                    },
                };
                CurrentPage.Title = "Page 1";
                return CurrentPage.SelectedButtons[0];


            }
        }
        int _currentPage;
        private void ExecuteNext()
        {
            // Next Screen Navigation
            _currentPage++;
                LoadPageData(_currentPage);
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