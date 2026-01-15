using IPCSoftware.App.ViewModels;
using System.Windows;

namespace IPCSoftware.App.Views
{
    public partial class ProcessSequenceWindow : Window
    {
        public ProcessSequenceWindow(ProcessSequenceViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (DataContext is ProcessSequenceViewModel vm)
            {
                vm.Dispose();
            }
            base.OnClosed(e);
        }
    }
}
