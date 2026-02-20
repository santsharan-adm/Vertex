
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace IPCSoftware.Services.AppLoggerServices
{
    public class AppLoggerService : IAppLogger, IDisposable
    {
        private readonly ILogManagerService _logManager;

        private readonly BlockingCollection<LogEntry> _logQueue;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processTask;

        private struct LogEntry
        {
            public string Level { get; set; }
            public string Message { get; set; }
            public LogType Type { get; set; }
            public DateTime Timestamp { get; set; }
        }


        public AppLoggerService(ILogManagerService logManager)
        {
            _logManager = logManager;
            _logQueue = new BlockingCollection<LogEntry>();
            _cts = new CancellationTokenSource();

            // Start the background listening thread
            // LongRunning hint tells scheduler to create a dedicated thread for this loop
            _processTask = Task.Factory.StartNew(ProcessLogQueue, TaskCreationOptions.LongRunning);
        }

        // Public APIs
        public void LogInfo(string message, LogType type) => EnqueueLog("INFO", message, type);
        public void LogWarning(string message, LogType type) => EnqueueLog("WARN", message, type);
        //public void LogError(string message, LogType type) => EnqueueLog("ERROR", message, type);

        public void LogError(
        string message,
        LogType type,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        {
            string finalMessage = message;

            // Add caller info ONLY for Diagnostics
            if (type == LogType.Diagnostics)
            {
                string className = Path.GetFileNameWithoutExtension(filePath);
                finalMessage = $"[{className}.{memberName}() Line:{lineNumber}] : {message}";
            }

            EnqueueLog("ERROR", finalMessage, type);
        }


        private void EnqueueLog(string level, string message, LogType type)
        {
            if (!_cts.IsCancellationRequested)
            {
                _logQueue.Add(new LogEntry
                {
                    Level = level,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.Now // Capture time when requested, not when written
                });
            }
        }

        // --- Background Consumer ---
        private void ProcessLogQueue()
        {
            // This loop runs until the app shuts down
            foreach (var entry in _logQueue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    WriteToFileSafe(entry);
                }
                catch (Exception ex)
                {
                    // Fallback: If logging fails entirely, write to Debug console so we don't lose the error
                    System.Diagnostics.Debug.WriteLine($"[CRITICAL LOGGER FAIL] {ex.Message}");
                }
            }
        }

        private void WriteToFileSafe(LogEntry entry)
        {
            var config = _logManager.GetConfig(entry.Type);
            if (config == null || !config.Enabled) return;

            string filePath = _logManager.ResolveLogFile(entry.Type);
            if (string.IsNullOrEmpty(filePath)) return;

            // Perform maintenance (Purging/Size Check) logic from LogManager
            // Doing this here ensures it happens on the background thread, not UI thread
          //  _logManager.ApplyMaintenance(config, filePath);

            string line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss:fff},{entry.Level},\"{entry.Message}\",{config.LogName}{Environment.NewLine}";

            // RETRY POLICY: Handles the case where 'CoreService' and 'App' try to write simultaneously.
            // We try 3 times with a small delay.
            const int MaxRetries = 3;
            const int DelayOnRetryMs = 50;

            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    File.AppendAllText(filePath, line);
                    return; // Success
                }
                catch (IOException)
                {
                    // File is locked by another process (e.g., the other App). 
                    // Wait briefly and try again.
                    if (i == MaxRetries - 1)
                    {
                        // Final failure: Output to debug/console
                        System.Diagnostics.Debug.WriteLine($"[LOGGER LOCKED] Could not write to {filePath} after retries.");
                    }
                    else
                    {
                        Thread.Sleep(DelayOnRetryMs);
                    }
                }
                catch (Exception)
                {
                    // Other errors (permissions, path invalid) - break loop
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (_cts.IsCancellationRequested) return;

            // Signal the queue to stop accepting new items
            _logQueue.CompleteAdding();
            _cts.Cancel();

            try
            {
                // Wait for the queue to drain remaining logs before dying
                _processTask.Wait(1000);
            }
            catch (AggregateException) { /* Ignore cancellation errors */ }

            _cts.Dispose();
            _logQueue.Dispose();
        }


    }


}
