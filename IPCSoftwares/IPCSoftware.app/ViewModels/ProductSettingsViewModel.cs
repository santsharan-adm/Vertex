using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class ProductSettingsViewModel : BaseViewModel
    {
        private readonly IProductConfigurationService _productService;
        private readonly IDialogService _dialog;
        private readonly CoreClient _coreClient;

        private string _productName;
        public string ProductName { get => _productName; set => SetProperty(ref _productName, value); }

        private string _productCode;
        public string ProductCode { get => _productCode; set => SetProperty(ref _productCode, value); }

        private int _selectedItemCount;
        public int SelectedItemCount { get => _selectedItemCount; set => SetProperty(ref _selectedItemCount, value); }

        private int _gridRows;
        public int GridRows { get => _gridRows; set => SetProperty(ref _gridRows, value); }

        private int _gridColumns;
        public int GridColumns { get => _gridColumns; set => SetProperty(ref _gridColumns, value); }

        // Dropdowns
        public List<int> ItemCounts { get; } = Enumerable.Range(1, 12).ToList(); // Expanded for larger trays
        public List<int> ColumnOptions { get; } = Enumerable.Range(1, 3).ToList();
        public List<int> RowOptions { get; } = Enumerable.Range(1, 4).ToList();

        public ICommand SaveCommand { get; }

        public ProductSettingsViewModel(IProductConfigurationService productService,
            CoreClient coreClient, IDialogService dialog, IAppLogger logger) : base(logger)
        {
            _productService = productService;
            _coreClient = coreClient;
            _dialog = dialog;
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var config = await _productService.LoadAsync();
            ProductName = config.ProductName;

            ProductCode = config.ProductCode;
            SelectedItemCount = config.TotalItems;

            // Default to 4x3 if not set
            GridRows = config.GridRows > 0 ? config.GridRows : 4;
            GridColumns = config.GridColumns > 0 ? config.GridColumns : 3;
        }

        private async Task SaveAsync()
        {
            if (await IsMachineRunningAsync())
            {
                _dialog.ShowWarning("Cannot save settings while Machine is running (Auto/Dry Run).");
                return;
            }

            // Validation
            if (GridRows * GridColumns < SelectedItemCount)
            {
                _dialog.ShowWarning($"Grid Layout ({GridRows}x{GridColumns} = {GridRows * GridColumns}) is too small to hold {SelectedItemCount} items.\nPlease increase Rows or Columns.");
                return;
            }

            var config = new ProductSettingsModel
            {
                ProductName = ProductName,
                ProductCode = ProductCode,
                TotalItems = SelectedItemCount,
                GridRows = GridRows,
                GridColumns = GridColumns
            };

            await _productService.SaveAsync(config);

            try
            {
                if (_coreClient.isConnected)
                {
                    _logger.LogInfo($"[ProductSettings] Syncing Total Items ({SelectedItemCount}) to PLC Tag {ConstantValues.NO_OF_Station}...", LogType.Audit);
                    bool success = await _coreClient.WriteTagAsync(ConstantValues.NO_OF_Station, SelectedItemCount);
                    if (!success) _dialog.ShowWarning("Settings saved, but failed to write Station Count to PLC.");
                }
                else
                {
                    _dialog.ShowWarning("Settings saved, but PLC is disconnected.\nStation Count will not be updated in PLC until reconnected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PLC Write Error: {ex.Message}", LogType.Diagnostics);
            }

            _dialog.ShowMessage("Product Settings Saved Successfully.");
        }

        private async Task<bool> IsMachineRunningAsync()
        {
            try
            {
                // Packet 5 contains the Mode Tags (Auto, DryRun)
                var data = await _coreClient.GetIoValuesAsync(5);

                if (data != null && data.Count > 0)
                {
                    bool isAuto = GetBool(data, ConstantValues.Mode_Auto.Read);
                    bool isDryRun = GetBool(data, ConstantValues.Mode_DryRun.Read);

                    // If either is true, machine is running
                    return isAuto || isDryRun;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Machine Status Check Failed: {ex.Message}", LogType.Diagnostics);
            }
            return false; // Default to allow if check fails (or handle as block depending on safety requirement)
        }

        private bool GetBool(Dictionary<int, object> data, int tagId)
        {
            if (data.TryGetValue(tagId, out object val))
            {
                return Convert.ToBoolean(val);
            }
            return false;
        }

    }
}