using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class UserConfigurationViewModel : BaseViewModel
    {
        private readonly IUserManagementService _userService;
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

        public UserConfigurationViewModel(IUserManagementService userService)
        {
            _userService = userService;

            Roles = new ObservableCollection<string> { "Admin", "User", "Operator", "Viewer" };

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
            FirstName = user.FirstName;
            LastName = user.LastName;
            UserName = user.UserName;
            Password = user.Password;
            SelectedRole = user.Role ?? "User";
            IsActive = user.IsActive;
        }

        private void SaveToModel()
        {
            _currentUser.FirstName = FirstName;
            _currentUser.LastName = LastName;
            _currentUser.UserName = UserName;
            _currentUser.Password = Password;
            _currentUser.Role = SelectedRole;
            _currentUser.IsActive = IsActive;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private async Task OnSaveAsync()
        {
            SaveToModel();

            if (IsEditMode)
            {
                await _userService.UpdateUserAsync(_currentUser);
            }
            else
            {
                await _userService.AddUserAsync(_currentUser);
            }

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
