using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Bending1MonitorView : UserControl
    {
        public Bending1MonitorView()
        {
            InitializeComponent();

            this.DataContext = new Bending1MonitorViewModel();
        }
    }
}