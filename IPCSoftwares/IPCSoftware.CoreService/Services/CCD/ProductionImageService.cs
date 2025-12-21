using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing; // NuGet: System.Drawing.Common
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class ProductionImageService : BaseService
    {
        private readonly CcdSettings _ccd;

        public ProductionImageService(IOptions<CcdSettings> ccdOptions,
            IAppLogger logger) : base(logger)
        {
            _ccd = ccdOptions.Value;
        }

        /// <summary>
        /// Processes a temp image, adds metadata, and moves both raw and processed versions to the production folder.
        /// </summary>
        /// <param name="tempFilePath">Full path to the source file (e.g. inside CCD folder)</param>
        /// <param name="uniqueDataString">The 40-char unique string</param>
        /// <param name="stNo">Station Number (1-12)</param>
        public string ProcessAndMoveImage(string tempFilePath, string uniqueDataString, int stNo, bool qrCodeFile = false)
        {
            try
            {
                if (!File.Exists(tempFilePath))
                {
                    Console.WriteLine($"[Error] Source file not found: {tempFilePath}");
                    _logger.LogInfo($"[Error] Source file not found: {tempFilePath}", LogType.Diagnostics);
                    return string.Empty;
                }

                // 1. Generate Time Data (Date of Today and Current Time)
                DateTime now = DateTime.Now;
                string dateStr = now.ToString("dd-MM-yyyy");       // e.g. 08-12-2025
                string timeStr = now.ToString("HH-mm-ss-fff");     // e.g. 14-30-01-123 (Colons replaced with dashes for filename validity)

                // 2. Construct Folder Name
                // Format: uniqueString_DateOfToday
                string folderName = $"{uniqueDataString}_{dateStr}".Replace("\0", "_");
                string targetFolder = Path.Combine(_ccd.BaseOutputDir, folderName);

                // Create directory if it doesn't exist
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                // 3. Construct File Name Base
                // Format: stNo_FolderName_time(hh-mm-ss-xxx)
                string fileNameBase = $"{stNo}_{folderName}_{timeStr}";

                // 4. Define Full Output Paths
                string rawDestPath = Path.Combine(targetFolder, $"{fileNameBase}_raw.bmp");
                //string rawUIDestPath = Path.Combine(Path.GetDirectoryName(tempFilePath), "UI", $"{fileNameBase}_raw.bmp");
                string rawUIDestPath = Path.Combine(_ccd.QrCodeImagePath, $"{fileNameBase}_raw.bmp");
                if (!Directory.Exists(_ccd.QrCodeImagePath))
                {
                    Directory.CreateDirectory(_ccd.QrCodeImagePath);
                }
                string procDestPath = Path.Combine(targetFolder, $"{fileNameBase}_processed.bmp");

                // 5. Prepare Metadata Strings
                string clientMetadata = $"Data:{uniqueDataString};StNo:{stNo}";
                string vendorMetadata = "Vendor:ABCDEF;Ver:1.0";

                if (!qrCodeFile)
                {
                  ProcessImageInternal(tempFilePath, procDestPath, clientMetadata, vendorMetadata);
                }
                // 6. Process I mage (Read Temp -> Add Meta -> Save to Processed Path)

                // 7. Copy Raw Image to Production Path
                // We use Copy instead of Move so we don't lock the file if logic fails halfway
                File.Copy(tempFilePath, rawDestPath, true);
                File.Copy(tempFilePath, rawUIDestPath, true);
                Console.WriteLine($"[Info] Raw image saved: {rawDestPath}");
                _logger.LogInfo($"[Info] Raw image saved: {rawDestPath}", LogType.Diagnostics);

                // 8. Cleanup Temp File
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warning] Could not delete temp file: {ex.Message}");
                    _logger.LogError($"[Warning] Could not delete temp file: {ex.Message}", LogType.Diagnostics);
                }

                return rawUIDestPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] Image Workflow failed: {ex.Message}");
                _logger.LogError($"[Exception] Image Workflow failed: {ex.Message}", LogType.Diagnostics);
                return string.Empty;
            }
        }

        private void ProcessImageInternal(string inputPath, string outputPath, string clientMeta, string vendorMeta)
        {
            try
            {
                // 1. Load Image to Memory
                byte[] fileData;
                using (var ms = new MemoryStream())
                {
                    // We open the file stream directly to ensure we don't lock it for long
                    using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
                    using (var img = Image.FromStream(fs))
                    {
                        img.Save(ms, ImageFormat.Bmp);
                    }
                    fileData = ms.ToArray();
                }

                // 2. Build Data Array
                List<byte> dataBuilder = new List<byte>(fileData);

                // Append Style
                dataBuilder.AddRange(Encoding.ASCII.GetBytes(_ccd.MetadataStyle));

                // Append Client Meta
                if (_ccd.MetadataStyle != "METADATASTYLE002")
                {
                    dataBuilder.AddRange(Encoding.ASCII.GetBytes(clientMeta));
                    dataBuilder.Add(0x90); // Terminator
                }

                // Append Vendor Meta
                if (_ccd.MetadataStyle != "METADATASTYLE001")
                {
                    dataBuilder.AddRange(Encoding.ASCII.GetBytes(vendorMeta));
                    dataBuilder.Add(0x80); // Terminator
                }

                // 3. Calculate Hash
                byte[] finalData = dataBuilder.ToArray();
                byte[] hash;
                using (MD5 md5 = MD5.Create())
                {
                    hash = md5.ComputeHash(finalData);
                }

                // 4. Write to Production Path
                using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(finalData, 0, finalData.Length);

                    // Write Length (2 bytes)
                    short metaLen = (short)(finalData.Length - fileData.Length);
                    fs.Write(BitConverter.GetBytes(metaLen), 0, 2);

                    // Write Hash (16 bytes)
                    fs.Write(hash, 0, hash.Length);
                }

                Console.WriteLine($"[Info] Processed image saved: {outputPath}");
                _logger.LogInfo($"[Info] Processed image saved: {outputPath}", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }
    }


}

