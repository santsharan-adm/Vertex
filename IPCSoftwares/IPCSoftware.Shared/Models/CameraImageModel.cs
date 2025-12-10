using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IPCSoftware.Shared.Models
{
    //public class CameraImageItem
    //{
    //    public int Id { get; set; }
    //    public string ImagePath { get; set; }
    //    public string Result { get; set; }  // "OK", "NG", "TOSSED"
    //}

    public class CameraImageItem : BaseViewModel
    {
        public int Id { get; set; }
        private ImageSource _imagePath;
        public ImageSource ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        private string _result;
        public string Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        public int StationNumber { get; set; }
        public string LastLoadedFilePath { get; set; } = string.Empty;
    }
}
