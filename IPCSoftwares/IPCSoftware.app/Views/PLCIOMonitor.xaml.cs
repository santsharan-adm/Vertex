using IPCSoftware.App.ViewModels;
using IPCSoftware.Shared.Models;
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

namespace IPCSoftware.App.Views
{
    /// <summary>
    /// Interaction logic for PLCIOMonitor.xaml
    /// </summary>
    public partial class PLCIOMonitor : UserControl
    {
        public PLCIOMonitor()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is PlcIoItem item)
            {
                (DataContext as PlcIoMonitorViewModel)?.RowDoubleClickCommand.Execute(item);
            }
        }
    }
}
