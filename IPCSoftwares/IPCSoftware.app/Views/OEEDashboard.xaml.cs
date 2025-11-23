
using IPCSoftware.App.Controls;
using IPCSoftware.App.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
    public partial class OEEDashboard : UserControl
    {
        public OEEDashboard()
        {
            // 1. INIT COMPONENT FIRST
            InitializeComponent();

            // 2. SET DATACONTEXT AFTER (but only in runtime)
            if (!DesignerProperties.GetIsInDesignMode(this))
                DataContext = new OEEDashboardViewModel();
        }

        private void PieChart_Loaded(object sender, RoutedEventArgs e)
        {
            var chart = sender as PieChart;
            chart.InvalidateVisual();
        }



    }
}
