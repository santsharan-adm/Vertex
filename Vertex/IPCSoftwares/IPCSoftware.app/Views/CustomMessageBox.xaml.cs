using IPCSoftware.App.ViewModels;
using System.Windows;

namespace IPCSoftware.App.Views
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox()
        {
            InitializeComponent();
        }

        // Method to inject ViewModel and subscribe to events
        public void Initialize(CustomMessageBoxViewModel vm)
        {
            this.DataContext = vm;

            // Subscribe to the CloseRequested event
            vm.CloseRequested += (result) =>
            {
                this.DialogResult = result;
                this.Close();
            };
        }
    }
}