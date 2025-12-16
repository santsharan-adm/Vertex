using IPCSoftware.App.Controls;
using IPCSoftware.App.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System;

namespace IPCSoftware.App.Views
{
    public partial class OEEDashboard : UserControl
    {
        public OEEDashboard()
        {
            InitializeComponent();
            //DataContext = new OEEDashboardViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            (DataContext as IDisposable)?.Dispose();
        }

        // Unloaded logic is handled by the LifecycleBehavior now
        // The rest of the event handlers (MouseDown, Loaded) remain if they are not binding related
    }
}