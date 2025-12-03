using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public partial class FullImageViewModel :BaseViewModel
    {

        private string _imagePath;

        public string ImagePath
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

        private string _xValue = "3.45";
        private string _yValue = "5.23"; // Logic to determine color can be added later
      private string _zValue = "0.02";

        public string XValue
        {
            get => _xValue;
            set => SetProperty(ref _xValue, value);
        }
        public string YValue
        {
            get => _yValue;
            set => SetProperty(ref _yValue, value);
        }
        public string ZValue
        {
            get => _zValue;
            set => SetProperty(ref _zValue, value);
        }



        // 2. The Logic (Close Command)
        // We use an Action to request the View to close itself (MVVM-safe way)
        public Action RequestClose;

        public ICommand CloseCommand { get; }

        // Constructor Injection: Pass the dependency here!
        public FullImageViewModel(string imagePath, string measurementName)
        {
            ImagePath = imagePath;
            MeasurementTitle = measurementName; // Store it
            CloseCommand = new RelayCommand(Close);
        }

        private void Close()
        {
            RequestClose?.Invoke();
        }
    }
}
