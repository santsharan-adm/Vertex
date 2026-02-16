using IPCSoftware.App.ViewModels;
using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IPCSoftware.App.Views
{
    public partial class FullImageView : Window
    {
        public FullImageView(
            CameraImageItem img,
            string title,
            double xMin, double xMax,
            double yMin, double yMax,
            double zMin, double zMax,
            string xUOM, string yUOM, string zUOM)
        {
            InitializeComponent();

            // 1. Create ViewModel with all limits
            var viewModel = new FullImageViewModel(img, title, xMin, xMax, yMin, yMax,
                zMin, zMax,xUOM,yUOM, zUOM
                );

            // 2. Hook up the Close Action
            viewModel.RequestClose += () => this.Close();

            // 3. Set DataContext
            this.DataContext = viewModel;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }

}
