using IPCSoftware.App.ViewModels;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
 public partial class ReportViewerView : UserControl
 {
 public ReportViewerView(ReportViewerViewModel viewModel)
 {
 InitializeComponent();
 DataContext = viewModel;
 }
 }
}
