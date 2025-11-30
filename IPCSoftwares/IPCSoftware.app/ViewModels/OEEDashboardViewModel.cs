using IPCSoftware.App.Controls;
using IPCSoftware.App.Helpers;
using IPCSoftware.App.Views;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class OEEDashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        // ================================================================
        // BASE FIELDS
        // ================================================================
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly UiTcpClient _uiClient = new UiTcpClient();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public DispatcherTimer Timer { get; }

        public ICommand UnloadCommand { get; }
        public ICommand ShowImageCommand { get; }

        private void Notify([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        // ================================================================
        // REAL-TIME DASHBOARD PROPERTIES
        // ================================================================
        private string _currentDateTime;
        public string CurrentDateTime
        {
            get => _currentDateTime;
            set { _currentDateTime = value; Notify(); }
        }

        private string _availability;
        public string Availability
        {
            get => _availability;
            set { _availability = value; Notify(); }
        }

        private string _performance;
        public string Performance
        {
            get => _performance;
            set { _performance = value; Notify(); }
        }

        private string _quality;
        public string Quality
        {
            get => _quality;
            set { _quality = value; Notify(); }
        }

        public string _overallOEE;
        public string OverallOEE
        {
            get => _overallOEE;
            set
            {
                _overallOEE = value;
                Notify();

                // update donut
                if (double.TryParse(value, out double oee))
                {
                    UpdateOeeDonut(oee);
                }
            }
        }


        private string _operatingTime;
        public string OperatingTime
        {
            get => _operatingTime;
            set { _operatingTime = value; Notify(); }
        }

        private string _downtime;
        public string Downtime
        {
            get => _downtime;
            set { _downtime = value; Notify(); }
        }

        private string _goodUnits;
        public string GoodUnits
        {
            get => _goodUnits;
            set { _goodUnits = value; Notify(); Notify(nameof(RejectedUnits)); }
        }

        private string _totalUnits;
        public string TotalUnits
        {
            get => _totalUnits;
            set { _totalUnits = value; Notify(); Notify(nameof(RejectedUnits)); }


        }

        public string RejectedUnits
        {
            get
            {
                if (int.TryParse(TotalUnits, out int total) &&
                    int.TryParse(GoodUnits, out int good))
                {
                    return (total - good).ToString();
                    Debug.WriteLine($"Good={GoodUnits}, Total={TotalUnits}, Rej={RejectedUnits}");
                }
                return "0";
            }
        }


        private string _remarks;
        public string Remarks
        {
            get => _remarks;
            set { _remarks = value; Notify(); }
        }

        // ================================================================
        // EXTRA VISUAL ITEMS (IMAGES / PIE / ETC)
        // ================================================================
        public ObservableCollection<PieSliceModel> OeeDonut { get; set; }
                = new ObservableCollection<PieSliceModel>();

        private void UpdateOeeDonut(double oee)
        {
            OeeDonut.Clear();
            OeeDonut.Add(new PieSliceModel { Label = "OEE", Value = oee });
            OeeDonut.Add(new PieSliceModel { Label = "Remaining", Value = 100 - oee });

            CenterOeeText = $"{oee:0.#}%";
        }

        private string _centerOeeText;
        public string CenterOeeText
        {
            get => _centerOeeText;
            set { _centerOeeText = value; Notify(); }
        }


        public ObservableCollection<CameraImageModel> CameraImages { get; set; }
            = new ObservableCollection<CameraImageModel>();

        // ================================================================
        // CONSTRUCTOR
        // ================================================================
        public OEEDashboardViewModel()
        {
            // Commands
            ShowImageCommand = new RelayCommand<CameraImageModel>(ShowImage);
            UnloadCommand = new RelayCommand(() => Dispose());

            // CLOCK (Top Right)
            Timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            Timer.Tick += (s, e) =>
            {
                CurrentDateTime = DateTime.Now.ToString("dddd, MMM dd yyyy HH:mm");
            };
            Timer.Start();

            // TCP
            _uiClient.DataReceived += OnDataReceived;
            _uiClient.StartAsync("127.0.0.1", 5050);

            StartPolling();
        }

        private void ShowImage(CameraImageModel img)
        {
            if (img == null) return;
            var win = new FullImageView(img.ImagePath);
            win.ShowDialog();
        }

        // ================================================================
        // POLLING (send RequestId=4 every 1 second)
        // ================================================================
        private async void StartPolling()
        {
            while (!_cts.IsCancellationRequested)
            {
                _uiClient.Send("{\"RequestId\":4}\n");
                await Task.Delay(1000);
            }
        }

        // ================================================================
        // DATA RECEIVED FROM CORESERVICE
        // ================================================================
        private void OnDataReceived(string msg)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ResponsePackage>(msg);

                if (response?.Parameters != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateDashboard(response.Parameters);
                    });
                }
            }
            catch
            {
                // ignore malformed incoming packets
            }
        }

        // ================================================================
        // UPDATE DASHBOARD USING SAFE STRING-ONLY LOGIC
        // ================================================================
        private void UpdateDashboard(Dictionary<uint, object> p)
        {
            if (p.TryGetValue(1u, out var a)) Availability = a?.ToString();         //Availability
            if (p.TryGetValue(2u, out var b)) Performance = b?.ToString();          //Performance
            if (p.TryGetValue(3u, out var c)) Quality = c?.ToString();              //
            if (p.TryGetValue(4u, out var d)) OverallOEE = d?.ToString();
            if (p.TryGetValue(5u, out var e)) OperatingTime = e?.ToString();
            if (p.TryGetValue(6u, out var f)) Downtime = f?.ToString();
            if (p.TryGetValue(7u, out var g)) GoodUnits = g?.ToString();
            if (p.TryGetValue(8u, out var h)) TotalUnits = h?.ToString();
            if (p.TryGetValue(9u, out var i)) Remarks = i?.ToString();              //Machine Status
        }

        // ================================================================
        // DISPOSABLE CLEANUP
        // ================================================================
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Cancel();
                Timer?.Stop();
                _cts.Dispose();
            }
        }
    }
}
