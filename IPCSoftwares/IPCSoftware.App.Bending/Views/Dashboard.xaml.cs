using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Views
{
    /// <summary>
    /// Dashboard.xaml code-behind
    /// Note: DataContext is set externally via App or dependency injection
    /// This ensures clean separation of concerns following MVVM pattern
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
            // DataContext is intentionally NOT set here
            // It should be set by the parent container or through dependency injection
            // This maintains proper MVVM separation and testability
        }
    }
}