using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard(DashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}