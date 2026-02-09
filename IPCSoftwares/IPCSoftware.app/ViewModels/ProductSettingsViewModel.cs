using IPCSoftware.App.Helpers;
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

        public ProductSettingsViewModel(IProductConfigurationService productService, IDialogService dialog, IAppLogger logger) : base(logger)
        {
            _productService = productService;
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
            _dialog.ShowMessage("Product Settings Saved Successfully.");
        }
    }
}