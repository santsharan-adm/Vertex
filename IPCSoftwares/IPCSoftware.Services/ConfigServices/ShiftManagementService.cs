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
   
    public class ShiftManagementService : BaseService, IShiftManagementService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<ShiftConfigurationModel> _shifts;
        private int _nextId = 1;

        public ShiftManagementService(IOptions<ConfigSettings> configSettings, IAppLogger logger) : base(logger)
        {
            var config = configSettings.Value;
            _dataFolder = config.DataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);

            _csvFilePath = Path.Combine(_dataFolder, "Shifts.csv");
            _shifts = new List<ShiftConfigurationModel>();
        }

        public async Task InitializeAsync()
        {
            await LoadFromCsvAsync();
        }

        public Task<List<ShiftConfigurationModel>> GetAllShiftsAsync() => Task.FromResult(_shifts.ToList());

        // --- MODIFIED: ADD SHIFT WITH UNIQUENESS CHECK ---
        public async Task<ShiftConfigurationModel> AddShiftAsync(ShiftConfigurationModel shift)
        {
            try
            {
                // 1. Validation: Check if ShiftName already exists
                bool nameExists = _shifts.Any(s => s.ShiftName.Equals(shift.ShiftName, StringComparison.OrdinalIgnoreCase));

                if (nameExists)
                {
                    throw new InvalidOperationException($"A shift with the name '{shift.ShiftName}' already exists.");
                }

                // 2. Validation: Ensure Start != End (Basic sanity check)
                if (shift.StartTime == shift.EndTime)
                {
                    throw new InvalidOperationException("Shift Start Time and End Time cannot be exactly the same.");
                }

                shift.Id = _nextId++;
                _shifts.Add(shift);
                await SaveToCsvAsync();
                return shift;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Add Shift Error: {ex.Message}", LogType.Diagnostics);
                throw; // Rethrow so ViewModel can show the message to user
            }
        }

        // --- MODIFIED: UPDATE SHIFT WITH UNIQUENESS CHECK ---
        public async Task<bool> UpdateShiftAsync(ShiftConfigurationModel shift)
        {
            try
            {
                var existing = _shifts.FirstOrDefault(s => s.Id == shift.Id);
                if (existing == null) return false;

                // 1. Validation: Check duplicate name (excluding self)
                bool nameTaken = _shifts.Any(s => s.Id != shift.Id &&
                                                  s.ShiftName.Equals(shift.ShiftName, StringComparison.OrdinalIgnoreCase));

                if (nameTaken)
                {
                    throw new InvalidOperationException($"The shift name '{shift.ShiftName}' is already taken by another shift.");
                }

                if (shift.StartTime == shift.EndTime)
                {
                    throw new InvalidOperationException("Shift Start Time and End Time cannot be exactly the same.");
                }

                existing.ShiftName = shift.ShiftName;
                existing.StartTime = shift.StartTime;
                existing.EndTime = shift.EndTime;
                existing.IsActive = shift.IsActive;

                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update Shift Error: {ex.Message}", LogType.Diagnostics);
                throw; // Rethrow so ViewModel handles it
            }
        }

        public async Task<bool> DeleteShiftAsync(int id)
        {
            try
            {
                var shift = _shifts.FirstOrDefault(s => s.Id == id);
                if (shift == null) return false;
                _shifts.Remove(shift);
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        // ... [LoadFromCsvAsync, SaveToCsvAsync, SplitCsvLine remain the same] ...
        private async Task LoadFromCsvAsync()
        {
            try
            {
                if (!File.Exists(_csvFilePath)) return;

                var lines = await File.ReadAllLinesAsync(_csvFilePath);
                _shifts.Clear();
                _nextId = 1;

                // Skip Header
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = SplitCsvLine(line);
                    if (parts.Count >= 5)
                    {
                        var s = new ShiftConfigurationModel
                        {
                            Id = int.Parse(parts[0]),
                            ShiftName = parts[1],
                            StartTime = TimeSpan.Parse(parts[2]),
                            EndTime = TimeSpan.Parse(parts[3]),
                            IsActive = bool.Parse(parts[4])
                        };
                        _shifts.Add(s);
                        if (s.Id >= _nextId) _nextId = s.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading shifts: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,ShiftName,StartTime,EndTime,IsActive");
                foreach (var s in _shifts)
                {
                    // Escape ShiftName just in case
                    string safeName = s.ShiftName.Replace("\"", "\"\"");
                    sb.AppendLine($"{s.Id},\"{safeName}\",{s.StartTime},{s.EndTime},{s.IsActive}");
                }
                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving shifts: {ex.Message}", LogType.Diagnostics);
                throw;
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
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { currentValue.Append('"'); i++; }
                    else { inQuotes = !inQuotes; }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else { currentValue.Append(c); }
            }
            values.Add(currentValue.ToString());
            return values;
        }
    }
}