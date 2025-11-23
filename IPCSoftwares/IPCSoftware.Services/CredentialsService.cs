
using System.IO;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models;
using System.Text.Json;

namespace IPCSoftware.Services
{
    public class CredentialsService : ICredentialsService
    {
        private readonly string _path;

        public CredentialsService()
        {
            _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigDB", "users.json");
        }

        public List<UserModel> LoadUsers()
        {
            if (!File.Exists(_path))
                return new List<UserModel>();

            var json = File.ReadAllText(_path);
            System.Diagnostics.Debug.WriteLine("JSON RAW: " + json);

            var list = JsonSerializer.Deserialize<List<UserModel>>(json);
            System.Diagnostics.Debug.WriteLine("DESERIALIZED COUNT: " + list?.Count);

            return list ?? new List<UserModel>();
        }

        public void SaveUsers(List<UserModel> users)
        {
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
    }
}

