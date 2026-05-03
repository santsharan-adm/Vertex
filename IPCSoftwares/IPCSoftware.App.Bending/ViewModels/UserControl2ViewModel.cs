using System;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.App.Bending.ViewModels
{
    // Simple implementation of ICommand to handle button clicks in MVVM
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute();
        public event EventHandler CanExecuteChanged;
    }

    public class UserControl2ViewModel
    {
        // Property for the button binding
        public ICommand ToggleSidebarCommand { get; }

        public UserControl2ViewModel()
        {
            // Initialize the command and point it to the method
            ToggleSidebarCommand = new RelayCommand(ExecuteGoToMenu);
        }

        private void ExecuteGoToMenu()
        {
            // Add your navigation or menu logic here
            // For now, we show a message to confirm binding works
            MessageBox.Show("Navigation  Moving to Menu..."); 
        }
    }
}