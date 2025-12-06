using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class TagControlViewModel : BaseViewModel
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;

        public ObservableCollection<WritableTagItem> WritableTags { get; } = new();

        public ICommand WriteCommand { get; }

        public TagControlViewModel(IPLCTagConfigurationService tagService, UiTcpClient tcpClient, IDialogService dialog)
        {
            _tagService = tagService;
            _coreClient = new CoreClient(tcpClient);
            _dialog = dialog;

            WriteCommand = new RelayCommand<WritableTagItem>(async (item) => await OnWriteAsync(item));

            // Load tags on startup
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            var allTags = await _tagService.GetAllTagsAsync();

            WritableTags.Clear();

            // Filter only tags where CanWrite is true
            var writable = allTags.Where(t => t.CanWrite).ToList();

            foreach (var tag in writable)
            {
                WritableTags.Add(new WritableTagItem(tag));
            }
        }

        private async Task OnWriteAsync(WritableTagItem item)
        {
            if (item == null) return;

            // 1. Validate Input
            if (!ValidateInput(item, out object parsedValue))
            {
                _dialog.ShowWarning($"Invalid format for {item.DataTypeDisplay}.\n\nAllowed values:\n{GetValidationMessage(item.Model.DataType)}");
                return;
            }

            try
            {
                // 2. Send to PLC (Simulated call to CoreClient)
                // In a real scenario: await _coreClient.WriteTagAsync(item.Model.TagNo, parsedValue);

                // Since CoreClient might not have a generic Write method exposed yet, we debug print:
                System.Diagnostics.Debug.WriteLine($"[WRITE] Tag: {item.Model.Name} ({item.Model.TagNo}), Value: {parsedValue}, Type: {item.DataTypeDisplay}");

                // Optional: Clear input or show success
                // item.InputValue = string.Empty; 
                _dialog.ShowMessage($"Successfully wrote '{parsedValue}' to {item.Model.Name}");
            }
            catch (Exception ex)
            {
                _dialog.ShowWarning($"Failed to write to PLC: {ex.Message}");
            }
        }

        private bool ValidateInput(WritableTagItem item, out object result)
        {
            result = null;
            string input = item.InputValue?.Trim();

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
    }

    // Helper Wrapper Class
    public class WritableTagItem : BaseViewModel
    {
        public PLCTagConfigurationModel Model { get; }

        private string _inputValue;
        public string InputValue
        {
            get => _inputValue;
            set => SetProperty(ref _inputValue, value);
        }

        public string DataTypeDisplay => GetDataTypeName(Model.DataType);

        public WritableTagItem(PLCTagConfigurationModel model)
        {
            Model = model;
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