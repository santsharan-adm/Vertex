/******************************************************************************
 * Project      : IPCSoftware-AOI /Bending
 * Module       : ServiceStartupViewModel
 * File Name    : ServiceStartupViewModel.cs
 * Author       : Rishabh
 * Organization : Vertex Automtion System Pvt Ltd
 * Created Date : 2026-04-15
 *
 * Description  :
 * Manages service control (Start/Stop) and audit log display for the Service Startup UI.
 * Monitors service status in real-time and logs all service-related events.
 *
 * Change History:
 * ---------------------------------------------------------------------------
 * Date        Author        Version     Description
 * ---------------------------------------------------------------------------
 * 2026-04-15  Rishabh       1.0         Initial creation
 * 
 *
 ******************************************************************************/
using IPCSoftware.Services;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;
using System.ComponentModel.DataAnnotations;
using IPCSoftware.Common.WPFExtensions;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.UI.CommonViews.ViewModels
{
    /// <summary>
    /// ViewModel for ServiceStartupView - handles service control operations
    /// and maintains audit log of service activities.
    /// </summary>
    public class ServiceStartupViewModel : BaseViewModel, IDisposable
    {
        private readonly SafePoller _servicePoller;
        private const string TARGET_SERVICE_NAME = "IPCSoftware.CoreService";

        //private RelayCommand _startServiceCommand;
        //private RelayCommand _stopServiceCommand;

        // --- Properties ---
        private string _serviceStatus = "Checking...";
        public string ServiceStatus
        {
            get => _serviceStatus;
            set
            {
                if (SetProperty(ref _serviceStatus, value))
                {
                    OnPropertyChanged(nameof(IsServiceRunning));
                    OnPropertyChanged(nameof(ServiceStatusColor));
                   // RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsServiceRunning => ServiceStatus == "Running";
        
        public string ServiceStatusColor => IsServiceRunning 
            ? "#28A745"           // Green
            : (ServiceStatus == "Stopped" ? "#DC3545" : "#FFC107"); // Red or Yellow

        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new ObservableCollection<AuditLogModel>();

        // --- Commands ---
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }

        public ServiceStartupViewModel(IAppLogger logger) : base(logger)
        {
            // Initialize commands
            StartServiceCommand = new RelayCommand(StartService, () => !IsServiceRunning);
            StopServiceCommand = new RelayCommand(StopService, () => IsServiceRunning);

            // Service Status Poller (Check every 2 seconds)
            _servicePoller = new SafePoller(
                TimeSpan.FromSeconds(2), 
                CheckServiceStatus,
                ex => _logger.LogError($"Service Poll Error: {ex.Message}", LogType.Diagnostics));
            _servicePoller.Start();

            // Perform initial status check
            _ = CheckServiceStatus();

            _logger.LogInfo("[ServiceStartup] ViewModel initialized", LogType.Diagnostics);
        }

        // --- SERVICE CONTROL METHODS ---

        /// <summary>
        /// Checks the current status of the Windows Service
        /// </summary>
        private async Task CheckServiceStatus()
        {
            try
            {
                using (ServiceController sc = new ServiceController(TARGET_SERVICE_NAME))
                {
                    ServiceControllerStatus status = sc.Status;
                    string statusStr = status.ToString();

                    // Update UI on dispatcher thread
                    Application.Current?.Dispatcher?.Invoke(() => ServiceStatus = statusStr);
                }
            }
            catch (Exception ex)
            {
                // Service might not be installed or permission denied
                Application.Current?.Dispatcher?.Invoke(() => 
                    ServiceStatus = "Not Found/Access Denied");
                
                _logger.LogWarning($"Service check failed: {ex.Message}", LogType.Diagnostics);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Starts the service via 'net start' command
        /// </summary>
        private void StartService()
        {
            RunServiceCommand("start");
        }

        /// <summary>
        /// Stops the service via 'net stop' command
        /// </summary>
        private void StopService()
        {
            RunServiceCommand("stop");
        }

        /// <summary>
        /// Executes service control command (start/stop) with admin privileges
        /// </summary>
        private void RunServiceCommand(string action)
        {
            try
            {
                // Validate action
                if (!action.Equals("start", StringComparison.OrdinalIgnoreCase) && 
                    !action.Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Invalid action: {action}");
                }

                ProcessStartInfo psi = new ProcessStartInfo("net", $"{action} {TARGET_SERVICE_NAME}");
                psi.Verb = "runas";              // Run as Administrator
                psi.UseShellExecute = true;      // Required for runas
                psi.CreateNoWindow = true;       // Hide cmd window

                Process.Start(psi);

                // User-friendly message
                string auditMsg = action.Equals("start", StringComparison.OrdinalIgnoreCase) 
                    ? "Started" 
                    : "Stopped";

                AddAudit($"Service {auditMsg}");
                _logger.LogInfo($"Service {auditMsg}", LogType.Audit);

                // Optimistic status update (poller will correct if needed)
                ServiceStatus = action.Equals("start", StringComparison.OrdinalIgnoreCase) 
                    ? "Starting..." 
                    : "Stopping...";
            }
            catch (Exception ex)
            {
                string action_name = action.Equals("start", StringComparison.OrdinalIgnoreCase) ? "start" : "stop";
                _logger.LogError($"Failed to {action_name} service: {ex.Message}", LogType.Diagnostics);
                
                AddAudit($"Service {action} FAILED: {ex.Message}");

                // Show error dialog
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Could not {action} service. Ensure you have Administrator privileges.",
                        "Service Control Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        // --- AUDIT LOG METHODS ---

        /// <summary>
        /// Adds an entry to the audit log collection
        /// </summary>
        private void AddAudit(string message)
        {
            try
            {
                AuditLogs.Insert(0, new AuditLogModel
                {
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Message = message
                });

                // Keep log size manageable (max 500 entries)
                while (AuditLogs.Count > 500)
                {
                    AuditLogs.RemoveAt(AuditLogs.Count - 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add audit log: {ex.Message}", LogType.Diagnostics);
            }
        }



        //private void RaiseCanExecuteChanged()
        //{
        //    try
        //    {
        //        //_startServiceCommand?.RaiseCanExecuteChanged();
        //        //_stopServiceCommand?.RaiseCanExecuteChanged();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError($"Error raising CanExecuteChanged: {ex.Message}", LogType.Diagnostics);
        //    }
        //}

        // --- CLEANUP ---

        public void Dispose()
        {
            try
            {
                _servicePoller?.Dispose();
                _logger.LogInfo("[ServiceStartup] ViewModel disposed", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Dispose error: {ex.Message}", LogType.Diagnostics);
            }
        }
    }
}