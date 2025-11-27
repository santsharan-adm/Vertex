namespace IPCSoftware.AppLogger.Models
{
    /// <summary>
    /// Represents a single record (row) from a CSV log file.
    /// Each CSV entry contains the timestamp, log level, message, and source.
    /// </summary>
    public class LogRecord
    {
        /// <summary>
        /// Timestamp of the log event (as written in the CSV file).
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Severity level of the log (e.g., Info, Warning, Error).
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// Log message text.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Component or system that produced the log entry.
        /// </summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// Represents a CSV file discovered inside a selected folder.
    /// Includes both the file name and the fully-qualified file path.
    /// </summary>
    public class CsvFileInfo
    {
        /// <summary>
        /// File name only (e.g., "Log_2025-01-12.csv").
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Full absolute path to the file on disk.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Ensures UI controls display only the file name.
        /// </summary>
        public override string ToString() => FileName;
    }

    /// <summary>
    /// Represents a folder inside the root log directory.
    /// Used to group CSV log files by date, category, or device.
    /// </summary>
    public class FolderInfo
    {
        /// <summary>
        /// The folder name only (e.g., "Machine01", "2025-Logs").
        /// </summary>
        public string FolderName { get; set; }

        /// <summary>
        /// Full absolute path to the folder on disk.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Ensures the UI shows only the folder name.
        /// </summary>
        public override string ToString() => FolderName;
    }
}
