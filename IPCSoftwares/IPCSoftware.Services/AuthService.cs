using IPCSoftware.Core.Interfaces;

using System;

namespace IPCSoftware.Services
{
    public class AuthService : IAuthService
    {
        private readonly ICredentialsService _creds;

        public AuthService(ICredentialsService creds)
        {
            _creds = creds;
        }

        public (bool Success, string Role) Login(string username, string password)
        {
            var users = _creds.LoadUsers();

            var user = users.FirstOrDefault(u =>u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null) 
                return (false, null);
            if (user.Password != password) 
                return (false, null);
            System.Diagnostics.Debug.WriteLine("Users read");
            return (true, user.Role);

            
        }
    }
}
