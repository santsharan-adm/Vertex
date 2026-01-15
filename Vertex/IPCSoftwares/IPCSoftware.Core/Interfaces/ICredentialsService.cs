using IPCSoftware.Shared.Models;

namespace IPCSoftware.Core.Interfaces
{
    public interface ICredentialsService
    {
        List<UserModel> LoadUsers();
        void SaveUsers(List<UserModel> users);
    }
}
