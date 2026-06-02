using IPCSoftware.App.Bending.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IPCSoftware.App.Bending.Views
{
    public partial class BendingDash : UserControl
    {
        private readonly FifoMonitorViewModel _vm;

        public BendingDash(FifoMonitorViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = _vm;
            _vm.PropertyChanged += OnViewModelPropertyChanged;
            _vm.Initialize();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FifoMonitorViewModel.IsTriggerActive) && _vm.IsTriggerActive)
            {
                var storyboard = (Storyboard)FindResource("TriggerAnimation");
                storyboard?.Begin(this, true);
            }
        }
    }
}
