/******************************************************************************
 * Project      : IPCSoftware-AOI /Bending
 * Module       : UiErrorLogger
 * File Name    : UiErrorLogger.cs
 * Author       : Rishabh
 * Organization : Motherson Technology Service Limited
 * Created Date : 2026-04-30
 *
 * Description  :
 * Provides a centralized error logging service for the UI components, capturing and routing error logs to the appropriate channels.
 *
 * Change History:
 * ---------------------------------------------------------------------------
 * Date        Author        Version     Description
 * ---------------------------------------------------------------------------
 * 2026-04-30  Rishabh       1.0         Initial creation
 * 
 * 
 *
 ******************************************************************************/


using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IPCSoftware.UI.CommonViews.Services
{
    /// <summary>
    /// Shared UI logger for AOI and Bending apps.
    /// Routes logs to Core Service via TCP with Event Viewer fallback.
    /// </summary>
    public class UiErrorLogger : IAppLogger
    {
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;
        private bool _hasWarnedUser = false;

        private readonly string SOURCE_NAME ;
        private readonly string LOG_NAME ;

        public UiErrorLogger(CoreClient coreClient, IDialogService dialog ,string source , string log)
        {
            _coreClient = coreClient;
            _dialog = dialog;
            SOURCE_NAME = source;
            LOG_NAME = log;
        }

        public void LogInfo(string message, LogType type)
            => TrySend("INFO", message, type);

        public void LogWarning(string message, LogType type)
            => TrySend("WARN", message, type);

        public void LogError(string message, LogType type,
            string memberName = "", string filePath = "", int lineNumber = 0)
            => TrySend("ERROR", message, type, memberName, filePath, lineNumber);

        public void LogTrace(string message)
            => TrySend("TRACE", message, LogType.TagTrace);

        private async void TrySend(string level, string message, LogType type,
            string memberName = "", string filePath = "", int lineNumber = 0)
        {
            bool sent = await _coreClient.SendLogAsync(message, level, type,
                memberName, filePath, lineNumber);

            if (!sent)
            {
                WriteToEventViewer(message, level);
                //if (!_hasWarnedUser && level == "ERROR")
                //{
                //    _hasWarnedUser = true;
                //    _dialog.ShowWarning("Logging service unavailable. Error logged to Event Viewer.");
                //}
            }
        }

        private void WriteToEventViewer(string message, string level)
        {

            try
            {
                try
                {

                    if (!EventLog.SourceExists(SOURCE_NAME))
                    {
                        EventLog.CreateEventSource(SOURCE_NAME, LOG_NAME);
                        System.Threading.Thread.Sleep(100);
                    }
                }

                catch (System.Security.SecurityException)
                {
                    System.Diagnostics.Debug.WriteLine($"[EVENT LOG PERMISSION ERROR] Cannot create event source '{SOURCE_NAME}'. Check permissions.");
                }
                var type = level == "ERROR" ? EventLogEntryType.Error : EventLogEntryType.Information;
                EventLog.WriteEntry(SOURCE_NAME, message, type);
            }
            catch { }
        }
    }

}