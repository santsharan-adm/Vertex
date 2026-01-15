using IPCSoftware.App; // UiTcpClient namespace
using IPCSoftware.App.Services.UI;
using IPCSoftware.Shared.Models.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IPCSoftware.App.Services
{

    /// Handles communication between the UI layer and the Core service using a TCP client.
    /// This class sends JSON-based requests to the core system and waits for JSON responses.
    public class CoreClient
    {
        private readonly UiTcpClient _tcpClient;                                 // TCP client used for sending/receiving data
        private TaskCompletionSource<string> _responseTcs;                       // Task completion source to await responses asynchronously

        /// Constructor — attaches data received handler to the UiTcpClient.
        public CoreClient(UiTcpClient client)
        {
            _tcpClient = client;
            _tcpClient.DataReceived += OnDataReceived;
        }

        /// Called whenever data (JSON string) is received from the core.
        /// Completes the pending TaskCompletionSource with the received data.
        private void OnDataReceived(string json)
        {
            _responseTcs?.TrySetResult(json);
        }

        /// Requests I/O values from the core service.
        /// Sends a request with RequestId = 5 and waits for a JSON response.
        /// Returns a dictionary mapping TagId → Value.
        public async Task<Dictionary<int, object>> GetIoValuesAsync()
        {
            _responseTcs = new TaskCompletionSource<string>();

            // Build the request packet
            var req = new RequestPackage
            {
                RequestId = 5,                                  // Request code for "Get IO Values"
                Parameters = null                               // No parameters needed
            };

            // Serialize and send the JSON request
            string json = JsonConvert.SerializeObject(req);
            System.Diagnostics.Debug.WriteLine("UI → Sending: " + json);
            _tcpClient.Send(json);

            // Wait for the core to respond
            string response = await _responseTcs.Task;
            // Deserialize response
            var res = JsonConvert.DeserializeObject<ResponsePackage>(response);

            // Convert parameters object into a usable dictionary
            return ConvertParameters(res.Parameters);
        }


        // ---------------------- WRITE TAG VALUE ----------------------

        /// Sends a write command to update a tag's value in the core.
        /// RequestId = 6 indicates a "Write" operation.


        public async Task<bool> WriteTagAsync(int tagId, object value)
        {
            _responseTcs = new TaskCompletionSource<string>();
            // Build parameters dictionary for the write command
            var parameters = new Dictionary<uint, object>
{
    { (uint)tagId, value }   // key = TagId, value = bool
};
            var req = new RequestPackage
            {
                RequestId = 6,   // Command for Write operation
                Parameters = parameters
            };

            // Serialize and send the request
            string json = JsonConvert.SerializeObject(req);
            System.Diagnostics.Debug.WriteLine("UI → Sending Write: " + json);
            _tcpClient.Send(json);

            // Wait for response from core
            string response = await _responseTcs.Task;
            // Deserialize response
            var res = JsonConvert.DeserializeObject<ResponsePackage>(response);

            // Return the success flag from the response
            return res.Success;
        }

        // ---------------------- PARAMETER CONVERSION ----------------------

        /// Converts the 'Parameters' field from the response into a Dictionary<int, object>.
        /// Handles both JObject (from JSON) and Dictionary<uint, object> formats.
        

        private Dictionary<int, object> ConvertParameters(object parameters)
        {
            var dict = new Dictionary<int, object>();

            // Handle JObject format (JSON parsed as key-value pairs)
            if (parameters is JObject jobj)
            {
                foreach (var kv in jobj)
                    dict[int.Parse(kv.Key)] = kv.Value.ToObject<object>();
            }
            // Handle already-deserialized Dictionary<uint, object>
            else if (parameters is Dictionary<uint, object> d2)
            {
                foreach (var kv in d2)
                    dict[(int)kv.Key] = kv.Value;
            }

            return dict;
        }
    }
}
