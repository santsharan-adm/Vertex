using IPCSoftware.App;
using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class PLCIOViewModel : BaseViewModel, IDisposable
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly SafePoller _timer;
        private readonly CoreClient _coreClient;
        private readonly UiTcpClient _tcpClient;

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

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(); }
        }

        public ICommand ToggleOutputCommand { get; }

        public PLCIOViewModel(
            CoreClient coreClient, 
            IPLCTagConfigurationService tagService,
            IAppLogger logger) : base(logger)
        {
            _tagService = tagService;
            _coreClient = coreClient;

            InitializeAsync();

            ToggleOutputCommand = new RelayCommand<IoTagModel>(OnToggleOutput);

            _timer = new SafePoller(TimeSpan.FromMilliseconds(100),
                TimerTick);
            
            _timer.Start();
        }

        private async void InitializeAsync()
        {
            try
            {
                var configTags = await _tagService.GetAllTagsAsync();

                AllInputTags.Clear();
                AllOutputTags.Clear();

                foreach (var tag in configTags)
                {
                    var model = new IoTagModel
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        Description = tag.Description,
                        Value = false
                    };

                    if (tag.Name != null)
                    {
                        //AllInputTags.Add(model);
                        if (tag.IOType.Equals("Input", StringComparison.OrdinalIgnoreCase))
                            AllInputTags.Add(model);
                        else if (tag.IOType.Equals("Output", StringComparison.OrdinalIgnoreCase))
                            AllOutputTags.Add(model);
                    }
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async void OnToggleOutput(IoTagModel tag)
        {
            try
            {
                if (tag == null) return;

                try
                {
                    bool currentState = tag.Value is bool b && b;
                    bool newValue = !currentState;

                    _isWriting = true;

                    tag.Value = newValue;

                    await _coreClient.WriteTagAsync(tag.Id, newValue);
                }
                finally
                {
                    _isWriting = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task TimerTick()
        {
            if ( _isWriting)
                return;

            try
            {
                var liveData = await _coreClient.GetIoValuesAsync(5);
                UpdateValues(liveData);
            }
          
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void ApplyFilter()
        {
            FilterList(AllInputTags, FilteredInputs);
            FilterList(AllOutputTags, FilteredOutputs);
        }

        private void FilterList(List<IoTagModel> source, ObservableCollection<IoTagModel> target)
        {
            try
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
                        (t.Description != null && t.Description.ToLower().Contains(s)) ||
                        t.Id.ToString().Contains(s));
                }

                foreach (var item in filtered)
                    target.Add(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void UpdateValues(Dictionary<int, object> dict)
        {
            try
            {
                if (dict == null) return;

                foreach (var input in AllInputTags)
                {
                    if (dict.TryGetValue(input.Id, out var live))
                        input.Value = live;
                }

                foreach (var output in AllOutputTags)
                {
                    if (dict.TryGetValue(output.Id, out var live))
                        output.Value = live;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        public void Dispose()
        {
            try
            {
                _timer.Dispose();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

      
    }

}