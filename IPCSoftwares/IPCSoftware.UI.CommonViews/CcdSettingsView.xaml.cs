using System.Windows;
using System.Windows.Controls;
using IPCSoftware.UI.CommonViews.ViewModels;

namespace IPCSoftware.UI.CommonViews
{
    public partial class CcdSettingsView : UserControl
    {
        public CcdSettingsView()
        {
            InitializeComponent();

            // If DataContext not set by host/DI, provide a default ViewModel so designer/runtime won't crash.
            if (this.DataContext == null)
            {
                this.DataContext = new CcdSettingsViewModel();
            }
        }
    }
}



