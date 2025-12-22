using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.NavServices
{
    /// DialogService provides a centralized way to show
    /// message dialogs (Info, Warning, Yes/No) in the application.
    public class DialogService : IDialogService
    {
        /// Shows a Yes/No confirmation dialog.
        public bool ShowYesNo(string message, string title = "Confirm")
        {
            // TRUE → Confirmation dialog (Red primary button, Cancel visible)
            var vm = new CustomMessageBoxViewModel(message, title, "Yes", "Cancel", true);
            return ShowWindow(vm);
        }

        public void ShowMessage(string message)
        {
            // FALSE = Info (Blue button, Hide Cancel)
            var vm = new CustomMessageBoxViewModel(message, "Information", "OK", "", false);
            ShowWindow(vm);
        }

        public void ShowWarning(string message)
        {
            // FALSE = Info (Blue button, Hide Cancel)
            var vm = new CustomMessageBoxViewModel(message, "Warning", "OK", "", false);
            ShowWindow(vm);
        }




        // Helper method to reduce code duplication
        private bool ShowWindow(CustomMessageBoxViewModel vm)
        {
            var msgBox = new CustomMessageBox();

            // Set owner to MainWindow so dialog stays on top
            if (Application.Current.MainWindow != null)
            {
                msgBox.Owner = Application.Current.MainWindow;
            }

            // Initialize dialog with ViewModel
            msgBox.Initialize(vm);

            // Show dialog modally
            var result = msgBox.ShowDialog();

            // Return true only when user confirms
            return result == true;
        }
    }

}
