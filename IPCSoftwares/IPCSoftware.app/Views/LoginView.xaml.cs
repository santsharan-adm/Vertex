using IPCSoftware.App;
using IPCSoftware.App.ViewModels;
using IPCSoftware.AppLogger.Interfaces;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IPCSoftware.App.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel(
            App.ServiceProvider.GetService<IAuthService>(),
            App.ServiceProvider.GetService<INavigationService>(),
            App.ServiceProvider.GetService<IDialogService>(),
            App.ServiceProvider.GetService<IAppLogger>(),
            App.ServiceProvider.GetService<MainWindowViewModel>());

        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }
    }
}

