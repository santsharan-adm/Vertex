using IPCSoftware.Shared.Models;
using IPCSoftware.App;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public ObservableCollection<IoTagModel> FilteredInputLeft { get; } = new();
        public ObservableCollection<IoTagModel> FilteredInputRight { get; } = new();

        public ObservableCollection<IoTagModel> FilteredOutputLeft { get; } = new();
        public ObservableCollection<IoTagModel> FilteredOutputRight { get; } = new();

        private readonly List<IoTagModel> AllInputTags = new();
        private readonly List<IoTagModel> AllOutputTags = new();

        private readonly DispatcherTimer _timer;

        private readonly CoreClient _coreClient;

        public PLCIOViewModel(UiTcpClient tcpClient)
        {
            LoadTags();   // Load from CSV or shared config
            ApplyFilter();
            _coreClient = new CoreClient(App.TcpClient);
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += TimerTick;
            _timer.Start();
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
            FilterBySearch(AllInputTags, FilteredInputLeft, FilteredInputRight);
            FilterBySearch(AllOutputTags, FilteredOutputLeft, FilteredOutputRight);
        }

        private void FilterBySearch(
            List<IoTagModel> source,
            ObservableCollection<IoTagModel> left,
            ObservableCollection<IoTagModel> right)
        {
            left.Clear();
            right.Clear();

            IEnumerable<IoTagModel> filtered;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = source;
            }
            else
            {
                var s = SearchText.Trim().ToLower();

                filtered = source.Where(t =>
                    t.Name.ToLower().Contains(s)
                    || t.Id.ToString().Contains(s));
            }

            var list = filtered.ToList();
            int count = list.Count;
            int half = (count + 1) / 2;

            for (int i = 0; i < half; i++)
                left.Add(list[i]);

            for (int i = half; i < count; i++)
                right.Add(list[i]);
        }

        private void UpdateValues(Dictionary<int, object> dict)
        {
            foreach (var input in AllInputTags)
            {
                if (dict.TryGetValue(input.Id, out var live))
                {
                    input.Value = live;
                    System.Diagnostics.Debug.WriteLine($"INPUT {input.Name} = {live}");
                }
            }

            foreach (var output in AllOutputTags)
            {
                if (dict.TryGetValue(output.Id, out var live))
                {
                    output.Value = live;
                    System.Diagnostics.Debug.WriteLine($"OUTPUT {output.Name} = {live}");
                }
            }
        }

    }
}

