using System.Windows;
using System.Windows.Controls;
using IPCSoftware.App.Bending.Views;

namespace IPCSoftware.App.Bending
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContentArea.Content = new UserControl1();
        }

        private void OpenMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuColumn.Width = new GridLength(250);
        }

        private void CloseMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuColumn.Width = new GridLength(0);
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;

            string target = btn.Tag.ToString();

            switch (target)
            {
                case "B1": MainContentArea.Content = new Bending1MonitorView(); break;
                case "B2": MainContentArea.Content = new Bending2MonitorView(); break;
                case "B3": MainContentArea.Content = new Bending3MonitorView(); break;
                case "UC1": MainContentArea.Content = new UserControl1(); break;
                case "UC2": MainContentArea.Content = new UserControl2(); break;
            }

            CloseMenu_Click(null, null);
        }
    }
}







//Show only single screen


//using System.Windows;

//namespace IPCSoftware.App.Bending
//{
//    public partial class MainWindow : Window
//    {
//        public MainWindow()
//        {
//            InitializeComponent();
//        }
//    }
//}