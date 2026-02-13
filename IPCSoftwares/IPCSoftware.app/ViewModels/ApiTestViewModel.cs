using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels; // Using ExternalSettings
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private readonly MacMiniTcpClient _tcpClient = new();
        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;
        private readonly IDialogService _dialog; // For Save confirmation
        private readonly string _appSettingsPath;

        private bool _isPingBusy;
        private bool _isBusy;

        public ObservableCollection<string> ProtocolOptions { get; } = new()
        {
            "TCP",
            "HTTP",
            "HTTPS"
        };

        // --- UI Properties ---

        private string _selectedProtocol;
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set { if (SetProperty(ref _selectedProtocol, value)) { CommandManager.InvalidateRequerySuggested(); UpdatePreview(); } }
        }

        private string _host;
        public string Host
        {
            get => _host;
            set { if (SetProperty(ref _host, value)) { CommandManager.InvalidateRequerySuggested(); UpdatePreview(); } }
        }

        private string _endpoint;
        public string Endpoint
        {
            get => _endpoint;
            set { if (SetProperty(ref _endpoint, value)) { CommandManager.InvalidateRequerySuggested(); UpdatePreview(); } }
        }

        private string _twoDCodeData;
        public string TwoDCodeData
        {
            get => _twoDCodeData;
            set { if (SetProperty(ref _twoDCodeData, value)) { UpdatePreview(); } }
        }

        private string _previousStationCode;
        public string PreviousStationCode
        {
            get => _previousStationCode;
            set { if (SetProperty(ref _previousStationCode, value)) { UpdatePreview(); } }
        }

        private string _currentMachineCode;
        public string CurrentMachineCode
        {
            get => _currentMachineCode;
            set { if (SetProperty(ref _currentMachineCode, value)) { UpdatePreview(); } }
        }

        // --- Preview & Results ---

        private string _requestPreview;
        public string RequestPreview
        {
            get => _requestPreview;
            set { if (SetProperty(ref _requestPreview, value)) { OnPropertyChanged(nameof(HasRequestPreview)); } }
        }

        public bool HasRequestPreview => !string.IsNullOrWhiteSpace(_requestPreview);

        private string _finalQueryPreview;
        public string FinalQueryPreview
        {
            get => _finalQueryPreview;
            set { if (SetProperty(ref _finalQueryPreview, value)) { OnPropertyChanged(nameof(HasFinalQueryPreview)); } }
        }

        public bool HasFinalQueryPreview => !string.IsNullOrWhiteSpace(_finalQueryPreview);

        private string _resultText = "Ready to test.";
        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        private string _pingResult = "Ping not executed.";
        public string PingResult
        {
            get => _pingResult;
            set => SetProperty(ref _pingResult, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (SetProperty(ref _isBusy, value)) { CommandManager.InvalidateRequerySuggested(); } }
        }

        // --- Commands ---

        public ICommand TestApiCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand PingTestCommand { get; }
        public ICommand CloseTcpCommand { get; }

        public ApiTestViewModel(
            IAppLogger logger,
            IOptionsMonitor<ExternalSettings> settingsMonitor,
            IDialogService dialog) : base(logger)
        {
            _settingsMonitor = settingsMonitor;
            _dialog = dialog;
            _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            TestApiCommand = new RelayCommand(async () => await ExecuteTestAsync(), CanExecuteTest);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            PingTestCommand = new RelayCommand(async () => await ExecutePingAsync(), CanExecutePing);

            CloseTcpCommand = new RelayCommand(() =>
            {
                _tcpClient.Disconnect();
                ResultText += "\n[TCP] Connection closed by user.";
            });

            // Load initial values from ExternalSettings
            LoadSettingsFromConfig();
            UpdatePreview();
        }

        private void LoadSettingsFromConfig()
        {
            try
            {
                var config = _settingsMonitor.CurrentValue;

                // Map ExternalSettings to UI
                SelectedProtocol = config.Protocol?.ToUpper() ?? "TCP";
                Endpoint = config.EndPoint;
                PreviousStationCode = config.PreviousMachineCode;
                CurrentMachineCode = config.AOIMachineCode;

                // Handle Host (Combine IP and Port for TCP, or just IP for HTTP)
                if (SelectedProtocol == "TCP")
                {
                    Host = $"{config.MacMiniIpAddress}:{config.Port}";
                }
                else
                {
                    Host = config.MacMiniIpAddress;
                }

                // TwoDCodeData is not stored in config, defaulting for test convenience
                TwoDCodeData = "TEST_QR_CODE";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading settings: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void SaveSettings()
        {
            try
            {
                // 1. Parse Host field into IP and Port
                string ip = Host;
                int port = 5000; // Default

                if (!string.IsNullOrWhiteSpace(Host))
                {
                    var parts = Host.Split(':');
                    ip = parts[0];
                    if (parts.Length > 1 && int.TryParse(parts[1], out int p))
                    {
                        port = p;
                    }
                }

                // 2. Read appsettings.json
                var json = File.ReadAllText(_appSettingsPath);
                var jsonObj = JObject.Parse(json);

                // 3. Ensure External section
                if (jsonObj["External"] == null)
                {
                    jsonObj["External"] = new JObject();
                }

                // 4. Update Values
                var ext = jsonObj["External"];
                ext["Protocol"] = SelectedProtocol;
                ext["MacMiniIpAddress"] = ip;
                ext["Port"] = port;
                ext["EndPoint"] = Endpoint;
                ext["PreviousMachineCode"] = PreviousStationCode;
                ext["AOIMachineCode"] = CurrentMachineCode;

                // Keep other existing settings intact (IsMacMiniEnabled, SharedFolderPath, etc.)
                // if they are not bound here.

                // 5. Write back
                File.WriteAllText(_appSettingsPath, jsonObj.ToString());

                _dialog.ShowMessage("External Interface Settings saved to appsettings.json.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save settings: {ex.Message}", LogType.Diagnostics);
                _dialog.ShowWarning("Failed to save settings. Check logs.");
            }
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
            if (!CanExecuteTest()) return;

            IsBusy = true;
            try
            {
                if (SelectedProtocol == "TCP")
                {
                    await ExecuteTcpTestAsync();
                }
                else
                {
                    await ExecuteHttpTestAsync();
                }
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

        private async Task ExecuteTcpTestAsync()
        {
            // Parse IP/Port locally
            string ip = Host;
            int port = 5000;

            if (Host.Contains(':'))
            {
                var parts = Host.Split(':');
                ip = parts[0];
                if (parts.Length > 1) int.TryParse(parts[1], out port);
            }

            string payload = BuildTcpPayload(out _);
            RequestPreview = $"[TCP] Target: {ip}:{port}\nPayload: {payload}";

            try
            {
                if (!_tcpClient.IsConnected)
                {
                    ResultText = $"Connecting to {ip}:{port} ...";
                    await _tcpClient.ConnectAsync(ip, port);
                    ResultText += " Connected.\n";
                }
                else
                {
                    ResultText = "Using existing connection.\n";
                }

                ResultText += $"Sending: {payload}\nWaiting for response...";
                var response = await _tcpClient.SendAndReceiveAsync(payload);
                ResultText += $"\n\nReceived:\n{response}";
            }
            catch (Exception ex)
            {
                _tcpClient.Disconnect();
                throw new Exception($"TCP Error: {ex.Message}");
            }
        }

        private async Task ExecuteHttpTestAsync()
        {
            var uri = BuildRequestUri(out _);
            RequestPreview = uri.ToString();

            ResultText = $"GET {uri}\nWaiting...";
            var response = await _httpClient.GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
            sb.AppendLine();
            sb.Append(body);
            ResultText = sb.ToString();
        }

        // --- Helper: Shared Parameter Logic ---
        private List<string> GetCommonQueryParameters()
        {
            var queryParts = new List<string>
            {
                "c=QUERY_4_SFC",
                "subcmd=carrier_query"
            };

            if (!string.IsNullOrWhiteSpace(TwoDCodeData))
                queryParts.Add($"carrier_sn={Uri.EscapeDataString(TwoDCodeData)}");

            if (!string.IsNullOrWhiteSpace(PreviousStationCode))
                queryParts.Add($"station_code={Uri.EscapeDataString(PreviousStationCode)}");

            if (!string.IsNullOrWhiteSpace(CurrentMachineCode))
                queryParts.Add($"station_id={Uri.EscapeDataString(CurrentMachineCode)}");

            return queryParts;
        }

        private string BuildTcpPayload(out string finalQuery)
        {
            var paramsList = GetCommonQueryParameters();
            string paramString = string.Join("&", paramsList);

            // Format: Endpoint@Params (e.g. sfc_post@c=QUERY...)
            string endpoint = (Endpoint ?? "").Trim().TrimStart('/');
            string payload = $"{endpoint}@{paramString}";

            finalQuery = payload;
            return payload;
        }

        private Uri BuildRequestUri(out string finalQuery)
        {
            var hostValue = Host.Trim();
            // Basic cleanup if user pasted protocol
            if (hostValue.StartsWith("http")) hostValue = hostValue.Substring(hostValue.IndexOf("://") + 3);

            var builder = new UriBuilder
            {
                Scheme = SelectedProtocol.ToLower() == "http" ? "http" : "https",
                Host = hostValue,
                Port = -1
            };

            if (hostValue.Contains(':'))
            {
                var split = hostValue.Split(':');
                builder.Host = split[0];
                if (split.Length > 1 && int.TryParse(split[1], out var port)) builder.Port = port;
            }

            builder.Path = Endpoint;
            var queryParts = GetCommonQueryParameters();
            if (queryParts.Count > 0) builder.Query = string.Join("&", queryParts);

            finalQuery = builder.Uri.ToString();
            return builder.Uri;
        }

        private void UpdatePreview()
        {
            try
            {
                if (SelectedProtocol == "TCP")
                {
                    BuildTcpPayload(out var fq);
                    RequestPreview = $"TCP://{Host} -> Send: {fq}";
                    FinalQueryPreview = fq;
                }
                else
                {
                    var uri = BuildRequestUri(out var fq);
                    RequestPreview = uri.ToString();
                    FinalQueryPreview = fq;
                }
            }
            catch
            {
                RequestPreview = "Invalid Configuration";
            }
        }

        private bool CanExecutePing() => !_isPingBusy && !string.IsNullOrWhiteSpace(Host);

        private async Task ExecutePingAsync()
        {
            string ip = Host.Split(':')[0];
            if (string.IsNullOrWhiteSpace(ip)) return;

            try
            {
                _isPingBusy = true; CommandManager.InvalidateRequerySuggested();
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 2000);
                PingResult = reply.Status == IPStatus.Success ? $"Success ({reply.RoundtripTime}ms)" : $"Failed: {reply.Status}";
            }
            catch (Exception ex) { PingResult = $"Error: {ex.Message}"; }
            finally { _isPingBusy = false; CommandManager.InvalidateRequerySuggested(); }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}