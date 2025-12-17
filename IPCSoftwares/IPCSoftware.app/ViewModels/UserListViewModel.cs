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

namespace IPCSoftware.App.ViewModels
{
    public class UserListViewModel : BaseViewModel
    {
        private readonly IUserManagementService _userService;
        private readonly INavigationService _nav;
        private ObservableCollection<UserConfigurationModel> _users;
        private ObservableCollection<UserConfigurationModel> _filteredUsers;
        private UserConfigurationModel _selectedUser;
        private string _searchText;

        public ObservableCollection<UserConfigurationModel> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public ObservableCollection<UserConfigurationModel> FilteredUsers
        {
            get => _filteredUsers;
            set => SetProperty(ref _filteredUsers, value);
        }

        public UserConfigurationModel SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public UserListViewModel(
            IUserManagementService userService, 
            INavigationService nav,
        IAppLogger logger) : base(logger)
        {
            _userService = userService;
            _nav = nav;
            Users = new ObservableCollection<UserConfigurationModel>();
            FilteredUsers = new ObservableCollection<UserConfigurationModel>();

            AddUserCommand = new RelayCommand(OnAddUser);
            EditUserCommand = new RelayCommand<UserConfigurationModel>(OnEditUser);
            DeleteUserCommand = new RelayCommand<UserConfigurationModel>(OnDeleteUser);

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                Users.Clear();
                FilteredUsers.Clear();

                foreach (var user in users)
                {
                    Users.Add(user);
                    FilteredUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void ApplyFilter()
        {
            FilteredUsers.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var user in Users)
                {
                    FilteredUsers.Add(user);
                }
            }
            else
            {
                var filtered = Users.Where(u =>
                    (u.FirstName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.LastName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.UserName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Role?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                );

                foreach (var user in filtered)
                {
                    FilteredUsers.Add(user);
                }
            }
        }

        private void OnAddUser()
        {
            _nav.NavigateToUserConfiguration(null, async () =>
            {
                await LoadDataAsync();
            });
        }

        private void OnEditUser(UserConfigurationModel user)
        {
            if (user == null) return;

            _nav.NavigateToUserConfiguration(user, async () =>
            {
                await LoadDataAsync();
            });
        }

        private async void OnDeleteUser(UserConfigurationModel user)
        {
            try
            {
                if (user == null) return;

                // TODO: Add confirmation dialog
                await _userService.DeleteUserAsync(user.Id);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }
    }
}
