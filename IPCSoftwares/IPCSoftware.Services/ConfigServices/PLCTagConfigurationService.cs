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
            return await Task.FromResult(_tags.ToList());
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
                await SaveToCsvAsync();
                return;
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(_csvFilePath);
                if (lines.Length <= 1) return;

                _tags.Clear();
                for (int i = 1; i < lines.Length; i++)
                {
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
                Console.WriteLine($"Error loading PLC tags CSV: {ex.Message}");
            }
        }

        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgNo,Offset,Span,Description,Remark");

                foreach (var tag in _tags)
                {
                    sb.AppendLine($"{tag.Id}," +
                        $"{tag.TagNo}," +
                        $"\"{EscapeCsv(tag.Name)}\"," +
                        $"{tag.PLCNo}," +
                        $"\"{EscapeCsv(tag.ModbusAddress)}\"," +
                        $"{tag.Length}," +
                        $"{tag.AlgNo}," +  // NO QUOTES - it's an int
                        $"{tag.Offset}," +
                        $"{tag.Span}," +
                        $"\"{EscapeCsv(tag.Description)}\"," +
                        $"\"{EscapeCsv(tag.Remark)}\"");
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving PLC tags CSV: {ex.Message}");
                throw;
            }
        }

        private PLCTagConfigurationModel ParseCsvLine(string line)
        {
            try
            {
                var values = SplitCsvLine(line);
                if (values.Count < 11) return null;

                return new PLCTagConfigurationModel
                {
                    Id = int.Parse(values[0]),
                    TagNo = int.Parse(values[1]),
                    Name = values[2],
                    PLCNo = int.Parse(values[3]),
                    ModbusAddress = values[4],
                    Length = int.Parse(values[5]),
                    AlgNo = int.Parse(values[6]),  // NOW PARSING AS INT
                    Offset = int.Parse(values[7]),
                    Span = int.Parse(values[8]),
                    Description = values[9],
                    Remark = values[10]
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
