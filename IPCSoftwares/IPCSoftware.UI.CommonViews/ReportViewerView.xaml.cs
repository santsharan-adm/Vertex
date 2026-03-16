using IPCSoftware.UI.CommonViews.ViewModels;
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

namespace IPCSoftware.UI.CommonViews
{
    /// <summary>
    /// Interaction logic for ReportViewerView.xaml
    /// </summary>
    public partial class ReportViewerView : System.Windows.Controls.UserControl
    {
        public ReportViewerView(ReportViewerViewModel viewmodelrepo)
        {
            InitializeComponent();
            DataContext = viewmodelrepo;
        }
    }
}
