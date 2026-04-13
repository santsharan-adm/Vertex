using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AttachMetaDataToFile.Model;

namespace FileWatcherService
{
    public class ImageMetadataWriter
    {
        //private readonly IConfiguration _config;
        //public ImageMetadataWriter(IConfiguration config)
        //{
        //    _config = config;
        //}

        private const byte TERMINATOR = 0x00;

        // =====================================================================
        // 1️⃣  DYNAMIC METADATA BUILDER (from Dictionary<string,string>)
        // =====================================================================
        public string BuildDynamicMetadata(List<KeyValuePair<string, string>> metadata)
        {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var item in metadata)
            {
                string key = item.Key;
                string value = item.Value;

                //bool keyIsOnlyComma = key.Trim() == ",";
                if (!isFirst)
                    sb.Append(",");

                if (string.IsNullOrEmpty(key))
                {
                    //sb.Append("");      // example: ","
                }
                else
                {
                    sb.Append($"{key}={value}");
                }

                isFirst = false;
            }

            return sb.ToString();
        }

        // =====================================================================
        // 2️⃣  STYLE 1: CUSTOM METADATA FORMATTER
        // =====================================================================
        private byte[] FormatCustomMetadata(string metadata, string style)
        {
            return Encoding.UTF8.GetBytes(metadata);

            //string formatted = $"{style}:{metadata}";
            //return Encoding.UTF8.GetBytes(formatted);
        }

        // =====================================================================
        // 3️⃣  STYLE 2: VENDOR METADATA FORMATTER
        // =====================================================================
        private byte[] FormatVendorMetadata(string metadata)
        {
            string formatted = $"VENDOR:{metadata}";
            return Encoding.UTF8.GetBytes(formatted);
        }

        // =====================================================================
        // 4️⃣  MAIN FUNCTION TO ATTACH METADATA (Follows all 12 steps)
        // =====================================================================
        /// <summary>
        /// using this function for METADATASTYLE001 and METADATASTYLE002
        /// </summary>
        /// <param name="inputImagePath"></param>
        /// <param name="outputImagePath"></param>
        /// <param name="dynamicMetadata"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public ResAttachMeta AttachMetadataToImage(string inputImagePath, string outputImagePath
            , List<KeyValuePair<string, string>> dynamicMetadata, string style)
        {
            string metaStyleText = string.Empty;
            MemoryStream ms = new MemoryStream();

            // STEP 1 & 2: Load image and save to MemoryStream as JPEG
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(inputImagePath))
            {
                img.Save(ms, ImageFormat.Jpeg);
            }

            // STEP 3: Convert JPEG to byte[]
            byte[] fileData = ms.ToArray();

            // STEP 4: Append metadata style to file data
            List<byte> output = new List<byte>(fileData);
            metaStyleText = $"METADATASTYLE={style},";
            output.AddRange(Encoding.ASCII.GetBytes(metaStyleText));

            // STEP 5: Initialize metadata size (including style byte)
            int metadataSize = Encoding.ASCII.GetBytes(metaStyleText).Length;





            // STEP 6: Format metadata based on style
            string formattedMetadata = BuildDynamicMetadata(dynamicMetadata);
            byte[] metadataBytes;
            metadataBytes = FormatCustomMetadata(formattedMetadata, style);

            output.AddRange(metadataBytes);

            // Append termination (0x00)
            output.Add(TERMINATOR);

            // Increase metadata size
            metadataSize += metadataBytes.Length + 1;

            //// STEP 7: MD5 hash of fileData + metadata
            //byte[] md5Hash = MD5.Create().ComputeHash(output.ToArray());
            //// STEP 8–12: Write final output
            //using (BinaryWriter bw = new BinaryWriter(File.Open(outputImagePath, FileMode.Create)))
            //{
            //    // STEP 9 - Write fileData + metadata
            //    bw.Write(output.ToArray());

            //    // STEP 10 - Write MetadataSize (int)
            //    bw.Write(metadataSize);

            //    // STEP 11 - Write MD5 Hash
            //    bw.Write(md5Hash);

            //    // STEP 12 - Close automatically due to using()
            //}

            ResAttachMeta resAttachMeta = new ResAttachMeta();
            resAttachMeta.output = output;
            resAttachMeta.imgPath = outputImagePath;
            resAttachMeta.metadataSize = metadataSize;

            return resAttachMeta;
        }

        /// <summary>
        /// using this function for METADATASTYLE003
        /// </summary>
        /// <param name="inputImagePath"></param>
        /// <param name="outputImagePath"></param>
        /// <param name="dynamicMetadata"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public ResAttachMeta AttachMetadataToImageForStyle3(string inputImagePath, string outputImagePath
            , List<KeyValuePair<string, string>> dynamicMetadataApple, List<KeyValuePair<string, string>> dynamicMetadataVendor, 
            string style)
        {
            string metaStyleText = string.Empty;
            MemoryStream ms = new MemoryStream();

            // STEP 1 & 2: Load image and save to MemoryStream as JPEG
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(inputImagePath))
            {
                img.Save(ms, ImageFormat.Jpeg);
            }

            // STEP 3: Convert JPEG to byte[]
            byte[] fileData = ms.ToArray();

            // STEP 4: Append metadata style to file data
            List<byte> output = new List<byte>(fileData);
            metaStyleText = $"METADATASTYLE={style},";
            output.AddRange(Encoding.ASCII.GetBytes(metaStyleText));

            // STEP 5: Initialize metadata size (including style byte)
            int metadataSize = Encoding.ASCII.GetBytes(metaStyleText).Length;





            // STEP 6: working for Apple Meta Data
            string formattedMetadata = BuildDynamicMetadata(dynamicMetadataApple);
            
            byte[] metadataBytes;
            metadataBytes = FormatCustomMetadata(formattedMetadata, style);
            output.AddRange(metadataBytes);

            // Append termination (0x00)
            output.Add(TERMINATOR);

            // Increase metadata size
            metadataSize += metadataBytes.Length + 1;


            // STEP 7: working for Vendor Meta Data
            string formattedVendorMetadata = BuildDynamicMetadata(dynamicMetadataVendor);    //vendorMetaDatas

            byte[] metadataBytesVendor;
            metadataBytesVendor = FormatCustomMetadata(formattedVendorMetadata, style);
            output.AddRange(metadataBytesVendor);

            // Append termination (0x00)
            output.Add(TERMINATOR);

            // Increase metadata size
            metadataSize += metadataBytesVendor.Length + 1;


            ResAttachMeta resAttachMeta = new ResAttachMeta();
            resAttachMeta.output = output;
            resAttachMeta.imgPath = outputImagePath;
            resAttachMeta.metadataSize = metadataSize;

            return resAttachMeta;
        }
    }
}
