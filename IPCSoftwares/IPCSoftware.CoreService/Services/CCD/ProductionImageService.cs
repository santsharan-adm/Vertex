using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Drawing; // NuGet: System.Drawing.Common
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class ProductionImageService
    {
        // Base directory for production images
    


        /// <summary>
        /// Processes a temp image, adds metadata, and moves both raw and processed versions to the production folder.
        /// </summary>
        /// <param name="tempFilePath">Full path to the source file (e.g. inside CCD folder)</param>
        /// <param name="uniqueDataString">The 40-char unique string</param>
        /// <param name="stNo">Station Number (1-12)</param>
        public void ProcessAndMoveImage(string tempFilePath, string uniqueDataString, int stNo, bool qrCodeFile = false)
        {
            if (!File.Exists(tempFilePath))
            {
                Console.WriteLine($"[Error] Source file not found: {tempFilePath}");
                return;
            }

            try
            {
                // 1. Generate Time Data (Date of Today and Current Time)
                DateTime now = DateTime.Now;
                string dateStr = now.ToString("dd-MM-yyyy");       // e.g. 08-12-2025
                string timeStr = now.ToString("HH-mm-ss-fff");     // e.g. 14-30-01-123 (Colons replaced with dashes for filename validity)

                // 2. Construct Folder Name
                // Format: uniqueString_DateOfToday
                string folderName = $"{uniqueDataString}_{dateStr}";
                string targetFolder = Path.Combine(ConstantValues.BASE_OUTPUT_DIR, folderName);

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
                string rawUIDestPath = Path.Combine(Path.GetDirectoryName(tempFilePath), "UI", $"{fileNameBase}_raw.bmp");
                string procDestPath = Path.Combine(targetFolder, $"{fileNameBase}_processed.bmp");

                // 5. Prepare Metadata Strings
                string clientMetadata = $"Data:{uniqueDataString};StNo:{stNo}";
                string vendorMetadata = "Vendor:ABCDEF;Ver:1.0";

                if (!qrCodeFile)
                {
                  ProcessImageInternal(tempFilePath, procDestPath, clientMetadata, vendorMetadata);
                }
                // 6. Process Image (Read Temp -> Add Meta -> Save to Processed Path)

                // 7. Copy Raw Image to Production Path
                // We use Copy instead of Move so we don't lock the file if logic fails halfway
                File.Copy(tempFilePath, rawDestPath, true);
                File.Copy(tempFilePath, rawUIDestPath, true);
                Console.WriteLine($"[Info] Raw image saved: {rawDestPath}");

                // 8. Cleanup (Delete from Temp/CCD)
                // Uncomment the line below when ready to delete source files
                 File.Delete(tempFilePath); 
                // Console.WriteLine($"[Info] Temp file deleted: {tempFilePath}");

                Console.WriteLine($"✅ Cycle Complete for St {stNo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] Workflow failed: {ex.Message}");
            }
        }


        private void ProcessImageInternal(string inputPath, string outputPath, string clientMeta, string vendorMeta)
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
            dataBuilder.AddRange(Encoding.ASCII.GetBytes(ConstantValues.METADATA_STYLE));

            // Append Client Meta
            if (ConstantValues.METADATA_STYLE != "METADATASTYLE002")
            {
                dataBuilder.AddRange(Encoding.ASCII.GetBytes(clientMeta));
                dataBuilder.Add(0x90); // Terminator
            }

            // Append Vendor Meta
            if (ConstantValues.METADATA_STYLE != "METADATASTYLE001")
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
        }
    }


}

