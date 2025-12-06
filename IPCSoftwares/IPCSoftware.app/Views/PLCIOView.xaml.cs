using IPCSoftware.App.Services;
using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
    public partial class PLCIOView : UserControl
    {
        public PLCIOView()
        {
            InitializeComponent();

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            (DataContext as IDisposable)?.Dispose();
        }
    }
}
