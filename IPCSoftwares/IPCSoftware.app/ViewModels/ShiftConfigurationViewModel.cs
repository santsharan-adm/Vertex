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
    public class ShiftConfigurationViewModel : BaseViewModel
    {
        private readonly IShiftManagementService _shiftService;
        private readonly IDialogService _dialogService;

        // List
        public ObservableCollection<ShiftConfigurationModel> Shifts { get; } = new();

        // Form Fields (Bound to Add/Edit UI)
        private int _editId = 0; // 0 means new
        private string _shiftName;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private bool _isActive = true;

        public string ShiftName { get => _shiftName; set => SetProperty(ref _shiftName, value); }
        public TimeSpan StartTime { get => _startTime; set => SetProperty(ref _startTime, value); }
        public TimeSpan EndTime { get => _endTime; set => SetProperty(ref _endTime, value); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearFormCommand { get; }

        public ShiftConfigurationViewModel(IShiftManagementService shiftService, IAppLogger logger,
            IDialogService dialogService): base(logger)
        {
            _shiftService = shiftService;
            _dialogService = dialogService;

            SaveCommand = new RelayCommand(SaveShift);
            EditCommand = new RelayCommand<ShiftConfigurationModel>(EditShift);
            DeleteCommand = new RelayCommand<ShiftConfigurationModel>(DeleteShift);
            ClearFormCommand = new RelayCommand(ClearForm);

            LoadData();
        }

        private async void LoadData()
        {
            var data = await _shiftService.GetAllShiftsAsync();
            Shifts.Clear();
            foreach (var s in data) Shifts.Add(s);
        }

        private async void SaveShift()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ShiftName))
                {
                    _dialogService.ShowWarning("Shift Name is required.");
                    return;
                }

                var model = new ShiftConfigurationModel
                {
                    Id = _editId,
                    ShiftName = ShiftName,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    IsActive = IsActive
                };

                if (_editId == 0) // New
                {
                    await _shiftService.AddShiftAsync(model);
                }
                else // Update
                {
                    await _shiftService.UpdateShiftAsync(model);
                }

                ClearForm();
                LoadData(); // Refresh list
                _dialogService.ShowMessage("Shift saved successfully.");
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Error: {ex.Message}", "Error");
            }
        }

        private void EditShift(ShiftConfigurationModel shift)
        {
            try
            {

                if (shift == null) return;
                _editId = shift.Id;
                ShiftName = shift.ShiftName;
                StartTime = shift.StartTime;
                EndTime = shift.EndTime;
                IsActive = shift.IsActive;
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Error: {ex.Message}", "Error");
            }
        }

        private async void DeleteShift(ShiftConfigurationModel shift)
        {
            if (shift == null) return;
            if (_dialogService.ShowYesNo($"Delete shift '{shift.ShiftName}'?", "Confirm Delete"))
            {
                await _shiftService.DeleteShiftAsync(shift.Id);
                LoadData();
            }
        }

        private void ClearForm()
        {
            _editId = 0;
            ShiftName = string.Empty;
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;
            IsActive = true;
        }
    }
}