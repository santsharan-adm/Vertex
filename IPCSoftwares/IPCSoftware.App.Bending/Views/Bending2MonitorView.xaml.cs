using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Bending2MonitorView : UserControl
    {
        public Bending2MonitorView(Bending2MonitorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}