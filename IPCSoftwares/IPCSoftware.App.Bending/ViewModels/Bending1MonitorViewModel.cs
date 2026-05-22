using IPCSoftware.Shared.Models.Bending.IPCSoftware.App.Bending.Models;
using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
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
    public class Bending1MonitorViewModel : BaseViewModel, IDisposable
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
            _navigationService.NavigateToBending2Monitor();
        }

        // ----------------------------------------------------------------
        // Pollers
        // ----------------------------------------------------------------

        // RequestId = 26 — Bending1 Monitor live data
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

        public Bending1MonitorViewModel(
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
            // RequestId = 26 — Bending1 Monitor live data
            _liveDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateBending1MonitorFromService,
                _logger,
                ex => _logger.LogError($"[Bending1Monitor] Poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 26);

            _liveDataPoller.Start();
        }



        // ----------------------------------------------------------------
        // RequestId = 26 — Bending1 Monitor data update
        // ----------------------------------------------------------------

        private async Task UpdateBending1MonitorFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data == null)
                    return;

                if (data.TryGetValue(26, out object modelObj))
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
                _logger.LogError($"[Bending1Monitor] UpdateBending1MonitorFromService error: {ex.Message}", LogType.Diagnostics);
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