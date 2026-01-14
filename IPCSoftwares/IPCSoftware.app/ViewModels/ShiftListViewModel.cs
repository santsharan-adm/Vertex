using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services.UI; // For IDialogService
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class ShiftListViewModel : BaseViewModel
    {
        private readonly IShiftManagementService _shiftService;
        private readonly IDialogService _dialogService;

        // Collection bound to DataGrid
        public ObservableCollection<ShiftConfigurationModel> Shifts { get; } = new();

        private ShiftConfigurationModel _selectedShift;
        public ShiftConfigurationModel SelectedShift
        {
            get => _selectedShift;
            set => SetProperty(ref _selectedShift, value);
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand DeleteShiftCommand { get; }
        public ICommand EditShiftCommand { get; } // Navigate to Configuration View
        public ICommand AddShiftCommand { get; }  // Navigate to Configuration View (New)

        public ShiftListViewModel(
            IShiftManagementService shiftService,
            IAppLogger logger,
            IDialogService dialogService) : base (logger)
        {
            _shiftService = shiftService;
            _dialogService = dialogService;

            RefreshCommand = new RelayCommand(LoadData);
            DeleteShiftCommand = new RelayCommand<ShiftConfigurationModel>(DeleteShift);

            // NOTE: You need to wire these to your Navigation logic
            EditShiftCommand = new RelayCommand<ShiftConfigurationModel>(OnEditShift);
            AddShiftCommand = new RelayCommand(OnAddShift);

            LoadData();
        }

        public async void LoadData()
        {
            Shifts.Clear();
            var data = await _shiftService.GetAllShiftsAsync();
            foreach (var s in data)
            {
                Shifts.Add(s);
            }
        }

        private async void DeleteShift(ShiftConfigurationModel shift)
        {
            if (shift == null) return;

            bool confirm = _dialogService.ShowYesNo(
                $"Are you sure you want to delete shift '{shift.ShiftName}'?",
                "Confirm Delete");

            if (confirm)
            {
                await _shiftService.DeleteShiftAsync(shift.Id);
                LoadData(); // Refresh list
            }
        }

        private void OnEditShift(ShiftConfigurationModel shift)
        {
            // TODO: Navigate to ShiftConfigurationView and pass 'shift' object
            // Example: _navigationService.NavigateTo<ShiftConfigurationViewModel>(shift);
        }

        private void OnAddShift()
        {
            // TODO: Navigate to ShiftConfigurationView (Empty)
        }
    }
}