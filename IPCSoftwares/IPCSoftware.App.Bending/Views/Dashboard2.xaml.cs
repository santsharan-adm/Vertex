using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Dashboard2 : UserControl
    {
        public Dashboard2()
        {
            InitializeComponent();
            this.DataContext = new DashboardViewModel();
        }
    }
}