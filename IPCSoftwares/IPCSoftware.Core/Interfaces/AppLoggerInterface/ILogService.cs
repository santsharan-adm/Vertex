using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces.AppLoggerInterface
{
    public interface ILogService
    {
        Task<List<LogFileInfo>> GetLogFilesAsync(LogType category);
        Task<List<LogEntry>> ReadLogFileAsync(string filePath);
    }
}
