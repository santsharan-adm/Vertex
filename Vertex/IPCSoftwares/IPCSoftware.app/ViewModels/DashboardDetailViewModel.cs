using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.ViewModels
{

    /// ViewModel for the Dashboard Detail window.
    /// Displays detailed metrics for a selected dashboard card.
    /// Handles dynamic title binding and provides a close command for the window.
    public class DashboardDetailViewModel : BaseViewModel
    {

        // -------------------Private Fields-----------------//


        private string _cardTitle;                             // Title shown inside the card header

        public string CardTitle                                
        {
            get => _cardTitle;
            set => SetProperty(ref _cardTitle, value);
        }

         private string  _windowTitle;                       // Title shown in the window bar


        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public ObservableCollection<MetricDetailItem> DetailRows { get; } = new();

        public RelayCommand CloseCommand { get; }

        // Reference to the current window (used to close it)
        private Window _window;


        // ---------------------- Constructor --------------------//

        /// Initializes the detail view model.
        /// Loads metric data and sets window title and close command.
        public DashboardDetailViewModel(Window window, string title, List<MetricDetailItem> data)
        {
            _window = window;
            WindowTitle = title;


            // Load metric details into observable collection for UI binding
            foreach (var item in data)
            {
                DetailRows.Add(item);
            }


            // Command closes the window when executed
            CloseCommand = new RelayCommand(() => _window.Close());
        }
    }
}


    
