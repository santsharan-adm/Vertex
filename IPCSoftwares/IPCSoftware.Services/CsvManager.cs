using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace IPCSoftware.Services
{
    public class CsvManager : IFileHandler
    {
        private readonly IAppLogger _logger;

        public CsvManager(IAppLogger logger)
        {
            _logger = logger;
        }
        public  string Getversion(string filepath)
        {
            if (!File.Exists(filepath)) 
                return "1.0";

            using var reader = new StreamReader(filepath, detectEncodingFromByteOrderMarks: true);
            var header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header)) 
                return "1.0";

            string[] temp = header.Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length > 1) 
                return temp[1].Trim();

            return "1.0";
        }

        public List<string[]> Read(string filePath)
        {
            var rows = new List<string[]>();

            if (!File.Exists(filePath)) 
                return rows;

            using (var reader = new StreamReader(filePath))
            {
                bool isHeader = true;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }
                    if (line.ToLower().StartsWith("id")) 
                        continue;
                    if (string.IsNullOrWhiteSpace(line)) 
                        continue;

                    // FIX: Don't use line.Split(',')
                    // Use Regex to split only on commas that are NOT inside quotes
                    string[] parts = SplitCsvLine(line);

                    rows.Add(parts);
                }
            }

            return rows;
        }

        private string[] SplitCsvLine(string line)
        {
            // This Regex finds commas that are followed by an even number of quotes (meaning outside of a string)
            string pattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";

            string[] rawParts = Regex.Split(line, pattern);

            // Clean up the quotes from the results
            for (int i = 0; i < rawParts.Length; i++)
            {
                string part = rawParts[i].Trim();

                // If it starts and ends with quotes, remove them and unescape double quotes
                if (part.StartsWith("\"") && part.EndsWith("\"") && part.Length >= 2)
                {
                    part = part.Substring(1, part.Length - 2).Replace("\"\"", "\"");
                }
                rawParts[i] = part;
            }

            return rawParts;
        }

        // Added by Rishabh - Date 17/04/2026
        public string GetHeader(string filepath)
        {
            if (!File.Exists(filepath)) 
                return string.Empty;

            string header = null;
            using (var reader = new StreamReader(filepath))
            {
                while (!reader.EndOfStream)
                {
                    header = reader.ReadLine();
                    if (header.ToLower().StartsWith("id")) 
                        break;
                }
            }
            return header;
        }

        // Added by Rishabh - date - 19/04/2026//
        public string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains("\""))
                return value.Replace("\"", "\"\"");

            return value;
        }


        //Added by Rishabh - date - 25/04/2026//
        //* WriteCsv method to write content to a CSV file with UTF-8 encoding and error handling//
        public async Task WriteCsv(string filepath, string content)
        {
            try
            {
                await File.WriteAllTextAsync(filepath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to write CSV file : {ex.Message}", LogType.Error);

            }
        }
    }
}