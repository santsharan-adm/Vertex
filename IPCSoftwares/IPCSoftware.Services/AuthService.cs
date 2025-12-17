using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System;

namespace IPCSoftware.Services
{
    public class AuthService : BaseService, IAuthService
    {
        private readonly IUserManagementService _userService;

        public AuthService(IUserManagementService userService,
            IAppLogger logger) : base(logger)
        {
            _userService = userService;
        }


    
        public async Task<(bool Success, string Role)> LoginAsync(string username, string password)
        {
            try
            {
                // Get user from CSV
                var user = await _userService.GetUserByUsernameAsync(username);

                if (user == null)
                    return (false, null);

                // Check if user is active
                if (!user.IsActive)
                    return (false, null);

                // Verify password
                if (user.Password != password)
                    return (false, null);

                return (true, user.Role);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}", LogType.Diagnostics);
                return (false, null);
            }
        }

        public async Task EnsureDefaultUserExistsAsync()
        {
            try
            {
                var allUsers = await _userService.GetAllUsersAsync();

                // Check if default admin exists
                var defaultAdmin = allUsers.FirstOrDefault(u =>
                    u.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase));

                if (defaultAdmin == null)
                {
                    // Create default admin user
                    var adminUser = new UserConfigurationModel
                    {
                        FirstName = "System",
                        LastName = "Administrator",
                        UserName = "admin",
                        Password = "admin123",  // Change this to your preferred default password
                        Role = "Admin",
                        IsActive = true
                    };

                    await _userService.AddUserAsync(adminUser);
                    System.Diagnostics.Debug.WriteLine("Default admin user created");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error ensuring default user: {ex.Message}", LogType.Diagnostics);
        
            }
        }

    }

    /*  public (bool Success, string Role) Login(string username, string password)
      {
          var users = _creds.LoadUsers();

          var user = users.FirstOrDefault(u =>u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

          if (user == null) 
              return (false, null);
          if (user.Password != password) 
              return (false, null);
          System.Diagnostics.Debug.WriteLine("Users read");
          return (true, user.Role);


      }*/


}