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

    /// ViewModel responsible for displaying a full-size image along with
    /// related measurement data (X, Y, Z values). 
    /// It also provides a command to close the image viewer window in an MVVM-friendly way.
    public partial class FullImageViewModel :BaseViewModel
    {
        
        // ------------Private Fields------------//


        private string _imagePath;                                  // Stores the file path or URI of the image

        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        private string _measurementTitle;                         // Holds the title of the measurement or image context

        public string MeasurementTitle
        {
            get => _measurementTitle;
            set => SetProperty(ref _measurementTitle, value);
        }

        private string _xValue = "3.45";                            // Default X coordinate value
        private string _yValue = "5.23";                            // Default Y coordinate value
        private string _zValue = "0.02";                            // Default Z coordinate value (e.g., depth or offset)


        // --------------Public Properties (Bindable to UI)-----------------//


        /// Path of the image to display in the full view window.
        /// Typically a local file path or network URL.
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

            // Initialize the Close command, which invokes the Close() method.
            CloseCommand = new RelayCommand(Close);
        }

        // ------------- Private Methods -------------//


        /// Invokes the RequestClose action to notify the view to close.
        private void Close()
        {
            RequestClose?.Invoke();
        }
    }
}
