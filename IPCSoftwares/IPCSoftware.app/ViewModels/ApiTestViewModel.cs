using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class ApiTestViewModel : BaseViewModel, IDisposable
    {
        private readonly HttpClient _httpClient = new();

        public ObservableCollection<string> ProtocolOptions { get; } = new()
        {
            "https",
            "http"
        };

        private string _selectedProtocol = "https";
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set
            {
                if (SetProperty(ref _selectedProtocol, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                    UpdatePreview();
                }
            }
        }

        private string _host = "localhost:5000";
        public string Host
        {
            get => _host;
            set
            {
                if (SetProperty(ref _host, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                    UpdatePreview();
                }
            }
        }

        private readonly string[] _baseQueryParts = new[]
        {
            "c=QUERY_4_SFC",
            "subcmd=carrier_query"
        };

        private string _endpoint = "sfc_post";
        public string Endpoint
        {
            get => _endpoint;
            set
            {
                if (SetProperty(ref _endpoint, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                    UpdatePreview();
                }
            }
        }

        private string _twoDCodeData;
        public string TwoDCodeData
        {
            get => _twoDCodeData;
            set
            {
                if (SetProperty(ref _twoDCodeData, value))
                {
                    UpdatePreview();
                }
            }
        }

        private string _previousStationCode;
        public string PreviousStationCode
        {
            get => _previousStationCode;
            set
            {
                if (SetProperty(ref _previousStationCode, value))
                {
                    UpdatePreview();
                }
            }
        }

        private string _currentMachineCode;
        public string CurrentMachineCode
        {
            get => _currentMachineCode;
            set
            {
                if (SetProperty(ref _currentMachineCode, value))
                {
                    UpdatePreview();
                }
            }
        }

        private string _requestPreview;
        public string RequestPreview
        {
            get => _requestPreview;
            set => SetProperty(ref _requestPreview, value);
        }

        private string _resultText = "Ready to test an endpoint.";
        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand TestApiCommand { get; }

        public ApiTestViewModel(IAppLogger logger) : base(logger)
        {
            TestApiCommand = new RelayCommand(async () => await ExecuteTestAsync(), CanExecuteTest);
            UpdatePreview();
        }

        private bool CanExecuteTest()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(SelectedProtocol) &&
                   !string.IsNullOrWhiteSpace(Host) &&
                   !string.IsNullOrWhiteSpace(Endpoint);
        }

        private async Task ExecuteTestAsync()
        {
            if (!CanExecuteTest())
            {
                ResultText = "Provide protocol, host, and endpoint before testing.";
                return;
            }

            Uri requestUri;
            try
            {
                requestUri = BuildRequestUri();
            }
            catch (Exception ex)
            {
                ResultText = $"Invalid URL: {ex.Message}";
                return;
            }

            RequestPreview = requestUri.ToString();

            try
            {
                IsBusy = true;

                var response = await _httpClient.GetAsync(requestUri);
                var body = await response.Content.ReadAsStringAsync();

                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] GET {requestUri}");
                sb.AppendLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                sb.AppendLine();
                sb.Append(body);

                ResultText = sb.ToString();
            }
            catch (Exception ex)
            {
                ResultText = $"Request failed: {ex.Message}";
                _logger.LogError($"API test failed: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Uri BuildRequestUri()
        {
            var hostValue = (Host ?? string.Empty).Trim();
            if (hostValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                hostValue = hostValue.Substring(7);
            }
            else if (hostValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                hostValue = hostValue.Substring(8);
            }

            hostValue = hostValue.Trim().TrimStart('/').TrimEnd('/');
            if (string.IsNullOrWhiteSpace(hostValue))
            {
                throw new InvalidOperationException("Host is required.");
            }

            var protocol = SelectedProtocol?.Trim().ToLowerInvariant() == "http" ? "http" : "https";

            var endpoint = (Endpoint ?? string.Empty).Trim();
            if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Endpoint should not include protocol. Use the Protocol selector instead.");
            }

            var slashIndex = hostValue.IndexOf('/');
            string hostOnly = hostValue;
            string hostPath = string.Empty;
            if (slashIndex >= 0)
            {
                hostOnly = hostValue[..slashIndex];
                hostPath = hostValue[(slashIndex + 1)..];
            }

            if (!string.IsNullOrWhiteSpace(hostPath))
            {
                endpoint = $"/{hostPath.Trim('/')}/{endpoint.TrimStart('/')}";
            }

            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }

            var builder = new UriBuilder
            {
                Scheme = protocol,
                Host = hostOnly,
                Port = -1
            };

            if (hostOnly.Contains(':'))
            {
                var split = hostOnly.Split(':');
                builder.Host = split[0];
                if (split.Length > 1 && int.TryParse(split[1], out var port))
                {
                    builder.Port = port;
                }
            }

            builder.Path = endpoint;
            var queryParts = _baseQueryParts.ToList();
            AppendQueryPart(queryParts, "carrier_sn", TwoDCodeData);
            AppendQueryPart(queryParts, "station_code", PreviousStationCode);
            AppendQueryPart(queryParts, "station_id", CurrentMachineCode);

            builder.Query = string.Join("&", queryParts);
            return builder.Uri;
        }

        private void AppendQueryPart(List<string> parts, string key, string value)
        {
            var part = CreateQueryPart(key, value);
            if (!string.IsNullOrEmpty(part))
            {
                parts.Add(part);
            }
        }

        private static string CreateQueryPart(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var safeValue = value ?? string.Empty;
            return $"{key}={Uri.EscapeDataString(safeValue)}";
        }

        private void UpdatePreview()
        {
            try
            {
                RequestPreview = BuildRequestUri().ToString();
            }
            catch
            {
                RequestPreview = string.Empty;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
