using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
            this.DataContext = new DashboardViewModel();
        }
    }
}