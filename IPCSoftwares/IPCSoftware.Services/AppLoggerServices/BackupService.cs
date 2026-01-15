
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


        public void PerformBackup(LogConfigurationModel config)
        {
            try
            {
                if (config.BackupSchedule == BackupScheduleType.Manual && !config.Enabled)
                    return; // Or allow Manual even if disabled? Usually yes.

                // 1. Backup Log Data Folder
                if (Directory.Exists(config.DataFolder) && !string.IsNullOrEmpty(config.BackupFolder))
                {
                    // Create timestamped subfolder or just overwrite?
                    // "you will just overwrite whole data at backup folder form datafolder" -> Implies mirroring.
                    // If you want history, we'd append a timestamp folder. 
                    // Based on "overwrite whole data", we copy directly to BackupFolder.

                    CopyDirectory(config.DataFolder, config.BackupFolder);
                }

                // 2. Special Case: Production Images
                /*if (config.LogType == LogType.Production)
                {
                 
                  //  string sourceImages = _ccdSettings.BaseOutputDir;
                    string backupImages = _ccdSettings.BaseOutputDirBackup;

                    if (Directory.Exists(sourceImages) && !string.IsNullOrEmpty(backupImages))
                    {
                        CopyDirectory(sourceImages, backupImages);
                    }
                }*/
            }
            catch (Exception ex)
            {
                // Log error (inject logger if needed, or swallow/debug print)
                System.Diagnostics.Debug.WriteLine($"Backup Error: {ex.Message}");
            }
        }


        public void PerformRestore(LogConfigurationModel config)
        {
            try
            {
             
                if (Directory.Exists(config.BackupFolder) && !string.IsNullOrEmpty(config.DataFolder))
                {
                  //  CopyDirectory(config.DataFolder, config.BackupFolder);
                    CopyDirectory(config.BackupFolder, config.DataFolder);
                }

                // 2. Special Case: Production Images
                //if (config.LogType == LogType.Production)
                //{

                //    string sourceImages = _ccdSettings.BaseOutputDir;
                //    string backupImages = _ccdSettings.BaseOutputDirBackup;

                //    if (Directory.Exists(backupImages) && !string.IsNullOrEmpty(sourceImages))
                //    {
                //        CopyDirectory(backupImages, sourceImages);
                //    }
                //}
            }
            catch (Exception ex)
            {
                // Log error (inject logger if needed, or swallow/debug print)
                System.Diagnostics.Debug.WriteLine($"Backup Error: {ex.Message}");
            }
        }


        private void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            var dir = new DirectoryInfo(sourceDir);

            // Copy all files
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true); // true = overwrite
            }

            // Copy all subdirectories (Recursive)
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string newDestDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir);
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
