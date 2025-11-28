using IPCSoftware.AppLogger.Models;
using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Role)> LoginAsync(string username, string password);

        Task EnsureDefaultUserExistsAsync();
    }

    public interface ILogService
    {
        Task<List<LogFileInfo>> GetLogFilesAsync(LogType category);
        Task<List<LogEntry>> ReadLogFileAsync(string filePath);
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
    }

    public class LogService : ILogService
    {
        // Base path where your logs are stored
        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory + "Logs";

        public async Task<List<LogFileInfo>> GetLogFilesAsync(LogType category)
        {
            return await Task.Run(() =>
            {
                var folderPath = Path.Combine(_basePath, category.ToString());
                if (!Directory.Exists(folderPath)) return new List<LogFileInfo>();

                var directory = new DirectoryInfo(folderPath);

                // Get CSV or TXT files, sort by newest first
                return directory.GetFiles("*.csv")
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new LogFileInfo
                    {
                        FileName = f.Name,
                        FullPath = f.FullName,
                        LastModified = f.LastWriteTime,
                        DisplaySize = $"{f.Length / 1024} KB"
                    }).ToList();
            });
        }

        public async Task<List<LogEntry>> ReadLogFileAsync(string filePath)
        {
            var logs = new List<LogEntry>();

            if (!File.Exists(filePath)) return logs;

            var lines = await File.ReadAllLinesAsync(filePath);

            // Skip Header Row (index 0)
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Basic CSV Split (Assuming structure: Time,Level,"Msg",Source)
                // Note: For complex CSVs, use CsvHelper library. 
                // This manual split handles the quotes in your example.
                var parts = SplitCsvLine(line);

                if (parts.Count >= 4)
                {
                    DateTime.TryParse(parts[0], out var date);
                    logs.Add(new LogEntry
                    {
                        Timestamp = date,
                        Level = parts[1],
                        Message = parts[2].Trim('"'), // Remove quotes
                        Source = parts[3]
                    });
                }
            }

            return logs.OrderByDescending(x => x.Timestamp).ToList();
        }

        private List<string> SplitCsvLine(string line)
        {
            // Simple logic to split by comma but ignore commas inside quotes
            var result = new List<string>();
            bool inQuotes = false;
            string currentToken = "";

            foreach (char c in line)
            {
                if (c == '\"') inQuotes = !inQuotes; // Toggle quote state
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentToken);
                    currentToken = "";
                }
                else
                {
                    currentToken += c;
                }
            }
            result.Add(currentToken);
            return result;
        }
    }



}

