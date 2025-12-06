using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;


namespace IPCSoftware.Services.ConfigServices
{
    public class PLCTagConfigurationService : IPLCTagConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<PLCTagConfigurationModel> _tags;
        private int _nextId = 1;
        private readonly TagConfigLoader _tagLoader = new TagConfigLoader(); // Use the dedicated loader

        // FIX: Constructor uses IConfiguration for path resolution
        public PLCTagConfigurationService(IConfiguration configuration)
        {
            // Use GetValue<T> extension method 
            string dataFolderName = configuration.GetValue<string>("Config:DataFolder") ?? "Data";
            string tagFileName = configuration.GetValue<string>("Config:PlcTagsFileName") ?? "PLCTags.csv";

            var appRootPath = AppContext.BaseDirectory;
            var appDataFolder = Path.Combine(appRootPath, dataFolderName);

            _dataFolder = appDataFolder;
            _csvFilePath = Path.Combine(_dataFolder, tagFileName);

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _tags = new List<PLCTagConfigurationModel>();
        }

        public async Task InitializeAsync()
        {
            await LoadTagsInternalAsync();
        }

        public async Task<List<PLCTagConfigurationModel>> GetAllTagsAsync()
        {
            if (_tags.Count == 0)
            {
                await LoadTagsInternalAsync();
            }
            return _tags.ToList();
        }

        // FIX CS0535: IMPLEMENT THE REQUIRED METHOD FOR DYNAMIC RELOAD
        public async Task<List<PLCTagConfigurationModel>> ReloadTagsAsync()
        {
            await LoadTagsInternalAsync();
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

        private async Task LoadTagsInternalAsync()
        {
            if (!File.Exists(_csvFilePath))
            {
                await SaveToCsvAsync();
                return;
            }

            try
            {
                // FIX: Use the dedicated TagConfigLoader (now accessible via using directive)
                var reloadedTags = _tagLoader.Load(_csvFilePath);

                // Thread-safe update of the internal cache list
                _tags = reloadedTags;

                // Update the next ID counter
                if (_tags.Any())
                {
                    _nextId = _tags.Max(t => t.Id) + 1;
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
                // Ensure the header includes the new CanWrite column (14 columns total)
                sb.AppendLine("Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,Description,Remark,CanWrite");

                foreach (var tag in _tags)
                {
                    sb.AppendLine($"{tag.Id}," +
                        $"{tag.TagNo}," +
                        $"\"{EscapeCsv(tag.Name)}\"," + // EscapeCsv is now defined
                        $"{tag.PLCNo}," +
                        $"{tag.ModbusAddress}," +
                        $"{tag.Length}," +
                        $"{tag.AlgNo}," +
                        $"{tag.DataType}," +
                        $"{tag.BitNo}," +
                        $"{tag.Offset}," +
                        $"{tag.Span}," +
                        $"\"{EscapeCsv(tag.Description)}\"," + // EscapeCsv is now defined
                        $"\"{EscapeCsv(tag.Remark)}\"," + // EscapeCsv is now defined
                        $"{tag.CanWrite}");
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving PLC tags CSV: {ex.Message}");
            }
        }

        // FIX 3: Re-introduce the missing helper method
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            // Check if the value contains quotes or a comma, and escape accordingly
            if (value.Contains("\"") || value.Contains(","))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        // Placeholder methods must be defined here to avoid further errors
        // NOTE: These should be implemented fully, but we add them to resolve current compilation issues.

        private List<string> SplitCsvLine(string line)
        {
            // Placeholder: Logic is needed here to split CSV line, considering quotes
            return new List<string>();
        }

        private int ParseIntSafe(string s)
        {
            if (int.TryParse(s, out int result)) return result;
            return 0;
        }

        private int ParseDataType(string s)
        {
            // Placeholder: Logic is needed here to convert type string to int
            if (string.IsNullOrWhiteSpace(s)) return 1;
            if (int.TryParse(s, out int result)) return result;
            return 1; // Default
        }
    }
}