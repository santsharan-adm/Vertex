using System.Windows.Controls;
using IPCSoftware.App.ViewModels;

namespace IPCSoftware.App.Views
{
    public partial class LiveOeeView : UserControl
    {
        public LiveOeeView()
        {
            InitializeComponent();

            // STEP 1: This runs ViewModel constructor
            this.DataContext = new LiveOeeViewModel();
        }
    }
}
