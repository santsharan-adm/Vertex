using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{

    public partial class Bending3MonitorView : UserControl
    {
        public Bending3MonitorView(Bending3MonitorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}