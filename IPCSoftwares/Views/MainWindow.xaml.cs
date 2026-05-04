using System.Windows;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Shutdown();
            }
        }
    }
}
