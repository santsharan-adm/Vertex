using IPCSoftware.App; // UiTcpClient namespace
using IPCSoftware.App.Services.UI;
using IPCSoftware.Shared.Models.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IPCSoftware.App.Services
{
    public class CoreClient
    {
        private readonly UiTcpClient _tcpClient;
        private TaskCompletionSource<string> _responseTcs;

        public CoreClient(UiTcpClient client)
        {
            _tcpClient = client;
            _tcpClient.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(string json)
        {
            _responseTcs?.TrySetResult(json);
        }

        public async Task<Dictionary<int, object>> GetIoValuesAsync()
        {
            _responseTcs = new TaskCompletionSource<string>();

            var req = new RequestPackage
            {
                RequestId = 5,
                Parameters = null
            };

            string json = JsonConvert.SerializeObject(req);
            System.Diagnostics.Debug.WriteLine("UI → Sending: " + json);
            _tcpClient.Send(json);

            string response = await _responseTcs.Task;
            var res = JsonConvert.DeserializeObject<ResponsePackage>(response);

            return ConvertParameters(res.Parameters);
        }
        //write values




        public async Task<bool> WriteTagAsync(int tagId, object value)
        {
            _responseTcs = new TaskCompletionSource<string>();
            // 
            var parameters = new Dictionary<uint, object>
{
    { (uint)tagId, value }   // key = TagId, value = bool
};
            var req = new RequestPackage
            {
                RequestId = 6,   // new WriteRequest
                Parameters = parameters
            };
            string json = JsonConvert.SerializeObject(req);
            System.Diagnostics.Debug.WriteLine("UI → Sending Write: " + json);
            _tcpClient.Send(json);
            string response = await _responseTcs.Task;
            var res = JsonConvert.DeserializeObject<ResponsePackage>(response);

            
            return res.Success;
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
