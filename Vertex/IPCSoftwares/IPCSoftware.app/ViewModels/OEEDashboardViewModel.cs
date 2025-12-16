using IPCSoftware.App.Controls;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class OEEDashboardViewModel : BaseViewModel, IDisposable
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;

        private readonly DispatcherTimer _liveDataTimer;
        private readonly DispatcherTimer _uiSyncTimer;

        private readonly string _jsonStatePath;
        private DateTime _lastJsonWriteTime;

        public ICommand ToggleThemeCommand { get; }
        public ICommand ShowImageCommand { get; }
        public ICommand OpenCardDetailCommand { get; }

        private bool _disposed;
        private bool _isDarkTheme = false;
        private Dictionary<int, Action<object>> _tagValueMap;

        private HashSet<int> allowedTagNos = new HashSet<int> { 15, 16, 17, 18, 19, 20 };

        private readonly int[] _visualToStationMap =
        {
            1,2,3,
            4,5,6,
            7,8,9,
            10,11,12
        };

        private string _currentThemePath = "/IPCSoftware.App;component/Styles/LightTheme.xaml";
        public string CurrentThemePath
        {
            get => _currentThemePath;
            set => SetProperty(ref _currentThemePath, value);
        }

        private short okNgStatus;
        public short OkNgStatus
        {
            get => okNgStatus;
            set => SetProperty(ref okNgStatus, value);
        }

        private string _qrCodeText = "Waiting for start...";
        public string QRCodeText
        {
            get => _qrCodeText;
            set => SetProperty(ref _qrCodeText, value);
        }

        private ImageSource _qrCodeImage;
        public ImageSource QrCodeImage
        {
            get => _qrCodeImage;
            set => SetProperty(ref _qrCodeImage, value);
        }

        public ObservableCollection<CameraImageItem> CameraImages { get; } = new();
        public ObservableCollection<WritableTagItem> AllInputs { get; } = new();

        private double _quality;
        public double Quality { get => _quality; set => SetProperty(ref _quality, value); }

        private double _availability;
        public double Availability { get => _availability; set => SetProperty(ref _availability, value); }

        private double _performance;
        public double Performance { get => _performance; set => SetProperty(ref _performance, value); }

        private double _overallOEE;
        public double OverallOEE { get => _overallOEE; set => SetProperty(ref _overallOEE, value); }

        private int _operatingTime;
        public int OperatingTime { get => _operatingTime; set => SetProperty(ref _operatingTime, value); }

        private int _downtime;
        public int Downtime { get => _downtime; set => SetProperty(ref _downtime, value); }

        private int _goodUnits;
        public int GoodUnits { get => _goodUnits; set => SetProperty(ref _goodUnits, value); }

        private int _rejectedUnits;
        public int RejectedUnits { get => _rejectedUnits; set => SetProperty(ref _rejectedUnits, value); }

        private int _inFlow;
        public int InFlow { get => _inFlow; set => SetProperty(ref _inFlow, value); }

        private int _cycleTime;
        public int CycleTime { get => _cycleTime; set => SetProperty(ref _cycleTime, value); }

        public string Remarks { get; set; }

        private List<double> _cycleTrend;
        public List<double> CycleTrend
        {
            get => _cycleTrend;
            set => SetProperty(ref _cycleTrend, value);
        }

        public OEEDashboardViewModel(
            IPLCTagConfigurationService tagService,
            IOptions<CcdSettings> ccdSettng,
            UiTcpClient tcpClient,
            IDialogService dialog)
        {
            try
            {
                _tagService = tagService;
                _coreClient = new CoreClient(tcpClient);
                _dialog = dialog;

                var ccd = ccdSettng.Value;
                _jsonStatePath = Path.Combine(ccd.QrCodeImagePath, ccd.CurrentCycleStateFileName);

                InitializeAsync();
                InitializeCameraGrid();

                ToggleThemeCommand = new RelayCommand(ToggleTheme);
                OpenCardDetailCommand = new RelayCommand<string>(OpenCardDetail);
                ShowImageCommand = new RelayCommand<CameraImageItem>(ShowImage);

                DummyData();

                _liveDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _liveDataTimer.Tick += LiveDataTimerTick;
                _liveDataTimer.Start();

                _uiSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                _uiSyncTimer.Tick += UiSyncTick;
                _uiSyncTimer.Start();

                SyncUiWithJson();
            }
            catch (Exception)
            {
                // Prevent constructor crash
            }
        }

        private void DummyData()
        {
            OverallOEE = Math.Round((Availability * Performance * Quality) / 10000, 2);
            Remarks = "All processes stable.";
            CycleTrend = new List<double> { 2.8, 2.9, 2.7, 3.0, 2.8, 2.9 };
        }

        private async void InitializeAsync()
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                AllInputs.Clear();

                foreach (var tag in allTags.Where(t => t.CanWrite && allowedTagNos.Contains(t.TagNo)))
                {
                    AllInputs.Add(new WritableTagItem(tag));
                }
            }
            catch (Exception)
            {
                // Prevent async crash
            }
        }

        private void InitializeCameraGrid()
        {
            try
            {
                CameraImages.Clear();
                for (int i = 0; i < 12; i++)
                {
                    CameraImages.Add(new CameraImageItem
                    {
                        StationNumber = _visualToStationMap[i],
                        Result = "Unchecked",
                        ImagePath = null,
                        ValX = 0,
                        ValY = 0,
                        ValZ = 0
                    });
                }
            }
            catch (Exception)
            {
                // Prevent UI crash
            }
        }

        private async void LiveDataTimerTick(object sender, EventArgs e)
        {
            if (_disposed) return;

            try
            {
                var resultDict = await _coreClient.GetIoValuesAsync(4);
                if (resultDict != null && resultDict.TryGetValue(4, out object oeeObj))
                {
                    var json = JsonConvert.SerializeObject(oeeObj);
                    var oeeResult = JsonConvert.DeserializeObject<OeeResult>(json);

                    if (oeeResult != null)
                    {
                        Availability = Math.Round(oeeResult.Availability * 100, 1);
                        Performance = Math.Round(oeeResult.Performance * 100, 1);
                        Quality = Math.Round(oeeResult.Quality * 100, 1);
                        OverallOEE = Math.Round(oeeResult.OverallOEE * 100, 1);

                        OperatingTime = oeeResult.OperatingTime;
                        Downtime = oeeResult.Downtime;
                        GoodUnits = oeeResult.OKParts;
                        RejectedUnits = oeeResult.NGParts;
                        CycleTime = oeeResult.CycleTime;
                        InFlow = oeeResult.TotalParts;
                    }
                }
            }
            catch (Exception)
            {
                // Prevent timer crash
            }
        }

        private void UiSyncTick(object sender, EventArgs e)
        {
            if (_disposed) return;
            try { SyncUiWithJson(); } catch { }
        }

        private void SyncUiWithJson()
        {
            try
            {
                if (!File.Exists(_jsonStatePath)) return;

                var currentWriteTime = File.GetLastWriteTime(_jsonStatePath);
                if (currentWriteTime == _lastJsonWriteTime) return;

                var jsonContent = File.ReadAllText(_jsonStatePath);
                var state = JsonConvert.DeserializeObject<CycleStateModel>(jsonContent);
                if (state == null) return;

                QRCodeText = state.BatchId;
                _lastJsonWriteTime = currentWriteTime;
            }
            catch (Exception)
            {
                // Prevent JSON sync crash
            }
        }

        private void ToggleTheme()
        {
            _isDarkTheme = !_isDarkTheme;
            CurrentThemePath = _isDarkTheme
                ? "/IPCSoftware.App;component/Styles/DarkTheme.xaml"
                : "/IPCSoftware.App;component/Styles/LightTheme.xaml";
        }

        private void ShowImage(CameraImageItem img)
        {
            try
            {
                if (img == null) return;
                var window = new FullImageView(img, $"INSPECTION POSITION {img.StationNumber}");
                window.ShowDialog();
            }
            catch (Exception)
            {
                // Prevent dialog crash
            }
        }

        private void OpenCardDetail(string cardType)
        {
            try
            {
                var win = new DashboardDetailWindow();
                win.ShowDialog();
            }
            catch (Exception)
            {
                // Prevent popup crash
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _liveDataTimer.Stop();
            _uiSyncTimer.Stop();

            GC.SuppressFinalize(this);
        }
    }
}
