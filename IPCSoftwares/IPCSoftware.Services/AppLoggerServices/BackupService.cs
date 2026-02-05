
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.AppLoggerServices
{
    public class BackupService 
    {
        private readonly CcdSettings _ccdSettings;

        public BackupService(IOptions<CcdSettings> ccd) 
        {
            _ccdSettings = ccd.Value;
        }

        /*  public void PerformBackup(LogConfigurationModel config, string filePath)
          {
              try
              {
                  if (config.BackupSchedule == BackupScheduleType.Manual)
                      return;

                  // Backup folder validation
                  if (!Directory.Exists(config.BackupFolder))
                      Directory.CreateDirectory(config.BackupFolder);

                  // Check if it's time to backup
                  if (!IsBackupDue(config))
                      return;

                  // Backup filename
                  string backupFileName = $"{config.FileName.Replace("{yyyyMMdd}",
                      DateTime.Now.ToString("yyyyMMdd"))}_backup_{DateTime.Now:HHmmss}.csv";

                  string backupFilePath = Path.Combine(config.BackupFolder, backupFileName);

                  // Copy file
                  File.Copy(filePath, backupFilePath, true);
              }
              catch (Exception ex)
              {
                 // _logger.LogError(ex.Message, LogType.Diagnostics);
              }
          }*/


        public BackupResult PerformBackup(LogConfigurationModel config)
        {
            var result = new BackupResult();
            try
            {
                if (config.BackupSchedule == BackupScheduleType.Manual && !config.Enabled)
                    return result;

                // 1. Backup Log Data Folder
                if (Directory.Exists(config.DataFolder) && !string.IsNullOrEmpty(config.BackupFolder))
                {
                    CopyDirectory(config.DataFolder, config.BackupFolder, result);
                }

                // 2. Special Case: Production Images
                if (config.LogType == LogType.Production)
                {
                    string sourceImages = config.ProductionImagePath;
                    string backupImages = config.ProductionImageBackupPath;

                    if (Directory.Exists(sourceImages) && !string.IsNullOrEmpty(backupImages))
                    {
                        CopyDirectory(sourceImages, backupImages, result);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup Logic Error: {ex.Message}");
                // Treat top-level exception as a failure, though technically files might have copied
            }
            return result;
        }


        public BackupResult PerformRestore(LogConfigurationModel config)
        {
            var result = new BackupResult();
            try
            {
                // 1. Restore Logs
                if (Directory.Exists(config.BackupFolder) && !string.IsNullOrEmpty(config.DataFolder))
                {
                    CopyDirectory(config.BackupFolder, config.DataFolder, result);
                }

                // 2. Special Case: Production Images
                if (config.LogType == LogType.Production)
                {
                    string sourceImages = config.ProductionImagePath;
                    string backupImages = config.ProductionImageBackupPath;

                    if (Directory.Exists(backupImages) && !string.IsNullOrEmpty(sourceImages))
                    {
                        CopyDirectory(backupImages, sourceImages, result);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Restore Error: {ex.Message}");
            }
            return result;
        }

        private void CopyDirectory(string sourceDir, string destDir, BackupResult result)
        {
            if (!Directory.Exists(destDir))
            {
                try { Directory.CreateDirectory(destDir); } catch { /* Ignore directory creation fail, file copy will fail next */ }
            }

            var dir = new DirectoryInfo(sourceDir);

            // Get files
            FileInfo[] files;
            try { files = dir.GetFiles(); }
            catch { return; } // Access denied to source dir

            // Copy all files
            foreach (FileInfo file in files)
            {
                result.TotalFiles++;
                try
                {
                    string targetFilePath = Path.Combine(destDir, file.Name);

                    // Check if file exists and is newer? 
                    // Requirement: "overwrite whole data".

                    file.CopyTo(targetFilePath, true); // true = overwrite
                    result.CopiedFiles++;
                }
                catch (Exception ex)
                {
                    result.FailedFiles++;
                    System.Diagnostics.Debug.WriteLine($"Failed to copy {file.Name}: {ex.Message}");
                }
            }

            // Copy all subdirectories (Recursive)
            DirectoryInfo[] subDirs;
            try { subDirs = dir.GetDirectories(); }
            catch { return; }

            foreach (DirectoryInfo subDir in subDirs)
            {
                string newDestDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir, result);
            }
        }



        private bool IsBackupDue(LogConfigurationModel config)
        {
            try
            {
                DateTime now = DateTime.Now;

                // Time check (hour/minute)
                if (now.Hour != config.BackupTime.Hours ||
                    now.Minute != config.BackupTime.Minutes)
                    return false;
                return config.BackupSchedule switch
                {
                    BackupScheduleType.Manual => false,
                    BackupScheduleType.Daily => true,
                    BackupScheduleType.Weekly => now.DayOfWeek == DayOfWeek.Monday,
                    BackupScheduleType.Monthly => now.Day == 1,
                    _ => false
                };
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }

        }
    }
}
