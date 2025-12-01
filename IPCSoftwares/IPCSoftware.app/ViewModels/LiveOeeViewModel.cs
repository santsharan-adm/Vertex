using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.Messaging;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IPCSoftware.App.ViewModels
{
    public class LiveOeeViewModel : BaseViewModel
    {
        

        private string _rawMessage;
        public string RawMessage
        {
            get => _rawMessage;
            set { _rawMessage = value; OnPropertyChanged(); }
        }

        private UiTcpClient _client;

        public LiveOeeViewModel()
        {
            _client = new UiTcpClient();

            _client.DataReceived += (msg) =>
            {
                RawMessage = msg; // keep raw JSON for debugging

                try
                {
                    var response = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponsePackage>(msg);
                    if (response?.Parameters != null)
                    {
                        // Example: Only Parameter[5] = OperatingMinutes
                        if (response.Parameters.TryGetValue(5, out var val))
                        {
                            OperatingMinutes = val.ToString();
                        }
                    }
                }
                catch
                {
                    // ignore malformed packet
                }
            };


            _ = _client.StartAsync("127.0.0.1", 5050);

            StartPolling();
        }

        private string _operatingMinutes;
        public string OperatingMinutes
        {
            get => _operatingMinutes;
            set { _operatingMinutes = value; OnPropertyChanged(); }
        }


        private async void StartPolling()
        {
            while (true)
            {
                _client.Send("{\"RequestId\":4}\n");
                await Task.Delay(1000);
            }
        }

    }
}
