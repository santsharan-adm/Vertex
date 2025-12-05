using IPCSoftware.App;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class PLCIOViewModel : BaseViewModel
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly DispatcherTimer _timer;
        private readonly CoreClient _coreClient;


        // Consolidated Collections for Single DataGrid
        public ObservableCollection<IoTagModel> FilteredInputs { get; } = new();
        public ObservableCollection<IoTagModel> FilteredOutputs { get; } = new();

        private readonly List<IoTagModel> AllInputTags = new();
        private readonly List<IoTagModel> AllOutputTags = new();

        private bool _isWriting = false;

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


        // Command for the Toggle Button
        public ICommand ToggleOutputCommand { get; }

        public PLCIOViewModel(UiTcpClient tcpClient, IPLCTagConfigurationService tagService)
        {
            _tagService = tagService;
            _coreClient = new CoreClient(App.TcpClient);
            InitializeAsync();

            // Initialize Command
            ToggleOutputCommand = new RelayCommand<IoTagModel>(OnToggleOutput);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1500);
            _timer.Tick += TimerTick;
            _timer.Start();
        }


        private async void InitializeAsync()
        {
            // 1. Fetch tags from the service (Async)
            var configTags = await _tagService.GetAllTagsAsync();

            AllInputTags.Clear();
            AllOutputTags.Clear();

            foreach (var tag in configTags)
            {
                var model = new IoTagModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Value = false // Default state
                };

                // 2. Apply your specific filtering logic
                if (tag.Name != null)
                {
                    if (tag.Name.StartsWith("IO_INPUT", StringComparison.OrdinalIgnoreCase))
                    {
                        AllInputTags.Add(model);
                    }
                    else if (tag.Name.StartsWith("IO_OUTPUT", StringComparison.OrdinalIgnoreCase))
                    {
                        AllOutputTags.Add(model);
                    }
                }
            }

            // 3. Refresh the UI lists
            ApplyFilter();
        }

        //private async void OnToggleOutput(IoTagModel tag)
        //{
        //    if (tag == null) return;



        //    try
        //    {
        //        bool currentState = false;
        //        if (tag.Value is bool b) currentState = b;

        //        bool newValue = !currentState;
        //        //tag.Value = newValue;

        //        await _coreClient.WriteTagAsync(tag.Id, newValue);

        //        System.Diagnostics.Debug.WriteLine($"Toggling Output: {tag.Name} (ID: {tag.Id})");
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Error toggling output: {ex.Message}");
        //    }
        //}
        private async void OnToggleOutput(IoTagModel tag)
        {
            if (tag == null) return;

            try
            {
                bool currentState = tag.Value is bool b && b;
                bool newValue = !currentState;

                // lock UI updates
                _isWriting = true;

                // optimistic UI
                tag.Value = newValue;

                // real PLC write
                await _coreClient.WriteTagAsync(tag.Id, newValue);
            }
            finally
            {
                _isWriting = false;
            }
        }

        //private async void TimerTick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var liveData = await _coreClient.GetIoValuesAsync();
        //        UpdateValues(liveData);
        //    }
        //    catch { }
        //}

        private async void TimerTick(object sender, EventArgs e)
        {
            // skip update during write
            if (_isWriting)
                return;

            try
            {
                var liveData = await _coreClient.GetIoValuesAsync();
                UpdateValues(liveData);
            }
            catch { }
        }

        //private void LoadTags()
        //{
        //    // Load from a shared TagConfig service
        //    // Replace this with your own tag loading
        //    var tags = TagConfigProvider.Tags;

        //    foreach (var tag in tags)
        //    {
        //        var model = new IoTagModel
        //        {
        //            Id = tag.Id,
        //            Name = tag.Name,
        //            Value = null
        //        };

        //        if (tag.Name.StartsWith("IO_INPUT", StringComparison.OrdinalIgnoreCase))
        //            AllInputTags.Add(model);
        //        else if (tag.Name.StartsWith("IO_OUTPUT", StringComparison.OrdinalIgnoreCase))
        //            AllOutputTags.Add(model);
        //    }
        //}

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