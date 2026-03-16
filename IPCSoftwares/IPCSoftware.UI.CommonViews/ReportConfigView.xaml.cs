using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IPCSoftware.UI.CommonViews.ViewModels;

namespace IPCSoftware.UI.CommonViews
{
    /// <summary>
    /// Interaction logic for ReportConfigView.xaml
    /// </summary>
    public partial class ReportConfigView : System.Windows.Controls.UserControl
    {
        public ReportConfigView(ReportConfigViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
