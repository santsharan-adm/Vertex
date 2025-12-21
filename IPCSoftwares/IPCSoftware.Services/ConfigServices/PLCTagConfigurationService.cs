using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Configuration;
using System.IO;
using System.Text;


namespace IPCSoftware.Services.ConfigServices
{
    public class PLCTagConfigurationService : BaseService, IPLCTagConfigurationService
    {
        //private readonly IConfiguration _configuration;
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<PLCTagConfigurationModel> _tags;
        private int _nextId = 1;
        private readonly TagConfigLoader _tagLoader ; // Use the dedicated loader
            
        public PLCTagConfigurationService(
            IOptions<ConfigSettings> configSettings,
            TagConfigLoader tagConfigLoader,
            IAppLogger logger) : base(logger) 
        {
            _tagLoader = tagConfigLoader;
            //    _configuration = configuration;
            //  string dataFolderPath = _configuration.GetValue<string>("Config:DataFolder");
            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;

            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _csvFilePath = Path.Combine(_dataFolder, config.PlcTagsFileName /*"PLCTags.csv"*/);

            _tags = new List<PLCTagConfigurationModel>();
        }


        public async Task InitializeAsync()
        {
            try
            {
             await LoadTagsInternalAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        public async Task<List<PLCTagConfigurationModel>> GetAllTagsAsync()
        {
            try
            {
                if (_tags.Count == 0)
                {
                    await LoadTagsInternalAsync();
                }
                return _tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        // FIX CS0535: IMPLEMENT THE REQUIRED METHOD FOR DYNAMIC RELOAD
        public async Task<List<PLCTagConfigurationModel>> ReloadTagsAsync()
        {
            try
            {
                await LoadTagsInternalAsync();
                return _tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return null;
            }
        }

        public async Task<PLCTagConfigurationModel> GetTagByIdAsync(int id)
        {
            return await Task.FromResult(_tags.FirstOrDefault(t => t.Id == id));
        }

        public async Task<PLCTagConfigurationModel> AddTagAsync(PLCTagConfigurationModel tag)
        {
            try
            {
                tag.Id = _nextId++;
                _tags.Add(tag);
                await SaveToCsvAsync();
                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        public async Task<bool> UpdateTagAsync(PLCTagConfigurationModel tag)
        {
            try
            {
                var existing = _tags.FirstOrDefault(t => t.Id == tag.Id);
                if (existing == null) return false;

                var index = _tags.IndexOf(existing);
                _tags[index] = tag;
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            try
            {
                var tag = _tags.FirstOrDefault(t => t.Id == id);
                if (tag == null) return false;

                _tags.Remove(tag);
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        private async Task LoadTagsInternalAsync()
        {
            try
            {
            if (!File.Exists(_csvFilePath))
            {
                await SaveToCsvAsync();
                return;
            }

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
               _logger.LogError($"Error loading PLC tags CSV: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                // Header
                sb.AppendLine("Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,Description,Remark,CanWrite");

                foreach (var tag in _tags)
                {
                    // CHANGE HERE: Removed the \" before and after the function calls.
                    // We trust EscapeCsv to add quotes ONLY if necessary.
                    sb.AppendLine($"{tag.Id}," +
                        $"{tag.TagNo}," +
                        $"{EscapeCsv(tag.Name)}," +         // <--- Was $"\"{EscapeCsv(tag.Name)}\","
                        $"{tag.PLCNo}," +
                        $"{tag.ModbusAddress}," +
                        $"{tag.Length}," +
                        $"{tag.AlgNo}," +
                        $"{GetDataTypeString(tag.DataType)}," + // Helper to convert int back to string (e.g. 1 -> Int16)
                        $"{tag.BitNo}," +
                        $"{tag.Offset}," +
                        $"{tag.Span}," +
                        $"{EscapeCsv(tag.Description)}," +  // <--- Was $"\"{EscapeCsv(tag.Description)}\","
                        $"{EscapeCsv(tag.Remark)}," +       // <--- Was $"\"{EscapeCsv(tag.Remark)}\","
                        $"{tag.CanWrite}");
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving PLC tags CSV: {ex.Message}", LogType.Diagnostics);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            // ONLY add quotes if the value contains a comma or a quote
            if (value.Contains("\"") || value.Contains(","))
            {
                // Double up any existing quotes and wrap the whole thing in quotes
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            // Otherwise, return the clean string without quotes
            return value;
        }

        // You likely need this helper to save "Int16" instead of "1" back to the CSV
        private string GetDataTypeString(int typeId)
        {
            return typeId switch
            {
                1 => "Int16",
                2 => "Word",
                3 => "Bit",
                4 => "Float",
                5 => "String",
                _ => "Int16"
            };
        }


    }
}