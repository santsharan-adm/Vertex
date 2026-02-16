using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private readonly HttpClient _httpClient;
        private readonly MacMiniTcpClient _tcpClient = new();
        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;
        private readonly IDialogService _dialog;
        private readonly string _appSettingsPath;

        // NEW Dependency
        private readonly IAeLimitService _aeLimitService;

        private bool _isPingBusy;
        private bool _isBusy;

        // Settings Cache for PDCA Tab
        private AeLimitSettings _pdcaSettings;

        public ObservableCollection<string> ProtocolOptions { get; } = new() { "TCP", "HTTP", "HTTPS" };

        // --- EXTERNAL INTERFACE PROPERTIES (Existing) ---
        private string _selectedProtocol;
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set { if (SetProperty(ref _selectedProtocol, value)) { UpdateDefaultPort(value); CommandManager.InvalidateRequerySuggested(); UpdatePreview(); OnPropertyChanged(nameof(IsPortVisible)); } }
        }
        private string _host;
        public string Host { get => _host; set { if (SetProperty(ref _host, value)) { CommandManager.InvalidateRequerySuggested(); UpdatePreview(); } } }
        private int _port;
        public int Port { get => _port; set { if (SetProperty(ref _port, value)) { UpdatePreview(); } } }
        public bool IsPortVisible => (SelectedProtocol ?? "").ToUpper() == "TCP";
        private string _endpoint;
        public string Endpoint { get => _endpoint; set { if (SetProperty(ref _endpoint, value)) { CommandManager.InvalidateRequerySuggested(); UpdatePreview(); } } }
        private string _twoDCodeData;
        public string TwoDCodeData { get => _twoDCodeData; set { if (SetProperty(ref _twoDCodeData, value)) { UpdatePreview(); } } }
        private string _previousStationCode;
        public string PreviousStationCode { get => _previousStationCode; set { if (SetProperty(ref _previousStationCode, value)) { UpdatePreview(); } } }
        private string _currentMachineCode;
        public string CurrentMachineCode { get => _currentMachineCode; set { if (SetProperty(ref _currentMachineCode, value)) { UpdatePreview(); } } }
        private string _requestPreview;
        public string RequestPreview { get => _requestPreview; set { if (SetProperty(ref _requestPreview, value)) { OnPropertyChanged(nameof(HasRequestPreview)); } } }
        public bool HasRequestPreview => !string.IsNullOrWhiteSpace(_requestPreview);
        private string _finalQueryPreview;
        public string FinalQueryPreview { get => _finalQueryPreview; set { if (SetProperty(ref _finalQueryPreview, value)) { OnPropertyChanged(nameof(HasFinalQueryPreview)); } } }
        public bool HasFinalQueryPreview => !string.IsNullOrWhiteSpace(_finalQueryPreview);
        private string _resultText = "Ready to test.";
        public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }
        private string _pingResult = "Ping not executed.";
        public string PingResult { get => _pingResult; set => SetProperty(ref _pingResult, value); }
        public bool IsBusy { get => _isBusy; set { if (SetProperty(ref _isBusy, value)) { CommandManager.InvalidateRequerySuggested(); } } }

        // --- PDCA DATA PROPERTIES (NEW) ---
        public string MachineId { get => _pdcaSettings?.MachineId; set { if (_pdcaSettings != null) { _pdcaSettings.MachineId = value; OnPropertyChanged(); } } }
        public string SubmitId { get => _pdcaSettings?.SubmitId; set { if (_pdcaSettings != null) { _pdcaSettings.SubmitId = value; OnPropertyChanged(); } } }
        public string VendorCode { get => _pdcaSettings?.VendorCode; set { if (_pdcaSettings != null) { _pdcaSettings.VendorCode = value; OnPropertyChanged(); } } }
        public string TossingDefault { get => _pdcaSettings?.TossingDefault; set { if (_pdcaSettings != null) { _pdcaSettings.TossingDefault = value; OnPropertyChanged(); } } }
        public string OperatorIdDefault { get => _pdcaSettings?.OperatorIdDefault; set { if (_pdcaSettings != null) { _pdcaSettings.OperatorIdDefault = value; OnPropertyChanged(); } } }
        public string ModeDefault { get => _pdcaSettings?.ModeDefault; set { if (_pdcaSettings != null) { _pdcaSettings.ModeDefault = value; OnPropertyChanged(); } } }
        public string TestSeriesDefault { get => _pdcaSettings?.TestSeriesIdDefault; set { if (_pdcaSettings != null) { _pdcaSettings.TestSeriesIdDefault = value; OnPropertyChanged(); } } }
        public string PriorityDefault { get => _pdcaSettings?.PriorityDefault; set { if (_pdcaSettings != null) { _pdcaSettings.PriorityDefault = value; OnPropertyChanged(); } } }
        public string OnlineFlag { get => _pdcaSettings?.OnlineFlagDefault; set { if (_pdcaSettings != null) { _pdcaSettings.OnlineFlagDefault = value; OnPropertyChanged(); } } }

        public string DutPositionLabel => _pdcaSettings?.Stations?.FirstOrDefault()?.DutPositionLabel;
        public int? Cavity => _pdcaSettings?.Stations?.FirstOrDefault()?.Cavity;

        public double CycleTimeMin
        {
            get => _pdcaSettings?.Stations?.FirstOrDefault()?.CycleTime.Lower ?? 0;
            set { if (_pdcaSettings?.Stations?.FirstOrDefault() != null) { _pdcaSettings.Stations[0].CycleTime.Lower = value; OnPropertyChanged(); } }
        }
        public double CycleTimeMax
        {
            get => _pdcaSettings?.Stations?.FirstOrDefault()?.CycleTime.Upper ?? 0;
            set { if (_pdcaSettings?.Stations?.FirstOrDefault() != null) { _pdcaSettings.Stations[0].CycleTime.Upper = value; OnPropertyChanged(); } }
        }

        // --- Commands ---
        public ICommand TestApiCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand SavePdcaCommand { get; } // NEW
        public ICommand PingTestCommand { get; }
        public ICommand CloseTcpCommand { get; }

        public ApiTestViewModel(
            IAppLogger logger,
            IOptionsMonitor<ExternalSettings> settingsMonitor,
            IAeLimitService aeLimitService, // Inject
            IDialogService dialog) : base(logger)
        {
            _settingsMonitor = settingsMonitor;
            _aeLimitService = aeLimitService;
            _dialog = dialog;
            _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            TestApiCommand = new RelayCommand(async () => await ExecuteTestAsync(), CanExecuteTest);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            SavePdcaCommand = new RelayCommand(async () => await SavePdcaSettingsAsync());
            PingTestCommand = new RelayCommand(async () => await ExecutePingAsync(), CanExecutePing);
            CloseTcpCommand = new RelayCommand(() => { _tcpClient.Disconnect(); ResultText += "\n[TCP] Connection closed by user."; });

            LoadSettingsFromConfig();
            _ = LoadPdcaSettingsAsync(); // Load PDCA Data
            UpdatePreview();
        }

        private async Task LoadPdcaSettingsAsync()
        {
            try
            {
                _pdcaSettings = await _aeLimitService.GetSettingsAsync();
                OnPropertyChanged(string.Empty); // Refresh all bindings
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load PDCA settings: {ex.Message}", Shared.Models.ConfigModels.LogType.Diagnostics);
            }
        }

        private async Task SavePdcaSettingsAsync()
        {
            try
            {
                if (_pdcaSettings != null)
                {
                    // Propagate global cycle time to all stations if needed, or just 0
                    if (_pdcaSettings.Stations != null)
                    {
                        foreach (var st in _pdcaSettings.Stations)
                        {
                            st.CycleTime.Lower = CycleTimeMin;
                            st.CycleTime.Upper = CycleTimeMax;
                        }
                    }

                    await _aeLimitService.SaveSettingsAsync(_pdcaSettings);
                    _dialog.ShowMessage("PDCA Data settings saved successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save PDCA settings: {ex.Message}", Shared.Models.ConfigModels.LogType.Diagnostics);
                _dialog.ShowWarning("Failed to save PDCA settings.");
            }
        }

        // ... (UpdateDefaultPort, LoadSettingsFromConfig, SaveSettings, ExecuteTestAsync, etc. - UNCHANGED) ...

        private void UpdateDefaultPort(string protocol)
        {
            if (string.IsNullOrEmpty(protocol)) return;
            switch (protocol.ToUpper()) { case "HTTP": Port = 80; break; case "HTTPS": Port = 443; break; case "TCP": Port = 5000; break; }
        }

        private void LoadSettingsFromConfig()
        {
            try
            {
                var config = _settingsMonitor.CurrentValue;
                SelectedProtocol = config.Protocol?.ToUpper() ?? "TCP";
                Host = config.MacMiniIpAddress;
                Port = config.Port > 0 ? config.Port : (SelectedProtocol == "HTTPS" ? 443 : 5000);
                Endpoint = config.EndPoint;
                PreviousStationCode = config.PreviousMachineCode;
                CurrentMachineCode = config.AOIMachineCode;
                TwoDCodeData = "TEST_QR_CODE";
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                string ip = Host; int port = 5000;
                if (!string.IsNullOrWhiteSpace(Host)) { var p = Host.Split(':'); ip = p[0]; if (p.Length > 1 && int.TryParse(p[1], out int pt)) port = pt; }
                var json = File.ReadAllText(_appSettingsPath);
                var jsonObj = JObject.Parse(json);
                if (jsonObj["External"] == null) jsonObj["External"] = new JObject();
                var ext = jsonObj["External"];
                ext["Protocol"] = SelectedProtocol; ext["MacMiniIpAddress"] = ip; ext["Port"] = Port; ext["EndPoint"] = Endpoint;
                ext["PreviousMachineCode"] = PreviousStationCode; ext["AOIMachineCode"] = CurrentMachineCode;
                File.WriteAllText(_appSettingsPath, jsonObj.ToString());
                _dialog.ShowMessage("Settings saved.");
            }
            catch (Exception ex) { _dialog.ShowWarning(ex.Message); }
        }
        private bool CanExecuteTest() => !IsBusy && !string.IsNullOrWhiteSpace(SelectedProtocol) && !string.IsNullOrWhiteSpace(Host);
        private async Task ExecuteTestAsync() { if (!CanExecuteTest()) return; IsBusy = true; try { if ((SelectedProtocol ?? "").ToUpper() == "TCP") await ExecuteTcpTestAsync(); else await ExecuteHttpTestAsync(); } catch (Exception ex) { ResultText = ex.Message; } finally { IsBusy = false; } }
        private async Task ExecuteTcpTestAsync()
        {
            string payload = BuildTcpPayload(out _); RequestPreview = $"[TCP] Target: {Host}:{Port}\nPayload: {payload}";
            try { if (!_tcpClient.IsConnected) { ResultText = "Connecting..."; await _tcpClient.ConnectAsync(Host, Port); ResultText += " Connected.\n"; } ResultText += $"Sending: {payload}\nWaiting..."; var resp = await _tcpClient.SendAndReceiveAsync(payload); ResultText += $"\n\nReceived:\n{resp}"; } catch (Exception ex) { _tcpClient.Disconnect(); throw new Exception(ex.Message); }
        }
        private async Task ExecuteHttpTestAsync()
        {
            try { var uri = BuildRequestUri(out _); RequestPreview = uri.ToString(); ResultText = $"GET {uri}\nWaiting..."; var resp = await _httpClient.GetAsync(uri); var b = await resp.Content.ReadAsStringAsync(); ResultText = $"Status: {resp.StatusCode}\n\n{b}"; } catch (Exception ex) { ResultText = ex.Message; }
        }
        private string BuildTcpPayload(out string fq) { var p = GetCommonQueryParameters(); string ep = (Endpoint ?? "").Trim().TrimStart('/'); string pl = $"{ep}@{string.Join("&", p)}"; fq = pl; return pl; }
        private Uri BuildRequestUri(out string fq) { var h = (Host ?? "").Trim(); var s = (SelectedProtocol ?? "http").ToLower(); if (s != "http" && s != "https") s = "http"; if (h.StartsWith("http://")) h = h.Substring(7); if (h.StartsWith("https://")) h = h.Substring(8); var b = new UriBuilder { Scheme = s, Host = h, Port = Port }; var ep = (Endpoint ?? "").Trim(); if (!ep.StartsWith("/")) ep = "/" + ep; b.Path = ep; if (GetCommonQueryParameters().Count > 0) b.Query = string.Join("&", GetCommonQueryParameters()); fq = b.Uri.ToString(); return b.Uri; }
        private List<string> GetCommonQueryParameters() { var l = new List<string> { "c=QUERY_4_SFC", "subcmd=carrier_query" }; if (!string.IsNullOrWhiteSpace(TwoDCodeData)) l.Add($"carrier_sn={Uri.EscapeDataString(TwoDCodeData)}"); if (!string.IsNullOrWhiteSpace(PreviousStationCode)) l.Add($"station_code={Uri.EscapeDataString(PreviousStationCode)}"); if (!string.IsNullOrWhiteSpace(CurrentMachineCode)) l.Add($"station_id={Uri.EscapeDataString(CurrentMachineCode)}"); return l; }
        private void UpdatePreview() { try { if ((SelectedProtocol ?? "").ToUpper() == "TCP") { BuildTcpPayload(out var q); RequestPreview = $"TCP://{Host}:{Port} -> Send: {q}"; } else { var u = BuildRequestUri(out var q); RequestPreview = u.ToString(); } } catch { RequestPreview = "Invalid"; } }
        private bool CanExecutePing() => !_isPingBusy && !string.IsNullOrWhiteSpace(Host);
        private async Task ExecutePingAsync() { try { _isPingBusy = true; string ip = (Host ?? "").Split(':')[0]; if (string.IsNullOrWhiteSpace(ip)) return; using var p = new Ping(); var r = await p.SendPingAsync(ip, 2000); PingResult = r.Status == IPStatus.Success ? "Success" : "Failed"; } catch (Exception ex) { PingResult = ex.Message; } finally { _isPingBusy = false; } }
        public void Dispose() { _httpClient?.Dispose(); _tcpClient?.Dispose(); }
    }
}