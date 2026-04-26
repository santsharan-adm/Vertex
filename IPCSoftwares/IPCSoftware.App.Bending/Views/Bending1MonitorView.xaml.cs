using IPCSoftware.App.Bending.ViewModels;
using System.Windows;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Bending1MonitorView : Window
    {
        public Bending1MonitorView()
        {
            InitializeComponent();
            // ViewModel ko assign kar rahe hain
            this.DataContext = new Bending1MonitorViewModel();
        }
    }
}