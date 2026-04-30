using IPCSoftware.App.Bending.ViewModels;
using IPCSoftware.App.Bending.Views;
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

            //var vm = App.ServiceProvider.GetService<MainWindowViewModelBending>();
            //DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModelBending>(); ;

            //var nav = App.ServiceProvider.GetService<INavigationService>();
            //nav.Configure(MainContent, RibbonHost);

            var uc1 = new UserControl1();
            uc1.DataContext = this;

            MainPage.SetContent(uc1);
            MainPage.SetTopBarVisibility(Visibility.Visible);



            // START WITH LOGIN ONLY



            
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
            var vm = App.ServiceProvider.GetService<MainWindowViewModelBending>();
            DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModelBending>();
            var nav = (INavigationService)App.ServiceProvider.GetService(typeof(INavigationService));

            ContentControl control = MainPage.GetMainContent() as ContentControl;
            ContentControl ribbonHost = MainPage.GetRibbonHost() as ContentControl;
            nav.Configure(control, ribbonHost);


            var ribbonView = new RibbonView { DataContext = this };

            // Load Ribbon
            nav.NavigateTop(ribbonView);
            //nav.NavigateMain<LoginView>();
        }
    }
}