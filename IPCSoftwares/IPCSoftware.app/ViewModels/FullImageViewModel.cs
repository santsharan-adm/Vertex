using IPCSoftware.App.Helpers;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace IPCSoftware.App.ViewModels
{
    public class FullImageViewModel : BaseViewModel
    {
        private ImageSource _imagePath;
        public ImageSource ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        private string _measurementTitle;
        public string MeasurementTitle
        {
            get => _measurementTitle;
            set => SetProperty(ref _measurementTitle, value);
        }

        private double _xValue;
        public double XValue
        {
            get => _xValue;
            set => SetProperty(ref _xValue, value);
        }

        private double _yValue;
        public double YValue
        {
            get => _yValue;
            set => SetProperty(ref _yValue, value);
        }

        private double _zValue;
        public double ZValue
        {
            get => _zValue;
            set => SetProperty(ref _zValue, value);
        }

        // --- LIMITS (Read-only, passed from Dashboard) ---
        public double LimitX_Min { get; }
        public double LimitX_Max { get; }
        public double LimitY_Min { get; }
        public double LimitY_Max { get; }
        public double LimitZ_Min { get; }
        public double LimitZ_Max { get; }

        // --- DYNAMIC COLORS ---
        public Brush StatusBrushX => GetStatusBrush(XValue, LimitX_Min, LimitX_Max);
        public Brush StatusBrushY => GetStatusBrush(YValue, LimitY_Min, LimitY_Max);
        public Brush StatusBrushZ => GetStatusBrush(ZValue, LimitZ_Min, LimitZ_Max);

        // --- COMMANDS ---
        public Action RequestClose;
        public ICommand CloseCommand { get; }

        public FullImageViewModel(
            CameraImageItem item,
            string measurementName,
            double xMin, double xMax,
            double yMin, double yMax,
            double zMin, double zMax) : base(null)
        {
            // Data
            ImagePath = item.ImagePath as ImageSource; // Cast object back to ImageSource
            XValue = item.ValX;
            YValue = item.ValY;
            ZValue = item.ValZ;
            MeasurementTitle = measurementName;

            // Limits
            LimitX_Min = xMin;
            LimitX_Max = xMax;
            LimitY_Min = yMin;
            LimitY_Max = yMax;
            LimitZ_Min = zMin;
            LimitZ_Max = zMax;

            CloseCommand = new RelayCommand(Close);
        }

        private void Close()
        {
            RequestClose?.Invoke();
        }

        private Brush GetStatusBrush(double val, double min, double max)
        {
            bool isOk = val >= min && val <= max;
            var colorCode = isOk ? "#28A745" : "#DC3545"; // Green : Red
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorCode));
        }
    }
}