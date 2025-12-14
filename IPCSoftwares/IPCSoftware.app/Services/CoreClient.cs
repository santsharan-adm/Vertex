using IPCSoftware.App.Services.UI;
using IPCSoftware.Shared.Models.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IPCSoftware.App.Services
{
    public class CoreClient
    {
        private readonly UiTcpClient _tcpClient;
        private TaskCompletionSource<string> _responseTcs;

        // Define the AlarmMessage event that the ViewModel subscribes to
        public event Action<AlarmMessage> OnAlarmMessageReceived;

        public CoreClient(UiTcpClient client)
        {
            _tcpClient = client;
            _tcpClient.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(string json)
        {
            JObject jObject = null;

            try
            {
                // 1. Attempt to parse the raw string.
                jObject = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CoreClient] Error parsing incoming JSON: {ex.Message}. Raw Data: '{json.Trim()}'");
                return;
            }

            var keys = jObject.Properties().Select(p => p.Name).ToList();

            // 1. **Check for PUSH Message (Alarm):**
            if (keys.Any(k => k.Equals("AlarmInstance", StringComparison.OrdinalIgnoreCase)) &&
                keys.Any(k => k.Equals("MessageType", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var settings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        Formatting = Formatting.None
                    };

                    var alarmMsg = jObject.ToObject<AlarmMessage>(JsonSerializer.Create(settings));
                    OnAlarmMessageReceived?.Invoke(alarmMsg);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CoreClient] Error converting JSON to AlarmMessage: {ex.Message}");
                }
                return;
            }

            // 2. **Check for REQUEST RESPONSE:**
            if (jObject.ContainsKey("ResponseId"))
            {
                _responseTcs?.TrySetResult(json);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CoreClient] Received unidentified JSON: {json.Trim()}");
            }
        }

        // --- IO METHODS ---

        public async Task<Dictionary<int, object>> GetIoValuesAsync()
        {
            var req = new RequestPackage { RequestId = 5, Parameters = null };
            string jsonResponse = await SendRequestAsync(req);

            if (string.IsNullOrEmpty(jsonResponse)) return new Dictionary<int, object>();

            var res = JsonConvert.DeserializeObject<ResponsePackage>(jsonResponse);
            return ConvertParameters(res.Parameters);
        }

        public async Task<bool> WriteTagAsync(int tagId, object value)
        {
            var parameters = new Dictionary<uint, object> { { (uint)tagId, value } };
            var req = new RequestPackage { RequestId = 6, Parameters = parameters };

            string jsonResponse = await SendRequestAsync(req);

            if (string.IsNullOrEmpty(jsonResponse)) return false;

            var res = JsonConvert.DeserializeObject<ResponsePackage>(jsonResponse);
            return res.Success;
        }

        // --- ALARM METHODS ---

        public async Task<bool> AcknowledgeAlarmAsync(int alarmNo, string userName)
        {
            try
            {
                var parameters = new
                {
                    Action = "Acknowledge",
                    AlarmNo = alarmNo,
                    UserName = userName
                };

                var request = new RequestPackage
                {
                    RequestId = 7,
                    Parameters = parameters
                };

                string jsonResponse = await SendRequestAsync(request);

                if (string.IsNullOrEmpty(jsonResponse)) return false;

                var response = JsonConvert.DeserializeObject<ResponsePackage>(jsonResponse);
                return response.Success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ack Error: {ex.Message}");
                return false;
            }
        }

        // --- HELPER METHODS ---

        private async Task<string> SendRequestAsync(RequestPackage request)
        {
            if (_tcpClient == null || !_tcpClient.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("CoreClient: Cannot send request, client disconnected.");
                return null;
            }

            string json = JsonConvert.SerializeObject(request);
            _responseTcs = new TaskCompletionSource<string>();

            try
            {
                System.Diagnostics.Debug.WriteLine("UI -> Sending: " + json);

                // Using .Send() as per your existing code pattern
                _tcpClient.Send(json);

                var completedTask = await Task.WhenAny(_responseTcs.Task, Task.Delay(5000));

                if (completedTask == _responseTcs.Task)
                {
                    return await _responseTcs.Task;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CoreClient: Request timed out.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CoreClient: Send Error: {ex.Message}");
                return null;
            }
        }

        private Dictionary<int, object> ConvertParameters(object parameters)
        {
            var dict = new Dictionary<int, object>();

            if (parameters is JObject jobj)
            {
                foreach (var kv in jobj)
                    dict[int.Parse(kv.Key)] = kv.Value.ToObject<object>();
            }
            else if (parameters is Dictionary<uint, object> d2)
            {
                foreach (var kv in d2)
                    dict[(int)kv.Key] = kv.Value;
            }

            return dict;
        }
    }
}