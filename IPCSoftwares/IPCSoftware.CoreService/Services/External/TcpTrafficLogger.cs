using IPCSoftware.Shared.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.External
{
    public interface ITcpTrafficLogger
    {
        Task LogTrafficAsync(string direction, string content, string type);
    }

    public class TcpTrafficLogger : ITcpTrafficLogger
    {
        private readonly string _logFilePath;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private readonly IOptionsMonitor<ExternalSettings> _externalSettings;
        private const int MaxEntries = 1000;

        public TcpTrafficLogger(IOptions<ConfigSettings> config, 
            IOptionsMonitor<ExternalSettings> externalSettings)
        {
            _externalSettings = externalSettings;
            var dataFolder = config.Value.DataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            _logFilePath = Path.Combine(dataFolder, "TCP_Traffic.log");
        }

        public async Task LogTrafficAsync(string direction, string content, string type)
        {
            if (!_externalSettings.CurrentValue.EnableTcpTrafficLogging)
            {
                return;
            }
            await _lock.WaitAsync();
            try
            {
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{type}] {direction}: {content}";

                // Read existing lines
                List<string> lines = new List<string>();
                if (File.Exists(_logFilePath))
                {
                    lines = (await File.ReadAllLinesAsync(_logFilePath)).ToList();
                }

                // Add new entry
                lines.Add(entry);

                // Trim if exceeding max entries
                // Assuming "1 count" is one line/entry in this log file. 
                // If content is multi-line, we might want to handle it differently, 
                // but usually TCP logs are single block entries. 
                // For safety with multi-line content, we'll replace newlines or treat the block as one entry if we use a delimiter.
                // Here, I'll append it as is, but manage the "blocks" logic if needed. 
                // For simplicity and robustness given "1000 items", let's assume 1000 *blocks* of logs.
                // To keep it simple and performant: keep last 1000 *lines* or *entries*? 
                // User said "1 count will be total process of one try" -> Query+Response = 1 count? 
                // Or just "1000 items". Let's stick to lines for now or simple appending, 
                // but actually, reading the whole file every time is slow.
                // Optimized approach: Append normally. Rotate file when it gets too big (like 1MB).
                // User specifically asked for "1000 items, remove below part". 
                // "Removing below part" usually means FIFO (remove top/oldest). 

                if (lines.Count > MaxEntries)
                {
                    // Remove oldest (from the top)
                    int removeCount = lines.Count - MaxEntries;
                    lines.RemoveRange(0, removeCount);
                }

                await File.WriteAllLinesAsync(_logFilePath, lines);
            }
            catch
            {
                // Swallow logging errors to not break prod flow
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
