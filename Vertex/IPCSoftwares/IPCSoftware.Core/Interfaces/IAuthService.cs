
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Role)> LoginAsync(string username, string password);

        Task EnsureDefaultUserExistsAsync();
    }

   

  


}

