using IPCSoftware.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace IPCSoftware.App.Views
{
    public partial class FullImageView : Window
    {
        public FullImageView(string imagePath)
        {
            InitializeComponent();

            // 1. Create ViewModel with Dependency
            var viewModel = new FullImageViewModel(imagePath);

            // 2. Hook up the Close Action
            viewModel.RequestClose += () => this.Close();

            // 3. Set DataContext
            this.DataContext = viewModel;
        }

        // Keep drag logic here (View-specific behavior)
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }

}
