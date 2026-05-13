using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    /// <summary>
    /// Interaction logic for PostBendingMonitor.xaml
    /// </summary>
    public partial class PostBendingMonitor : UserControl
    {
        public PostBendingMonitor(PostBendingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.Initialize();
        }
    }
}
