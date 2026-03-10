using IPCSoftware.Helpers;
using IPCSoftware.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;

using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;

using IPCSoftware.UI.CommonViews.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class TagControlViewModel : BaseViewModel, IDisposable
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly SafePoller _timer;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;

        public ObservableCollection<WritableTagItem> WritableTags { get; } = new();
        public ObservableCollection<WritableTagItem> AllInputs { get; } = new();

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

        public ICommand WriteCommand { get; }

        public TagControlViewModel(
            IPLCTagConfigurationService tagService,
            CoreClient coreClient,
            IDialogService dialog,
            IAppLogger logger) : base(logger)
        {
            _tagService = tagService;
            _coreClient = coreClient;
            _dialog = dialog;

            WriteCommand = new RelayCommand<WritableTagItem>(async (item) => await OnWriteAsync(item));

            InitializeAsync();

            _timer = new SafePoller(TimeSpan.FromMilliseconds(100), TimerTick);
            _timer.Start();
        }

        private async Task TimerTick()
        {
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
            WritableTags.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var item in AllInputs)
                    WritableTags.Add(item);
            }
            else
            {
                var s = SearchText.Trim().ToLower();

                var matches = AllInputs.Where(t =>
                    t.Model.Name != null && t.Model.Name.ToLower().Contains(s) ||
                    t.Model.TagNo.ToString().Contains(s));

                foreach (var item in matches)
                    WritableTags.Add(item);
            }
        }

        private async void InitializeAsync()
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();

                WritableTags.Clear();
                AllInputs.Clear();

                var writable = allTags.Where(t => t.CanWrite).ToList();

                foreach (var tag in writable)
                {
                    var item = new WritableTagItem(tag);
                    AllInputs.Add(item);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task OnWriteAsync(WritableTagItem item)
        {
            try
            {
                if (item == null) return;

                if (!ValidateInput(item, out object parsedValue))
                {
                    _dialog.ShowWarning($"Invalid format for {item.DataTypeDisplay}...");
                    return;
                }

                _timer.Stop();

                try
                {
                    bool success = await _coreClient.WriteTagAsync(item.Model.TagNo, parsedValue);

                    if (success)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            item.InputValue = null;
                        });

                        await Task.Delay(50);
                    }
                }
                catch (Exception ex)
                {
                    _dialog.ShowWarning($"Failed to write to PLC: {ex.Message}");
                }
                finally
                {
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private bool ValidateInput(WritableTagItem item, out object result)
        {
            result = null;
            string input = item.InputValue as string;

            if (string.IsNullOrWhiteSpace(input)) return false;

            switch (item.Model.DataType)
            {
                case 1:
                    if (short.TryParse(input, out short sVal)) { result = sVal; return true; }
                    if (int.TryParse(input, out int iVal) && iVal >= short.MinValue && iVal <= short.MaxValue)
                    { result = (short)iVal; return true; }
                    break;

                case 2:
                    if (int.TryParse(input, out int intVal)) { result = intVal; return true; }
                    break;

                case 3:
                    if (bool.TryParse(input, out bool bVal)) { result = bVal; return true; }
                    if (input == "1") { result = true; return true; }
                    if (input == "0") { result = false; return true; }
                    break;

                case 4:
                    if (float.TryParse(input, out float fVal)) { result = fVal; return true; }
                    break;

                case 5:
                    result = input;
                    return true;

                case 6:
                    if (ushort.TryParse(input, out ushort sVal2)) { result = sVal2; return true; }
                    if (uint.TryParse(input, out uint iVal2) && iVal2 <= ushort.MaxValue)
                    { result = (ushort)iVal2; return true; }
                    break;

                case 7:
                    if (int.TryParse(input, out int intVal2)) { result = intVal2; return true; }
                    break;

                default:
                    if (int.TryParse(input, out int defVal)) { result = defVal; return true; }
                    break;
            }

            return false;
        }

        private void UpdateValues(Dictionary<int, object> dict)
        {
            if (dict == null) return;

            foreach (var input in AllInputs)
            {
                if (dict.TryGetValue(input.Model.Id, out var live))
                {
                    input.DisplayValue = live;
                    input.Description = input.Model.Description;
                }
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}