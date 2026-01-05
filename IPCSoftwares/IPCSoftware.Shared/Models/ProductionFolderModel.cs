using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class ProductionFolderModel
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ImageCount { get; set; }
    }

    public class ProductionImageModel
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        // We bind directly to FullPath in XAML, 
        // WPF handles the thumbnail generation if we set DecodePixelWidth.
    }
}
