using IPCSoftware.UI.CommonViews.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows.Controls;

namespace IPCSoftware.UI.CommonViews
{
    public partial class RibbonView : UserControl
    {
        public RibbonView()
        {
            InitializeComponent();
            //DataContext = App.ServiceProvider.GetService<RibbonViewModel>();

            Debug.WriteLine("RibbonView Loaded");
            

        }
    }
}
