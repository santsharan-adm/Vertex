using System.Windows.Controls;
using IPCSoftware.UI.CommonViews.ViewModels;

namespace IPCSoftware.UI.CommonViews
{
    public partial class LiveOeeView : UserControl
    {
        public LiveOeeView()
        {
            InitializeComponent();

            // STEP 1: This runs ViewModel constructor
            //this.DataContext = new LiveOeeViewModel();
        }
    }
}
