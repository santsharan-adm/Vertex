using IPCSoftware.App.Bending.Models;
using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class Bending3MonitorViewModel : BaseViewModel, IDisposable
    {
        // ----------------------------------------------------------------
        // DI Services
        // ----------------------------------------------------------------

        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;

        // ----------------------------------------------------------------
        // Navigation
        // ----------------------------------------------------------------

        private void ExecuteNext()
        {
            _navigationService.NavigateToDashboard2();
        }

        // ----------------------------------------------------------------
        // Pollers
        // ----------------------------------------------------------------

        // RequestId = 28 — Bending1 Monitor live data
        private SafePollerEx _liveDataPoller;

        private bool _disposed;

        // ----------------------------------------------------------------
        // Commands
        // ----------------------------------------------------------------

        public ICommand NextCommand { get; }

        // ----------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------

        private BendingMonitorModel _bendingModel = new();
        public BendingMonitorModel BendingModel
        {
            get => _bendingModel;
            set => SetProperty(ref _bendingModel, value);
        }

        // ----------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------

        public Bending3MonitorViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            NextCommand = new RelayCommand(ExecuteNext);
        }

        // ----------------------------------------------------------------
        // Initialize  (called from code-behind after DataContext is set)
        // ----------------------------------------------------------------

        public void Initialize()
        {
            // RequestId = 28 — Bending1 Monitor live data
            _liveDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateBending3MonitorFromService,
                _logger,
                ex => _logger.LogError($"[Bending3Monitor] Poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 28);

            _liveDataPoller.Start();
        }



        // ----------------------------------------------------------------
        // RequestId = 28 — Bending1 Monitor data update
        // ----------------------------------------------------------------

        private async Task UpdateBending3MonitorFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data == null)
                    return;

                if (data.TryGetValue(28, out object modelObj))
                {
                    var model = Deserialize<BendingMonitorModel>(modelObj);
                    if (model != null)
                    {
                        BendingModel = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Bending2Monitor] UpdateBending2MonitorFromService error: {ex.Message}", LogType.Diagnostics);
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

        // ----------------------------------------------------------------
        // Dispose
        // ----------------------------------------------------------------

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _liveDataPoller?.Stop();
            _liveDataPoller?.Dispose();
        }
    }
}