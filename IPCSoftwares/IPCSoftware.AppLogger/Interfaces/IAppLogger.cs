using IPCSoftware.AppLogger.Models;


namespace IPCSoftware.AppLogger.Interfaces
{
    public interface IAppLogger
    {
        void LogInfo(string message, LogType type);
        void LogWarning(string message, LogType type);
        void LogError(string message, LogType type);
    }
}
