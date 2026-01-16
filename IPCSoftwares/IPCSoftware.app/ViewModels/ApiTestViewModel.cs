using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ApiTest;
using LogTypeEnum = IPCSoftware.Shared.Models.ConfigModels.LogType;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class ApiTestViewModel : BaseViewModel, IDisposable
    {
        private readonly HttpClient _httpClient = new();
        private readonly IApiTestSettingsService _settingsService;
        private bool _settingsLoaded;
        private bool _isPingBusy;

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
            set
            {
                if (SetProperty(ref _requestPreview, value))
                {
                    OnPropertyChanged(nameof(HasRequestPreview));
                }
            }
        }

        private string _resultText = "Ready to test an endpoint.";
        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        private string _finalQueryPreview = "Final query not available.";
        public string FinalQueryPreview
        {
            get => _finalQueryPreview;
            set
            {
                if (SetProperty(ref _finalQueryPreview, value))
                {
                    OnPropertyChanged(nameof(HasFinalQueryPreview));
                }
            }
        }

        public bool HasRequestPreview => !string.IsNullOrWhiteSpace(_requestPreview);
        public bool HasFinalQueryPreview => !string.IsNullOrWhiteSpace(_finalQueryPreview);

        private string _pingResult = "Ping not executed.";
        public string PingResult
        {
            get => _pingResult;
            set => SetProperty(ref _pingResult, value);
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
        public ICommand SaveSettingsCommand { get; }
        public ICommand PingTestCommand { get; }

        public ApiTestViewModel(IAppLogger logger, IApiTestSettingsService settingsService) : base(logger)
        {
            _settingsService = settingsService;
            TestApiCommand = new RelayCommand(async () => await ExecuteTestAsync(), CanExecuteTest);
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync(), () => _settingsLoaded);
            PingTestCommand = new RelayCommand(async () => await ExecutePingAsync(), CanExecutePing);
            UpdatePreview();
            _ = LoadPersistedSettingsAsync();
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
                requestUri = BuildRequestUri(out _);
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
                _logger.LogError($"API test failed: {ex.Message}", LogTypeEnum.Diagnostics);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Uri BuildRequestUri(out string finalQuery)
        {
            var hostValue = NormalizeHostValue();
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
            var queryParts = new List<string>();
            var hasDynamicParams = !string.IsNullOrWhiteSpace(TwoDCodeData)
                                   || !string.IsNullOrWhiteSpace(PreviousStationCode)
                                   || !string.IsNullOrWhiteSpace(CurrentMachineCode);

            if (hasDynamicParams)
            {
                queryParts.AddRange(_baseQueryParts);
                AppendQueryPart(queryParts, "carrier_sn", TwoDCodeData);
                AppendQueryPart(queryParts, "station_code", PreviousStationCode);
                AppendQueryPart(queryParts, "station_id", CurrentMachineCode);
            }

            if (queryParts.Count > 0)
            {
                builder.Query = string.Join("&", queryParts);
            }
            else
            {
                builder.Query = null;
            }

            if (queryParts.Count > 0)
            {
                var pathSegment = builder.Path?.TrimStart('/') ?? string.Empty;
                var querySegment = builder.Query?.TrimStart('?') ?? string.Empty;
                finalQuery = string.IsNullOrWhiteSpace(pathSegment)
                    ? querySegment
                    : $"{pathSegment}@{querySegment}";
            }
            else
            {
                finalQuery = string.Empty;
            }

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
                var uri = BuildRequestUri(out var finalQuery);
                RequestPreview = uri.ToString();
                FinalQueryPreview = finalQuery;
            }
            catch
            {
                RequestPreview = string.Empty;
                FinalQueryPreview = string.Empty;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private async Task LoadPersistedSettingsAsync()
        {
            if (_settingsService == null)
            {
                _settingsLoaded = true;
                return;
            }

            try
            {
                var stored = await _settingsService.LoadAsync().ConfigureAwait(false);

                SelectedProtocol = stored.Protocol ?? _selectedProtocol;
                Host = stored.Host ?? _host;
                Endpoint = stored.Endpoint ?? _endpoint;
                TwoDCodeData = stored.TwoDCodeData ?? string.Empty;
                PreviousStationCode = stored.PreviousStationCode ?? string.Empty;
                CurrentMachineCode = stored.CurrentMachineCode ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[API TEST] Failed to load saved settings: {ex.Message}", LogTypeEnum.Diagnostics);
            }
            finally
            {
                _settingsLoaded = true;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task SaveSettingsAsync()
        {
            if (_settingsService == null)
            {
                ResultText = "Settings service not available.";
                return;
            }

            try
            {
                var snapshot = new ApiTestSettings
                {
                    Protocol = SelectedProtocol,
                    Host = Host,
                    Endpoint = Endpoint,
                    TwoDCodeData = TwoDCodeData,
                    PreviousStationCode = PreviousStationCode,
                    CurrentMachineCode = CurrentMachineCode
                };

                await _settingsService.SaveAsync(snapshot).ConfigureAwait(false);
                ResultText = "Settings saved to ApiTestSettings.json.";
            }
            catch (Exception ex)
            {
                ResultText = $"Failed to save settings: {ex.Message}";
                _logger.LogError($"[API TEST] Failed to save settings: {ex.Message}", LogTypeEnum.Diagnostics);
            }
        }

        private bool CanExecutePing()
        {
            return !_isPingBusy && !string.IsNullOrWhiteSpace(Host);
        }

        private async Task ExecutePingAsync()
        {
            var hostOnly = ExtractHostName();
            if (string.IsNullOrWhiteSpace(hostOnly))
            {
                PingResult = "Enter a valid host to ping.";
                return;
            }

            try
            {
                _isPingBusy = true;
                CommandManager.InvalidateRequerySuggested();

                using var ping = new Ping();
                var reply = await ping.SendPingAsync(hostOnly, 2000);

                if (reply.Status == IPStatus.Success)
                {
                    PingResult = $"Ping to {hostOnly} succeeded in {reply.RoundtripTime} ms (IP {reply.Address}).";
                }
                else
                {
                    PingResult = $"Ping to {hostOnly} failed: {reply.Status}.";
                }
            }
            catch (Exception ex)
            {
                PingResult = $"Ping error: {ex.Message}";
                _logger.LogError($"[API TEST] Ping failed: {ex.Message}", LogTypeEnum.Diagnostics);
            }
            finally
            {
                _isPingBusy = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string NormalizeHostValue()
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

            return hostValue.Trim().TrimStart('/').TrimEnd('/');
        }

        private string ExtractHostName()
        {
            var hostValue = NormalizeHostValue();
            if (string.IsNullOrWhiteSpace(hostValue))
            {
                return null;
            }

            var slashIndex = hostValue.IndexOf('/');
            if (slashIndex >= 0)
            {
                hostValue = hostValue[..slashIndex];
            }

            return hostValue;
        }
    }
}
