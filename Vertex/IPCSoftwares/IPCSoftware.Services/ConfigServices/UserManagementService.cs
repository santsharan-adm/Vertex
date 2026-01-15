using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    public class UserManagementService : IUserManagementService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<UserConfigurationModel> _users;
        private int _nextId = 1;


        public UserManagementService(string dataFolderPath = null)
        {
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _csvFilePath = Path.Combine(_dataFolder, "Users.csv");
            _users = new List<UserConfigurationModel>();
        }

        public async Task InitializeAsync()
        {
            await LoadFromCsvAsync();
        }

        public async Task<List<UserConfigurationModel>> GetAllUsersAsync()
        {
            return await Task.FromResult(_users.ToList());
        }

        public async Task<UserConfigurationModel> GetUserByIdAsync(int id)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }

        public async Task<UserConfigurationModel> GetUserByUsernameAsync(string username)
        {
            return await Task.FromResult(_users.FirstOrDefault(u =>
                u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<UserConfigurationModel> AddUserAsync(UserConfigurationModel user)
        {
            // 1. Check for Duplicate Username
            bool usernameExists = _users.Any(u => u.UserName.Equals(user.UserName, StringComparison.OrdinalIgnoreCase));

            if (usernameExists)
            {
                // Return null or throw an exception to indicate failure
                throw new InvalidOperationException("This Username is already taken.");
            }

            // 2. ID is auto-generated, ensuring uniqueness
            user.Id = _nextId++;

            _users.Add(user);
            await SaveToCsvAsync();
            return user;
        }

        public async Task<bool> UpdateUserAsync(UserConfigurationModel user)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null) return false;

            // 1. Check for Duplicate Username (excluding the current user being edited)
            bool usernameTaken = _users.Any(u => u.Id != user.Id &&
                                                 u.UserName.Equals(user.UserName, StringComparison.OrdinalIgnoreCase));

            if (usernameTaken)
            {
                throw new InvalidOperationException("This Username is already taken by another user.");
            }

            var index = _users.IndexOf(existing);
            _users[index] = user;
            await SaveToCsvAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;

            _users.Remove(user);
            await SaveToCsvAsync();
            return true;
        }

        private async Task LoadFromCsvAsync()
        {
            if (!File.Exists(_csvFilePath))
            {
                await SaveToCsvAsync();
                return;
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(_csvFilePath);
                if (lines.Length <= 1) return;

                _users.Clear();
                for (int i = 1; i < lines.Length; i++)
                {
                    var user = ParseCsvLine(lines[i]);
                    if (user != null)
                    {
                        _users.Add(user);
                        if (user.Id >= _nextId)
                            _nextId = user.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading users CSV: {ex.Message}");
            }
        }

        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,FirstName,LastName,UserName,Password,Role,IsActive");

                foreach (var user in _users)
                {
                    sb.AppendLine($"{user.Id}," +
                        $"\"{EscapeCsv(user.FirstName)}\"," +
                        $"\"{EscapeCsv(user.LastName)}\"," +
                        $"\"{EscapeCsv(user.UserName)}\"," +
                        $"\"{EscapeCsv(user.Password)}\"," +
                        $"\"{EscapeCsv(user.Role)}\"," +
                        $"{user.IsActive}");
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving users CSV: {ex.Message}");
                throw;
            }
        }

        private UserConfigurationModel ParseCsvLine(string line)
        {
            try
            {
                var values = SplitCsvLine(line);
                if (values.Count < 7) return null;

                return new UserConfigurationModel
                {
                    Id = int.Parse(values[0]),
                    FirstName = values[1],
                    LastName = values[2],
                    UserName = values[3],
                    Password = values[4],
                    Role = values[5],
                    IsActive = bool.Parse(values[6])
                };
            }
            catch
            {
                return null;
            }
        }

        private List<string> SplitCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains("\""))
                return value.Replace("\"", "\"\"");

            return value;
        }
    }
}
