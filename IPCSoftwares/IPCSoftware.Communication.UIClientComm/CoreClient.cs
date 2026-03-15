using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IPCSoftware.Common.UIClientComm
{
    public class CoreClient : BaseService
    {
        private readonly UiTcpClient _tcpClient;

        // Use a lock to prevent multiple pages from scrambling the socket
        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);

        // The active TCS for the current request being processed
        private TaskCompletionSource<string> _currentResponseTcs;

        public event Action<AlarmMessage> OnAlarmMessageReceived;
        public bool isConnected => _tcpClient?.IsConnected ?? false;

        public CoreClient(UiTcpClient client, IAppLogger logger) : base(logger)
        {
            _tcpClient = client;
            _tcpClient.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(string json)
        {
            // 1. FAST PARSE: Determine if it's an Alarm or a Response
            JObject jObject;
            try
            {
                jObject = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CoreClient] JSON Parse Error: {ex.Message}", LogType.Diagnostics);
                return;
            }

            // 2. CHECK FOR ALARM (Push Message)
            // Alarms can happen at any time, even while waiting for a request
            if (jObject.ContainsKey("AlarmInstance") && jObject.ContainsKey("MessageType"))
            {
                HandleAlarm(jObject);
                return;
            }

            // 3. CHECK FOR RESPONSE
            // If we are waiting for a response (TCS is not null), complete it.
            if (_currentResponseTcs != null && !_currentResponseTcs.Task.IsCompleted)
            {
                // We assume the next non-alarm message is our response because we are locking requests one by one.
                _currentResponseTcs.TrySetResult(json);
            }
            else
            {
                _logger.LogWarning($"[CoreClient] Unhandled message: {json}", LogType.Diagnostics);
            }
        }

        private void HandleAlarm(JObject jObject)
        {
            try
            {
                var alarmMsg = jObject.ToObject<AlarmMessage>();
                OnAlarmMessageReceived?.Invoke(alarmMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CoreClient] Alarm conversion error: {ex.Message}", LogType.Diagnostics);
            }
        }

        // =================================================================
        // ROBUST SEND METHOD
        // =================================================================
        private async Task<string> SendRequestAsync(RequestPackage request)
        {
            // 1. Connection Check
            if (!isConnected)
            {
                _logger.LogWarning("Cannot send request: Client disconnected.", LogType.Diagnostics);
                return null;
            }

            // 2. Acquire Lock (Wait if another page is already sending)
            await _requestLock.WaitAsync();

            try
            {
                // 3. Setup the TCS for THIS specific request
                _currentResponseTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

                string json = JsonConvert.SerializeObject(request);

                // 4. Send Data
                await _tcpClient.Send(json);

                // 5. Wait for Response with Timeout (e.g., 1 second)
                var timeoutTask = Task.Delay(1000);
                var completedTask = await Task.WhenAny(_currentResponseTcs.Task, timeoutTask);

                if (completedTask == _currentResponseTcs.Task)
                {
                    // Success: We got a result
                    return await _currentResponseTcs.Task;
                }
                else
                {
                    // Fail: Timeout
                    _logger.LogError($"[CoreClient] Request {request.RequestId} Timed Out.", LogType.Diagnostics);
                    try { _currentResponseTcs.TrySetCanceled(); } catch { }
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CoreClient] Send Exception: {ex.Message}", LogType.Diagnostics);
                return null;
            }
            finally
            {
                // 6. CLEANUP & RELEASE LOCK
                _currentResponseTcs = null;
                _requestLock.Release();
            }
        }

        // =================================================================
        // PUBLIC METHODS
        // =================================================================
        public async Task<Dictionary<int, object>> GetIoValuesAsync(int reqId)
        {
            // Retry Logic: Try up to 3 times if we get a null/empty response
            int retryCount = 0;
            while (retryCount < 3)
            {
                try
                {
                    var req = new RequestPackage { RequestId = reqId, Parameters = null };
                    string jsonResponse = await SendRequestAsync(req);

                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        var res = JsonConvert.DeserializeObject<ResponsePackage>(jsonResponse);
                        return ConvertParameters(res.Parameters);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GetIoValues Error (Attempt {retryCount + 1}): {ex.Message}", LogType.Diagnostics);
                }

                retryCount++;
                await Task.Delay(100);
            }

            return new Dictionary<int, object>();
        }

        public async Task<bool> WriteTagAsync(int tagId, object value)
        {
            var parameters = new Dictionary<int, object> { { tagId, value } };
            var req = new RequestPackage { RequestId = 6, Parameters = parameters };

            string jsonResponse = await SendRequestAsync(req);

            if (string.IsNullOrEmpty(jsonResponse)) return false;

            var res = JsonConvert.DeserializeObject<ResponsePackage>(jsonResponse);
            if (!res.Success)
            {
                _logger.LogError($"{res.ErrorMessage} for tagId: {tagId} and value was {value}", LogType.Diagnostics);
            }
            return res.Success;
        }

        public async Task<bool> AcknowledgeAlarmAsync(int alarmNo, string userName)
        {
            try
            {
                var parameters = new { Action = "Acknowledge", AlarmNo = alarmNo, UserName = userName };
                var request = new RequestPackage { RequestId = 7, Parameters = parameters };

                string jsonResponse = await SendRequestAsync(request);

                if (string.IsNullOrEmpty(jsonResponse)) return false;

                var response = JsonConvert.DeserializeObject<ResponsePackage>(jsonResponse);
                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ack Error: {ex.Message}", LogType.Diagnostics);
                return false;
            }
        }

        private Dictionary<int, object> ConvertParameters(object parameters)
        {
            try
            {
                var dict = new Dictionary<int, object>();
                if (parameters is JObject jobj)
                {
                    foreach (var kv in jobj) dict[int.Parse(kv.Key)] = kv.Value.ToObject<object>();
                }
                else if (parameters is Dictionary<int, object> d2)
                {
                    foreach (var kv in d2) dict[(int)kv.Key] = kv.Value;
                }
                return dict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }
    }
}
