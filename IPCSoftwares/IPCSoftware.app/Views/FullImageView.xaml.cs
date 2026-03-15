using IPCSoftware.UI.CommonViews.ViewModels;
using IPCSoftware.Shared.Models;
using System;
using System.Windows;
using System.Windows.Input;

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

            try
            {
                // 1. Create ViewModel with all limits
                var viewModel = new FullImageViewModel(img, title, xMin, xMax, yMin, yMax,
                    zMin, zMax, xUOM, yUOM, zUOM
               );

                // 2. Hook up the Close Action
                viewModel.RequestClose += () => this.Close();

                // 3. Set DataContext
                this.DataContext = viewModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullImageView init error: {ex.Message}");
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }

}
