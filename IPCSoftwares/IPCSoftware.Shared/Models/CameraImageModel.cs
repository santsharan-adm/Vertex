using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IPCSoftware.Shared.Models
{
    public class CameraImageItem : ObservableObjectVM
    {
        private int _stationNumber;
        public int StationNumber
        {
            get => _stationNumber;
            set => SetProperty(ref _stationNumber, value);
        }

        private ImageSource _imagePath;
        public ImageSource ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        // Used to track file changes so we don't reload the same bitmap
        public string LastLoadedFilePath { get; set; }

        private string _result; // "OK", "NG", "Unchecked"
        public string Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        // --- NEW FIELDS FOR DATA SYNC ---

        private double _valX;
        public double ValX
        {
            get => _valX;
            set => SetProperty(ref _valX, value);
        }

        private double _valY;
        public double ValY
        {
            get => _valY;
            set => SetProperty(ref _valY, value);
        }

        private double _valZ;
        public double ValZ
        {
            get => _valZ;
            set => SetProperty(ref _valZ, value);
        }
    }
}
