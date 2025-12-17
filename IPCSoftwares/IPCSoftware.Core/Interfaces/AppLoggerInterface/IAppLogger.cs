
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces.AppLoggerInterface
{
    public interface IAppLogger
    {
        void LogInfo(string message, LogType type);
        void LogWarning(string message, LogType type);
        void LogError(
        string message,
        LogType type, 
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);
    }
}
