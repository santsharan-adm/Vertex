using IPCSoftware.App.Bending.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.UI.CommonViews;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace IPCSoftware.App.Bending
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //}

            //protected override void OnInitialized(EventArgs e)
            //{
            //    base.OnInitialized(e);
            //    this.Loaded += MainWindow_Loaded;
            //}

            //private void MainWindow_Loaded(object sender, RoutedEventArgs e)
            //{
            var vm = App.ServiceProvider.GetRequiredService<MainWindowViewModelBending>();
            DataContext = vm;

            var nav = App.ServiceProvider.GetRequiredService<INavigationService>();

            ContentControl control = MainPage.GetMainContent() as ContentControl;
            ContentControl ribbonHost = MainPage.GetRibbonHost() as ContentControl;
            nav.Configure(control, ribbonHost);

            // Set the MainPage DataContext to the ViewModel so sidebar works
            MainPage.DataContext = vm;

            var ribbonView = new RibbonView { DataContext = vm.RibbonVM };

            // Load Ribbon
            // nav.NavigateTop(ribbonView);
            nav.NavigateMain<LoginView>();
        }
    }
}