using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared
{
    public static class FileTypeHelper
    {
        private static readonly HashSet<string> ImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
            ".bmp", ".png", ".jpg", ".jpeg", ".tiff", ".tif"
            };

        public static bool IsImageFile(string extension)
        {
            return ImageExtensions.Contains(extension);
        }
    }

}
