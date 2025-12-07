using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace IPCSoftware.App.ViewModels
{
    public class TagControlViewModel : BaseViewModel, IDisposable
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly DispatcherTimer _timer;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;

        private bool _disposed;

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

        public TagControlViewModel(IPLCTagConfigurationService tagService, UiTcpClient tcpClient, IDialogService dialog)
        {
            _tagService = tagService;
            _coreClient = new CoreClient(tcpClient);
            _dialog = dialog;

            WriteCommand = new RelayCommand<WritableTagItem>(async (item) => await OnWriteAsync(item));

            // Load tags on startup
            InitializeAsync();


            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += TimerTick;
            _timer.Start();
        }


        private async void TimerTick(object sender, EventArgs e)
        {
            if (_disposed)
                return;

            try
            {
                var liveData = await _coreClient.GetIoValuesAsync();
                UpdateValues(liveData);
            }
            catch { }
        }


        private void ApplyFilter()
        {
            // We are modifying the UI collection, so we clear it first
            WritableTags.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Case 1: No Search - Add everything from Master List to UI List
                foreach (var item in AllInputs)
                {
                    WritableTags.Add(item);
                }
            }
            else
            {
                // Case 2: Search Active - Filter Master List and add matches to UI List
                var s = SearchText.Trim().ToLower();

                var matches = AllInputs.Where(t =>
                    (t.Model.Name != null && t.Model.Name.ToLower().Contains(s)) ||
                    t.Model.TagNo.ToString().Contains(s) // Searching by TagNo (what is shown in Grid)
                );

                foreach (var item in matches)
                {
                    WritableTags.Add(item);
                }
            }
        }


        private async void InitializeAsync()
        {
            var allTags = await _tagService.GetAllTagsAsync();

            // 1. Clear both lists
            WritableTags.Clear();
            AllInputs.Clear();

            // 2. Filter for writable tags only
            var writable = allTags.Where(t => t.CanWrite).ToList();

            // 3. Populate the Master List (AllInputs)
            foreach (var tag in writable)
            {
                var item = new WritableTagItem(tag);
                AllInputs.Add(item);
            }

            // 4. Populate UI list (WritableTags) based on current filter
            ApplyFilter();
        }



        private async Task OnWriteAsync(WritableTagItem item)
        {
            if (item == null) return;

            // 1. Validate Input
            if (!ValidateInput(item, out object parsedValue))
            {
                _dialog.ShowWarning($"Invalid format for {item.DataTypeDisplay}...");
                return;
            }

            // [STEP 1] PAUSE THE TIMER
            // Stop reading background data so we don't overwrite our new value with old data
            _timer.Stop();

            try
            {
                bool success = await _coreClient.WriteTagAsync(item.Model.TagNo, parsedValue);

                if (success)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // [STEP 2] OPTIMISTIC UPDATE
                        // Manually set the DisplayValue to what we just wrote.
                        // This gives the user instant feedback that it worked.
                  

                        // Clear the input box
                        item.InputValue = null;
                    });

                    // [STEP 3] SETTLE DELAY
                    // Give the PLC a moment (e.g., 500ms) to update its internal memory
                    // before we start asking it for values again.
                    await Task.Delay(50);
                }
            }
            catch (Exception ex)
            {
                _dialog.ShowWarning($"Failed to write to PLC: {ex.Message}");
            }
            finally
            {
                // [STEP 4] RESUME TIMER
                // Always restart the timer, even if the write failed
                if (!_disposed)
                {
                    _timer.Start();
                }
            }
        }

        private bool ValidateInput(WritableTagItem item, out object result)
        {
            result = null;
            string input = item.InputValue as string;

            if (string.IsNullOrWhiteSpace(input)) return false;

            switch (item.Model.DataType)
            {
                case 1: // Int / Int16
                    if (short.TryParse(input, out short sVal)) { result = sVal; return true; }
                    if (int.TryParse(input, out int iVal) && iVal >= short.MinValue && iVal <= short.MaxValue) { result = (short)iVal; return true; }
                    break;

                case 2: // Word / Dint (Int32)
                    if (int.TryParse(input, out int intVal)) { result = intVal; return true; }
                    break;

                case 3: // Bit / Bool
                    if (bool.TryParse(input, out bool bVal)) { result = bVal; return true; }
                    if (input == "1") { result = true; return true; }
                    if (input == "0") { result = false; return true; }
                    break;

                case 4: // Float / FP
                    if (float.TryParse(input, out float fVal)) { result = fVal; return true; }
                    break;

                case 5: // String
                    result = input;
                    return true;

                default: // Fallback to Int
                    if (int.TryParse(input, out int defVal)) { result = defVal; return true; }
                    break;
            }

            return false;
        }

        /*   private void UpdateValues(Dictionary<int, object> dict)
           {
               if (dict == null) return;

               foreach (var input in WritableTags)
               {
                   if (dict.TryGetValue(input.Model.Id, out var live))
                       input.InputValue =(string) live;
               }

           }*/

        private void UpdateValues(Dictionary<int, object> dict)
        {
            if (dict == null) return;

            foreach (var input in AllInputs)
            {
                if (dict.TryGetValue(input.Model.Id, out var live))
                {
                    input.DisplayValue = live;
                }
            }
        }

        private string GetValidationMessage(int dataType)
        {
            return dataType switch
            {
                1 => "Integer (-32768 to 32767)",
                2 => "Integer (Whole numbers)",
                3 => "True/False or 1/0",
                4 => "Decimal number (e.g., 12.34)",
                5 => "Text string",
                _ => "Unknown type"
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer.Stop();
            _timer.Tick -= TimerTick;
            GC.SuppressFinalize(this);
        }
    }

    // Helper Wrapper Class
    public class WritableTagItem : BaseViewModel
    {
        public PLCTagConfigurationModel Model { get; }

        //private string _inputValue;
        //public string InputValue
        //{
        //    get => _inputValue;
        //    set => SetProperty(ref _inputValue, value);
        //}

        public string DataTypeDisplay => GetDataTypeName(Model.DataType);

        public WritableTagItem(PLCTagConfigurationModel model)
        {
            Model = model;
        }
        private object _displayValue;
        public object DisplayValue
        {
            get => _displayValue;
            set => SetProperty(ref _displayValue, value);
            //set { _selectedTabIndex = value; OnPropertyChanged(); }
            //set
            //{
            //    _value = value;
            //    OnPropertyChanged();
            //    OnPropertyChanged(nameof(DisplayStatus));
            //}
        }

        private object _inputValue;
        public object InputValue
        {
            get => _inputValue;
            set => SetProperty(ref _inputValue, value);
            //set
            //{
            //    _inputValue = value;
            //    OnPropertyChanged();
            //    OnPropertyChanged(nameof(DisplayStatus));
            //}
        }



      


        private string GetDataTypeName(int typeId)
        {
            return typeId switch
            {
                1 => "Int16",
                2 => "Int32",
                3 => "Boolean",
                4 => "Float",
                5 => "String",
                _ => "Unknown"
            };
        }
    }
}