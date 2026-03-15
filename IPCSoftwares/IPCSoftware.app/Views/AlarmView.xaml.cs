// Inside AlarmView.xaml.cs (Code-Behind)

using IPCSoftware.UI.CommonViews.ViewModels;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
    public partial class AlarmView : UserControl
    {
        // 🚨 Assumes ViewModel is injected via Dependency Injection
        public AlarmView()
        {
            InitializeComponent();
           // this.DataContext = viewModel;
        }
    }
}