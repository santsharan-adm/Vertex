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
    public class DialogService : IDialogService
    {
        public bool ShowYesNo(string message, string title = "Confirm")
        {
            // TRUE = Confirmation (Red button, Show Cancel)
            var vm = new CustomMessageBoxViewModel(message, title, "Yes", "Cancel", true);
            return ShowWindow(vm);
        }

        public void ShowMessage(string message, string DialogBoxName = "Information")
        {
            // FALSE = Info (Blue button, Hide Cancel)
            var vm = new CustomMessageBoxViewModel(message, DialogBoxName, "OK", "", false);
            ShowWindow(vm);
        }

        public void ShowWarning(string message)
        {
            // FALSE = Info (Blue button, Hide Cancel)
            var vm = new CustomMessageBoxViewModel(message, "Warning", "OK", "", false);
            ShowWindow(vm);
        }

        public UserInputResult UserInput( string field1title,string field1value,string field2title, string field2value )
        {
            var vm = new CustomUserInputBoxViewModel("","Save Recipe","Save","Cancle" ,field1title,field1value,field2title,field2value,true);
            bool dialogResult = ShowInputWindow(vm);
            return new UserInputResult
            {
                Confirmed = dialogResult,
                Field1Value = vm.Field1Value,
                Field2Value = vm.Field2Value
            };
        }





        // Helper method to reduce code duplication
        private bool ShowWindow(CustomMessageBoxViewModel vm)
        {
            var msgBox = new CustomMessageBox();
            if (Application.Current.MainWindow != null)
            {
                msgBox.Owner = Application.Current.MainWindow;
            }
            msgBox.Initialize(vm);
            var result = msgBox.ShowDialog();
            return result == true;
        }

        private bool ShowInputWindow(CustomUserInputBoxViewModel vm)
        {
            var msgBox = new CustomUserInputBox();
            if (Application.Current.MainWindow != null)
            {
                msgBox.Owner = Application.Current.MainWindow;
            }
            msgBox.Initialize(vm);
            var result = msgBox.ShowDialog();
            return result == true;
        }
    }

}
