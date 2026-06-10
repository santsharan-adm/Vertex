using IPCSoftware.App.Bending.Controls;
using IPCSoftware.App.Bending.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace IPCSoftware.App.Bending.Views
{
    /// <summary>
    /// Interaction logic for Dashboard2.xaml
    /// </summary>
    public partial class Dashboard2 : UserControl
    {
        public Dashboard2(Dashboard2ViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.Initialize();
           
        }

        // Handler wired in XAML (or via AddHandler)
        private void StationIndexCC_ButtonClick(object? sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not StationIndexCC control) return;

            var batchNo = control.BatchNo; // adjust type if not string

            //BatchListView?.ItemsSource?.Cast<object?>()
            //    .FirstOrDefault(it => ItemMatchesBatchNo(it, batchNo))
            //    is { } item
            //    && BatchListView.Items.Contains(item)
            //    && BatchListView.ScrollIntoView(item);

            BatchListView.ScrollToBatchNo(batchNo);
        }

        
    }
}
