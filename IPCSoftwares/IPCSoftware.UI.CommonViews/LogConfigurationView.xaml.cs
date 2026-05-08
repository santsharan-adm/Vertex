using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IPCSoftware.UI.CommonViews
{
    /// <summary>
    /// Interaction logic for LogConfigurationView.xaml
    /// </summary>
    public partial class LogConfigurationView : UserControl, IDialogService
    {
        public LogConfigurationView()
        {
            InitializeComponent();
        }

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

        public string ShowBrowseDialoge(string message)
        {
            
                var dialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    Title = message,
                    AllowNonFileSystemItems = false,
                    Multiselect = false
                };

                if ((CommonFileDialogResult)dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    return dialog.FileName;
                }

            return string.Empty;
           
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
    }
}

