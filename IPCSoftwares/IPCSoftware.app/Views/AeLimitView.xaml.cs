using System;
using System.Windows;
using IPCSoftware.App.ViewModels;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
    public partial class AeLimitView : UserControl
    {
        public AeLimitView(AeLimitViewModel viewModel)
        {
            LoadContent();
            DataContext = viewModel;
        }

        private void LoadContent()
        {
            var resourceLocater = new Uri("/IPCSoftware.App;component/Views/AeLimitView.xaml", UriKind.Relative);
            Application.LoadComponent(this, resourceLocater);
        }
    }
}
