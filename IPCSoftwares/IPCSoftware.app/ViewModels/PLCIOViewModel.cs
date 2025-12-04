using IPCSoftware.Shared.Models;
using IPCSoftware.App;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using IPCSoftware.Shared;
using IPCSoftware.App.Services;

namespace IPCSoftware.App.ViewModels
{
    public class PLCIOViewModel : BaseViewModel
    {
        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        // Selected Tab Index (0 = Inputs, 1 = Outputs)
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(); }
        }

        // Consolidated Collections for Single DataGrid
        public ObservableCollection<IoTagModel> FilteredInputs { get; } = new();
        public ObservableCollection<IoTagModel> FilteredOutputs { get; } = new();

        private readonly List<IoTagModel> AllInputTags = new();
        private readonly List<IoTagModel> AllOutputTags = new();

        private readonly DispatcherTimer _timer;
        private readonly CoreClient _coreClient;

        // Command for the Toggle Button
        public ICommand ToggleOutputCommand { get; }

        public PLCIOViewModel(UiTcpClient tcpClient)
        {
            LoadTags();
            ApplyFilter();

            _coreClient = new CoreClient(App.TcpClient);

            // Initialize Command
            ToggleOutputCommand = new RelayCommand<IoTagModel>(OnToggleOutput);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        private void OnToggleOutput(IoTagModel tag)
        {
            if (tag == null) return;

            // Logic to toggle the output. 
            // Since we don't have the full write implementation here, we assume:
            // 1. Get current bool state
            // 2. Invert it
            // 3. Send write command via CoreClient (Placeholder logic below)

            try
            {
                // Simple toggle logic for UI feedback immediately (optional)
                // In real app, you might wait for PLC feedback
                // bool currentState = false;
                // if (tag.Value is bool b) currentState = b;

                // Call your Write method here, e.g.:
                // await _coreClient.WriteTagAsync(tag.Id, !currentState);

                System.Diagnostics.Debug.WriteLine($"Toggling Output: {tag.Name} (ID: {tag.Id})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling output: {ex.Message}");
            }
        }

        private async void TimerTick(object sender, EventArgs e)
        {
            try
            {
                var liveData = await _coreClient.GetIoValuesAsync();
                UpdateValues(liveData);
            }
            catch { }
        }

        private void LoadTags()
        {
            // Load from a shared TagConfig service
            // Replace this with your own tag loading
            var tags = TagConfigProvider.Tags;

            foreach (var tag in tags)
            {
                var model = new IoTagModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Value = null
                };

                if (tag.Name.StartsWith("IO_INPUT", StringComparison.OrdinalIgnoreCase))
                    AllInputTags.Add(model);
                else if (tag.Name.StartsWith("IO_OUTPUT", StringComparison.OrdinalIgnoreCase))
                    AllOutputTags.Add(model);
            }
        }

        private void ApplyFilter()
        {
            FilterList(AllInputTags, FilteredInputs);
            FilterList(AllOutputTags, FilteredOutputs);
        }

        private void FilterList(List<IoTagModel> source, ObservableCollection<IoTagModel> target)
        {
            target.Clear();
            IEnumerable<IoTagModel> filtered;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = source;
            }
            else
            {
                var s = SearchText.Trim().ToLower();
                filtered = source.Where(t =>
                    (t.Name != null && t.Name.ToLower().Contains(s)) ||
                    t.Id.ToString().Contains(s));
            }

            foreach (var item in filtered)
            {
                target.Add(item);
            }
        }

        private void UpdateValues(Dictionary<int, object> dict)
        {
            if (dict == null) return;

            foreach (var input in AllInputTags)
            {
                if (dict.TryGetValue(input.Id, out var live))
                {
                    input.Value = live;
                    // Assuming OnPropertyChanged is fired inside IoTagModel when Value is set
                }
            }

            foreach (var output in AllOutputTags)
            {
                if (dict.TryGetValue(output.Id, out var live))
                {
                    output.Value = live;
                }
            }
        }
    }
}