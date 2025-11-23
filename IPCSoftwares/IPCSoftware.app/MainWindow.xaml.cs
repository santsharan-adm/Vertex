using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IPCSoftware.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = App.ServiceProvider.GetService<MainWindowViewModel>();
            DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModel>(); ;

            var nav = App.ServiceProvider.GetService<INavigationService>();
            nav.Configure(MainContent, RibbonHost);

            // START WITH LOGIN ONLY
               
            nav.NavigateMain<LoginView>();
        }


    }
}
