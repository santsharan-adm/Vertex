using IPCSoftware.App.ViewModels;
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
using System.Windows.Shapes;

namespace IPCSoftware.App.Views
{

    public partial class CustomUserInputBox : Window
    {
        public CustomUserInputBox()
        {
            InitializeComponent();
        }

        public void Initialize(CustomUserInputBoxViewModel vm)
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
