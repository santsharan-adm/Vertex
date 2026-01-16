using IPCSoftware.Shared.Models.ApiTest;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IApiTestSettingsService
    {
        Task<ApiTestSettings> LoadAsync();
        Task SaveAsync(ApiTestSettings settings);
    }
}
