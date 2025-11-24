using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IPCSoftware.Common.Models
{
    public class UserRecord
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }

    public class AuthDatabase
    {
        public List<UserRecord> Users { get; set; } = new();
    }
}