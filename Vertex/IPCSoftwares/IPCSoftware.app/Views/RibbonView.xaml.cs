using IPCSoftware.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
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
