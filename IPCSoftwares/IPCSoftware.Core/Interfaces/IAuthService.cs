using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IAuthService
    {
        (bool Success, string Role) Login(string username, string password);
    }
}

