using System;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace IPCSoftware.Common.CommonExtensions
{
    public class SafePoller : IDisposable
    {
        private readonly DispatcherTimer _timer;
        internal readonly Func<Dictionary<int, object>, Task> _asyncAction; // The work to do
        private readonly Action<Exception> _onError;

        private bool _isBusy;
        private bool _disposed;

        public SafePoller(TimeSpan interval, Func<Dictionary<int, object>, Task> asyncAction, Action<Exception> onError = null)
        {
            _asyncAction = asyncAction;
            _onError = onError;

            _timer = new DispatcherTimer();
            _timer.Interval = interval;
            _timer.Tick += Timer_Tick;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        internal virtual async Task LiveDataTickAsync()
        {
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            // 1. Safety Checks
            if (_disposed || _isBusy) return;

            try
            {
                // 2. Lock
                _isBusy = true;

                // 3. Do the actual work
                await LiveDataTickAsync();
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
            finally
            {
                // 4. Unlock
                _isBusy = false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer.Stop();
            _timer.Tick -= Timer_Tick;
        }
    }
}