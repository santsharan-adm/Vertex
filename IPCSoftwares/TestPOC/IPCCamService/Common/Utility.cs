using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AttachMetaDataToFile.Common
{
    public static class Utility
    {
        public static void CalculateMD5HashCode(List<byte> output, string outputImagePath, int metadataSize)
        {
            // STEP 7: MD5 hash of fileData + metadata
            byte[] md5Hash = MD5.Create().ComputeHash(output.ToArray());
            // STEP 8–12: Write final output
            //using (BinaryWriter bw = new BinaryWriter(File.Open(inputImagePath, FileMode.Create)))
            using (BinaryWriter bw = new BinaryWriter(File.Open(outputImagePath, FileMode.Create)))
            {
                // STEP 9 - Write fileData + metadata
                bw.Write(output.ToArray());

                // STEP 10 - Write MetadataSize (int)
                bw.Write(metadataSize);

                // STEP 11 - Write MD5 Hash
                bw.Write(md5Hash);

                // STEP 12 - Close automatically due to using()
            }
        }

        public static string ValidateImageNickname(string nickname)
        {
            if (nickname == null)
                nickname = "";

            // 1) Allow only printable ASCII characters (0x21 to 0x7E)
            nickname = new string(nickname
                .Where(c => c >= 0x21 && c <= 0x7E)
                .ToArray());

            // 2) Remove commas (0x2C)
            nickname = nickname.Replace(",", "");

            // 3) Must be unique in the DUT process cycle
            //if (usedNicknames.Contains(nickname))
            //    throw new Exception($"ImageNickname '{nickname}' is already used in this DUT cycle. It must be unique.");

            // 4) Mark as used
            //usedNicknames.Add(nickname);

            return nickname;
        }

    }
}
