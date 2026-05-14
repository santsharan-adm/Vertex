using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Common.CommonExtensions
{
    public class SafePollerEx : SafePoller
    {
        protected readonly IAppLogger _logger;
        //(CoreClient coreClient, Action<List<IoValue>> asyncAction, int intervalMs = 1000) : base(asyncAction, intervalMs)
        public SafePollerEx(CoreClient coreClient,TimeSpan interval, Func<Dictionary<int, object>, Task> asyncAction,IAppLogger logger, Action<Exception> onError = null):base(interval, asyncAction, onError)
        {
            _coreClient = coreClient;
            _logger = logger;
        }
        private int _liveDataRunning;
        private readonly CoreClient _coreClient;
        internal override async Task LiveDataTickAsync()
        {
            if (Interlocked.Exchange(ref _liveDataRunning, 1) == 1)
                return;

            try
            {
                if (!_coreClient.isConnected)
                    return;

                var data = await _coreClient.GetIoValuesAsync(5);
                if (data != null && data.Count > 0)
                {
                    await _asyncAction(data);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Bending1Monitor] LiveDataTickAsync error: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                Interlocked.Exchange(ref _liveDataRunning, 0);
            }


        }

    }
}
