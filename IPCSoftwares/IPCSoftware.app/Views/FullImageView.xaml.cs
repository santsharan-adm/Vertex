using IPCSoftware.UI.CommonViews.ViewModels;
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

            // TODO: Phase 2 - Uncomment after moving FullImageViewModel to UI.CommonViews
            try
            {
                // TEMP: Disabled - FullImageViewModel not yet migrated
                // var viewModel = new FullImageViewModel(...);
                // viewModel.RequestClose += () => this.Close();
                // this.DataContext = viewModel;

                MessageBox.Show("FullImageView: ViewModel not yet migrated (Phase 2 pending)");
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
