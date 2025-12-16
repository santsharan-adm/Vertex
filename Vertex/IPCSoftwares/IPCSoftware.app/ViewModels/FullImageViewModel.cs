using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace IPCSoftware.App.ViewModels
{
    public partial class FullImageViewModel : BaseViewModel
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

        private double _xValue = 0;
        private double _yValue = 0;
        private double _zValue = 0;

        public double XValue
        {
            get => _xValue;
            set => SetProperty(ref _xValue, value);
        }

        public double YValue
        {
            get => _yValue;
            set => SetProperty(ref _yValue, value);
        }

        public double ZValue
        {
            get => _zValue;
            set => SetProperty(ref _zValue, value);
        }

        // Action used to request the View to close (MVVM-safe)
        public Action RequestClose;

        public ICommand CloseCommand { get; }

        // Constructor
        public FullImageViewModel(CameraImageItem item, string measurementName)
        {
            try
            {
                ImagePath = item.ImagePath;
                XValue = item.ValX;
                YValue = item.ValY;
                ZValue = item.ValZ;
                MeasurementTitle = measurementName;
                CloseCommand = new RelayCommand(Close);
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private void Close()
        {
            try
            {
                RequestClose?.Invoke();
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }
    }
}
