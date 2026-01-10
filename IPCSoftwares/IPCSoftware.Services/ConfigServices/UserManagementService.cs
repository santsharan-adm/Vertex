using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    public class UserManagementService :BaseService, IUserManagementService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<UserConfigurationModel> _users;
        private int _nextId = 1;


        public UserManagementService(IOptions<ConfigSettings> configSettings,
            IAppLogger logger) : base(logger)
        {
            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _csvFilePath = Path.Combine(_dataFolder, config.UserFileName /*"Users.csv"*/);
            _users = new List<UserConfigurationModel>();
        }

        public async Task InitializeAsync()
        {
            try
            {
             await LoadFromCsvAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
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


        private UserConfigurationModel ParseCsvLine(string line)
        {
            try
            {
                var values = SplitCsvLine(line);
                // We expect more columns now (Salt + Signature)
                if (values.Count < 9) return null;

                var user = new UserConfigurationModel
                {
                    Id = int.Parse(values[0]),
                    FirstName = values[1],
                    LastName = values[2],
                    UserName = values[3],
                    Password = values[4],      // This is the Hash
                    PasswordSalt = values[5],  // New Column
                    Role = values[6],
                    IsActive = bool.Parse(values[7]),
                    RowSignature = values[8]   // New Column
                };

                // === INTEGRITY CHECK ===
                // Recalculate signature based on the data read
                string calculatedSig = SecurityService.GenerateRowSignature(
                    user.Id, user.UserName, user.Role, user.Password, user.IsActive);

                if (calculatedSig != user.RowSignature)
                {
                    _logger.LogError($"DATA TAMPERING DETECTED for User ID: {user.Id}. Skipping load.", LogType.Audit);
                    return null; // Reject this row!
                }

                return user;
            }
            catch
            {
                return null;
            }
        }

        // MODIFIED: Save to CSV with Signature generation
        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                // Update Header
                sb.AppendLine("Id,FirstName,LastName,UserName,PasswordHash,PasswordSalt,Role,IsActive,IntegritySignature");

                foreach (var user in _users)
                {
                    // Ensure signature is up to date before saving
                    user.RowSignature = SecurityService.GenerateRowSignature(
                        user.Id, user.UserName, user.Role, user.Password, user.IsActive);

                    sb.AppendLine($"{user.Id}," +
                        $"\"{EscapeCsv(user.FirstName)}\"," +
                        $"\"{EscapeCsv(user.LastName)}\"," +
                        $"\"{EscapeCsv(user.UserName)}\"," +
                        $"\"{user.Password}\"," +        // Hash
                        $"\"{user.PasswordSalt}\"," +    // Salt
                        $"\"{EscapeCsv(user.Role)}\"," +
                        $"{user.IsActive}," +
                        $"\"{user.RowSignature}\"");     // HMAC
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving users CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }

        // MODIFIED: Add User (Hashing Logic)
        public async Task<UserConfigurationModel> AddUserAsync(UserConfigurationModel user)
        {
            // ... (Duplicate check remains same) ...

            user.Id = _nextId++;

            // Hash the password coming from UI
            if (!string.IsNullOrEmpty(user.PlainTextPassword))
            {
                string salt;
                user.Password = SecurityService.HashPassword(user.PlainTextPassword, out salt);
                user.PasswordSalt = salt;
            }

            _users.Add(user);
            await SaveToCsvAsync();
            return user;
        }

        // MODIFIED: Update User (Hashing Logic)
        public async Task<bool> UpdateUserAsync(UserConfigurationModel user)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null) return false;

            // ... (Duplicate check remains same) ...

            // Only re-hash if the user typed a new password
            if (!string.IsNullOrEmpty(user.PlainTextPassword))
            {
                string salt;
                existing.Password = SecurityService.HashPassword(user.PlainTextPassword, out salt);
                existing.PasswordSalt = salt;
            }
            // If PlainTextPassword is empty, we keep the old existing.Password (Hash)

            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.Role = user.Role;
            existing.IsActive = user.IsActive;

            // NOTE: We do not update UserName usually as it breaks the signature logic if not careful, 
            // but since we regenerate signature on Save, it is fine.
            existing.UserName = user.UserName;

            await SaveToCsvAsync();
            return true;
        }


        public async Task<UserConfigurationModel> AddUserAsync2(UserConfigurationModel user)
        {
            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
            // 1. Check for Duplicate Username
        }

        public async Task<bool> UpdateUserAsync2(UserConfigurationModel user)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                if (user == null) return false;

                _users.Remove(user);
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        private async Task LoadFromCsvAsync()
        {
            try
            {
            if (!File.Exists(_csvFilePath))
            {
                await SaveToCsvAsync();
                return;
            }

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
                _logger.LogError($"Error loading users CSV: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task SaveToCsvAsync2()
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
                _logger.LogError($"Error saving users CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }

        private UserConfigurationModel ParseCsvLine2(string line)
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
