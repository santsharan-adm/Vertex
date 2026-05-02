using IPCSoftware.App.Bending.Views;
using IPCSoftware.App.Bending.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Views
{
    public partial class RibbonViewBending : UserControl
    {
        public RibbonViewBending()
        {
            InitializeComponent();
            Debug.WriteLine("RibbonView Loaded");
        }
    }
}
