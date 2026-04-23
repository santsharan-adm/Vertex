using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IPCSoftware.App.Bending
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DashBoard1_Loaded(this, new RoutedEventArgs());
        }

        //private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        //private void DashBoard1_Loaded(object sender, RoutedEventArgs e)

        //private void DashBoard1_Loaded(object sender, RoutedEventArgs e)
        private void DashBoard1_Loaded(object sender, RoutedEventArgs e) 

        {

        }
    }
}





//using System.Windows;

//namespace IPCSoftware.App.Bending
//{
//    public partial class MainWindow : Window
//    {
//        public MainWindow()
//        {
//            InitializeComponent();

//            // App open hote hi default UserControl1 load hoga
//            MainContentArea.Content = new Views.UserControl1();
//        }

//        private void BtnDashboard1_Click(object sender, RoutedEventArgs e)
//        {
//            // Button 1 click par UserControl1
//            MainContentArea.Content = new Views.UserControl1();
//        }

//        private void BtnDashboard2_Click(object sender, RoutedEventArgs e)
//        {
//            // Button 2 click par UserControl2
//            MainContentArea.Content = new Views.UserControl2();
//        }
//    }
//}