using IPCSoftware.Core.Interfaces.AppLoggerInterface;
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
    public partial class FullImageViewModel :ObservableObjectVM
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
        private double _yValue = 0; // Logic to determine color can be added later
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



        // 2. The Logic (Close Command)
        // We use an Action to request the View to close itself (MVVM-safe way)
        public Action RequestClose;

        public ICommand CloseCommand { get; }

        // Constructor Injection: Pass the dependency here!
        public FullImageViewModel(CameraImageItem item, 
            string measurementName) 
        {
            ImagePath = item.ImagePath;
            XValue = item.ValX;
            YValue = item.ValY;
            ZValue = item.ValZ;
            MeasurementTitle = measurementName; // Store it
            CloseCommand = new RelayCommand(Close);
        }

        private void Close()
        {
            RequestClose?.Invoke();
        }
    }
}
