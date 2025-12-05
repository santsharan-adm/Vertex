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
    public class PLCTagConfigurationService : IPLCTagConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<PLCTagConfigurationModel> _tags;
        private int _nextId = 1;

        public PLCTagConfigurationService(string dataFolderPath = null)
        {
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _csvFilePath = Path.Combine(_dataFolder, "PLCTags.csv");
            _tags = new List<PLCTagConfigurationModel>();
        }

        public async Task InitializeAsync()
        {
            await LoadFromCsvAsync();
        }

        public async Task<List<PLCTagConfigurationModel>> GetAllTagsAsync()
        {
            // If tags are empty, try loading again just in case
            if (_tags.Count == 0)
            {
                await LoadFromCsvAsync();
            }
            return _tags.ToList();
        }

        public async Task<PLCTagConfigurationModel> GetTagByIdAsync(int id)
        {
            return await Task.FromResult(_tags.FirstOrDefault(t => t.Id == id));
        }

        public async Task<PLCTagConfigurationModel> AddTagAsync(PLCTagConfigurationModel tag)
        {
            tag.Id = _nextId++;
            _tags.Add(tag);
            await SaveToCsvAsync();
            return tag;
        }

        public async Task<bool> UpdateTagAsync(PLCTagConfigurationModel tag)
        {
            var existing = _tags.FirstOrDefault(t => t.Id == tag.Id);
            if (existing == null) return false;

            var index = _tags.IndexOf(existing);
            _tags[index] = tag;
            await SaveToCsvAsync();
            return true;
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            var tag = _tags.FirstOrDefault(t => t.Id == id);
            if (tag == null) return false;

            _tags.Remove(tag);
            await SaveToCsvAsync();
            return true;
        }

        private async Task LoadFromCsvAsync()
        {
            if (!File.Exists(_csvFilePath))
            {
                await SaveToCsvAsync(); // Create empty file with headers
                return;
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(_csvFilePath);
                if (lines.Length <= 1) return;

                _tags.Clear();
                // Start from i=1 to skip header
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    var tag = ParseCsvLine(lines[i]);
                    if (tag != null)
                    {
                        _tags.Add(tag);
                        if (tag.Id >= _nextId)
                            _nextId = tag.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading PLC tags CSV: {ex.Message}");
            }
        }

        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,Description,Remark");

                foreach (var tag in _tags)
                {
                    // For DataType, we save it as a number or string? 
                    // To match your input CSV format, let's save it as the text representation if you prefer,
                    // or keep it simple integers. If the input has "Bit", we should probably write "Bit" back 
                    // OR stick to integers if your system prefers consistency. 
                    // Below saves as Integers to be safe, unless you specifically need "Bit" in the text file.

                    sb.AppendLine($"{tag.Id}," +
                        $"{tag.TagNo}," +
                        $"\"{EscapeCsv(tag.Name)}\"," +
                        $"{tag.PLCNo}," +
                        $"{tag.ModbusAddress}," +
                        $"{tag.Length}," +
                        $"{tag.AlgNo}," +
                        $"{tag.DataType}," + // Saving as int (e.g. 3 for Bit)
                        $"{tag.BitNo}," +
                        $"{tag.Offset}," +
                        $"{tag.Span}," +
                        $"\"{EscapeCsv(tag.Description)}\"," +
                        $"\"{EscapeCsv(tag.Remark)}\"");
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving PLC tags CSV: {ex.Message}");
            }
        }

        private PLCTagConfigurationModel ParseCsvLine(string line)
        {
            try
            {
                var values = SplitCsvLine(line);
                // We expect at least 11 columns, but your CSV has 13.
                if (values.Count < 11) return null;

                return new PLCTagConfigurationModel
                {
                    Id = ParseIntSafe(values[0]),
                    TagNo = ParseIntSafe(values[1]),
                    Name = values[2],
                    PLCNo = ParseIntSafe(values[3]),
                    ModbusAddress = ParseIntSafe(values[4]),
                    Length = ParseIntSafe(values[5]),
                    AlgNo = ParseIntSafe(values[6]),

                    // FIX: Use ParseDataType instead of int.Parse
                    DataType = ParseDataType(values[7]),

                    BitNo = ParseIntSafe(values[8]),
                    Offset = ParseIntSafe(values[9]),
                    Span = ParseIntSafe(values[10]),
                    Description = values.Count > 11 ? values[11] : "",
                    Remark = values.Count > 12 ? values[12] : ""
                };
            }
            catch
            {
                return null;
            }
        }

        // --- Helper Methods ---

        private int ParseIntSafe(string s)
        {
            if (int.TryParse(s, out int result)) return result;
            return 0;
        }


        private int ParseDataType(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 1; // Default Int16

            // Check if it's already a number
            if (int.TryParse(s, out int result)) return result;

            s = s.Trim().ToLowerInvariant();

            return s switch
            {
                "int" => 1,
                "int16" => 1,
                "word" => 2,
                "dint" => 2,
                "bit" => 3,     // THIS FIXES YOUR ISSUE
                "bool" => 3,
                "fp" => 4,
                "float" => 4,
                "string" => 5,
                _ => 1
            };
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
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains("\"")) return value.Replace("\"", "\"\"");
            return value;
        }
    }
}