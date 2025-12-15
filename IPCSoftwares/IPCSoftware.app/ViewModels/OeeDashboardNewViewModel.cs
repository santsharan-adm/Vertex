/*using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Shared.Models.Messaging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IPCSoftware.App.ViewModels
{
    public class OeeDashboardNewViewModel : INotifyPropertyChanged
    {
        private readonly UiTcpClient _tcpClient;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // ================================================================
        // RAW PLC VALUES (from CoreService)
        // ================================================================
        private Dictionary<int, object>? _rawValues;
        public Dictionary<int, object>? RawValues
        {
            get => _rawValues;
            set
            {
                if (value != _rawValues)
                {
                    _rawValues = value;
                    OnPropertyChanged();
                }
            }
        }

        // ================================================================
        // FORMATTED UI VALUES
        // ================================================================
        private string? _availability;
        public string? Availability
        {
            get => _availability;
            set
            {
                if (value != _availability)
                {
                    _availability = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _performance;
        public string? Performance
        {
            get => _performance;
            set
            {
                if (value != _performance)
                {
                    _performance = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _quality;
        public string? Quality
        {
            get => _quality;
            set
            {
                if (value != _quality)
                {
                    _quality = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _oee;
        public string? Oee
        {
            get => _oee;
            set
            {
                if (value != _oee)
                {
                    _oee = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _operatingTime;
        public string? OperatingTime
        {
            get => _operatingTime;
            set
            {
                if (value != _operatingTime)
                {
                    _operatingTime = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _downtime;
        public string? Downtime
        {
            get => _downtime;
            set
            {
                if (value != _downtime)
                {
                    _downtime = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _okParts;
        public string? OkParts
        {
            get => _okParts;
            set
            {
                if (value != _okParts)
                {
                    _okParts = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _ngParts;
        public string? NgParts
        {
            get => _ngParts;
            set
            {
                if (value != _ngParts)
                {
                    _ngParts = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _status;
        public string? Status
        {
            get => _status;
            set
            {
                if (value != _status)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        
        // ================================================================
        // 3. TCP CLIENT BINDING
        // ================================================================
        public OeeDashboardNewViewModel(UiTcpClient tcpClient)
        {
            _tcpClient = tcpClient; 
            App.ResponseReceived += OnResponseReceived;
            StartPolling();
        }

        private void RequestRawValues()
        {
            try
            {
                _tcpClient.Send("{\"RequestId\":4}\n");
            }
            catch
            {
                // Optional: log or ignore errors
            }
        }

        private async void StartPolling()
        {
            while (true)
            {
                RequestRawValues();
                await Task.Delay(1000);
            }
        }

        //adding handler
        private void OnResponseReceived(ResponsePackage response)
        {
            if (response.ResponseId == 4)
            {
                RawValues = response.Parameters;

                UpdateDashboardFromRaw(response.Parameters);
            }
        }

        


        // ================================================================
        // UI MAPPING
        // ================================================================
        private void UpdateDashboardFromRaw(Dictionary<int, object> values)
        {
            if (values == null || values.Count == 0)
                return;

            Availability = GetValue(values, 40001);
            Performance = GetValue(values, 40002);
            Quality = GetValue(values, 40003);
            Oee = GetValue(values, 40004);
            OperatingTime = GetValue(values, 40005);
            Downtime = GetValue(values, 40006);
            OkParts = GetValue(values, 40007);
            NgParts = GetValue(values, 40009);
            Status = GetValue(values, 40008);
        }

        private string GetValue(Dictionary<uint, object> values, uint key)
        {
            if (values.TryGetValue(key, out var val))
                return val?.ToString() ?? "-";

            return "-";
        }

        
        
    }
}
*/