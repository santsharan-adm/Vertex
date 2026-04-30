using IPCSoftware.UI.CommonViews.ViewModels;
using System.Windows.Controls;
using IPCSoftware.App;

namespace IPCSoftware.UI.CommonViews
{
    public partial class ServiceStartupView : UserControl
    {
        public ServiceStartupView()
        {
            InitializeComponent();
          //  this.DataContext = App.ServiceProvider?.GetService<ServiceStartupViewModel>();
        }
    }
}