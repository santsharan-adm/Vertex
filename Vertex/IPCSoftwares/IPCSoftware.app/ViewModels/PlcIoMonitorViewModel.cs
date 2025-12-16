using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class PlcIoMonitorViewModel : INotifyPropertyChanged
    {
        private const string JsonPath = "plc_io_data.json";

        public ObservableCollection<PlcIoItem> IoItems { get; set; }

        private FileSystemWatcher _watcher;

        public ICommand ToggleCommand { get; }
        public ICommand RowDoubleClickCommand { get; }

        public PlcIoMonitorViewModel()
        {
            try
            {
                IoItems = new ObservableCollection<PlcIoItem>();
                LoadJson();
                WatchJsonFile();

                ToggleCommand = new RelayCommand<PlcIoItem>(ToggleValue);
                RowDoubleClickCommand = new RelayCommand<PlcIoItem>(ShowPopup);
            }
            catch (Exception)
            {
                // Prevent constructor crash
            }
        }

        private void LoadJson()
        {
            try
            {
                if (!File.Exists(JsonPath)) return;

                var json = File.ReadAllText(JsonPath);
                var list = JsonConvert.DeserializeObject<ObservableCollection<PlcIoItem>>(json);

                App.Current.Dispatcher.Invoke(() =>
                {
                    IoItems.Clear();
                    foreach (var item in list)
                        IoItems.Add(item);
                });
            }
            catch (Exception)
            {
                // Prevent JSON read / UI thread crash
            }
        }

        private void WatchJsonFile()
        {
            try
            {
                _watcher = new FileSystemWatcher(".", JsonPath)
                {
                    NotifyFilter = NotifyFilters.LastWrite
                };
                _watcher.Changed += (s, e) => LoadJson();
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception)
            {
                // Prevent FileSystemWatcher crash
            }
        }

        private void ToggleValue(PlcIoItem item)
        {
            try
            {
                item.Value = item.Value == "ON" ? "OFF" : "ON";

                SaveJson();
                OnPropertyChanged(nameof(IoItems));
            }
            catch (Exception)
            {
                // Prevent toggle/save crash
            }
        }

        private void SaveJson()
        {
            try
            {
                string json = JsonConvert.SerializeObject(IoItems, Formatting.Indented);
                File.WriteAllText(JsonPath, json);
            }
            catch (Exception)
            {
                // Prevent file write crash
            }
        }

        private void ShowPopup(PlcIoItem item)
        {
            try
            {
                MessageBox.Show(
                    $"Tag: {item.Tag}\nValue: {item.Value}\nDescription: {item.Description}",
                    "PLC IO Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception)
            {
                // Prevent popup crash
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch (Exception)
            {
                // Prevent notification crash
            }
        }
    }
}
