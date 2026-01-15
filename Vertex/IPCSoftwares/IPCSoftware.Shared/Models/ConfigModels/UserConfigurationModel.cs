using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class UserConfigurationModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }

        public UserConfigurationModel()
        {
            IsActive = true;
            Role = "User";
        }

        public UserConfigurationModel Clone()
        {
            return new UserConfigurationModel
            {
                Id = this.Id,
                FirstName = this.FirstName,
                LastName = this.LastName,
                UserName = this.UserName,
                Password = this.Password,
                Role = this.Role,
                IsActive = this.IsActive
            };
        }
    }
}
