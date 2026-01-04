using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.ViewModels
{
    public class UserConfigurationViewModel : BaseViewModel
    {
        private readonly IUserManagementService _userService;
        private readonly IDialogService _dialog;
        private UserConfigurationModel _currentUser;
        private bool _isEditMode;
        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // Properties
        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _selectedRole;
        public string SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public ObservableCollection<string> Roles { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public UserConfigurationViewModel(
            IUserManagementService userService, 
            IDialogService dialog,
        IAppLogger logger) : base(logger)
        {
            _userService = userService;
            _dialog = dialog;

            Roles = new ObservableCollection<string> { "Admin", "Supervisor", "Operator", "Viewer" };

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);

            InitializeNewUser();
        }

        public void InitializeNewUser()
        {
            Title = "User Details - New";
            IsEditMode = false;
            _currentUser = new UserConfigurationModel();
            LoadFromModel(_currentUser);
        }

        public void LoadForEdit(UserConfigurationModel user)
        {
            Title = "User Details - Edit";
            IsEditMode = true;
            _currentUser = user.Clone();
            LoadFromModel(_currentUser);
        }

        private void LoadFromModel(UserConfigurationModel user)
        {
            try
            {
                FirstName = user.FirstName;
                LastName = user.LastName;
                UserName = user.UserName;
                Password = user.Password;
                SelectedRole = user.Role ?? "User";
                IsActive = user.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void SaveToModel()
        {
            try
            {
                _currentUser.FirstName = FirstName;
                _currentUser.LastName = LastName;
                _currentUser.UserName = UserName;
                _currentUser.Password = Password;
                _currentUser.Role = SelectedRole;
                _currentUser.IsActive = IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);

        }

        private async Task OnSaveAsync()
        {
            ErrorMessage = string.Empty;

            SaveToModel();

            try
            {
                if (IsEditMode)
                {
                    await _userService.UpdateUserAsync(_currentUser);
                }
                else
                {
                    await _userService.AddUserAsync(_currentUser);
                }

                // Only close/complete if successful
                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (InvalidOperationException ex)
            {
                // Capture the "Username taken" message and show it in the UI

                _logger.LogError(ex.Message, LogType.Diagnostics);
                _dialog.ShowWarning(ex.Message);    
                
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                // Handle generic errors
               // ErrorMessage = "An unexpected error occurred: " + ex.Message;
            }
        }

        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
