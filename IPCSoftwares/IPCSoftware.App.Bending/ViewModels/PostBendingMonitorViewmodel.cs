using IPCSoftware.Shared.Models.Bending.IPCSoftware.App.Bending.Models;
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
    public class PostBendingMonitorViewModel : BaseViewModel, IDisposable
    {
        // ----------------------------------------------------------------
        // DI Services
        // ----------------------------------------------------------------

        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;

        // ----------------------------------------------------------------
        // Navigation
        // ----------------------------------------------------------------

        private void ExecutePrevious()
        {
            _navigationService.NavigateToDashboard3();
        }

        // ----------------------------------------------------------------
        // Pollers
        // ----------------------------------------------------------------

        // RequestId = 29 — Post Bending Monitor live data
        private SafePollerEx _liveDataPoller;

        private bool _disposed;

        // ----------------------------------------------------------------
        // Commands
        // ----------------------------------------------------------------

        public ICommand PreviousCommand { get; }

        // ----------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------

        private PostBendingMonitorModel _postBendingModel = new();
        public PostBendingMonitorModel PostBendingModel
        {
            get => _postBendingModel;
            set => SetProperty(ref _postBendingModel, value);
        }

        // ----------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------

        public PostBendingMonitorViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            PreviousCommand = new RelayCommand(ExecutePrevious);
        }

        // ----------------------------------------------------------------
        // Initialize  (called from code-behind after DataContext is set)
        // ----------------------------------------------------------------

        public void Initialize()
        {
            // RequestId = 29 — Post Bending Monitor live data
            _liveDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdatePostBendingMonitorFromService,
                _logger,
                ex => _logger.LogError($"[PostBendingMonitor] Poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 29);

            _liveDataPoller.Start();
        }



        // ----------------------------------------------------------------
        // RequestId = 29 — Post Bending Monitor data update
        // ----------------------------------------------------------------

        private async Task UpdatePostBendingMonitorFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data == null)
                    return;

                if (data.TryGetValue(29, out object modelObj))
                {
                    var model = Deserialize<PostBendingMonitorModel>(modelObj);
                    if (model != null)
                    {
                        PostBendingModel = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PostBendingMonitor] UpdatePostBendingMonitorFromService error: {ex.Message}", LogType.Diagnostics);
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