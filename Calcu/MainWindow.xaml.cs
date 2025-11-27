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

namespace Calcu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Addbtn_Click(object sender, RoutedEventArgs e)
        {

            Resultbox.Text = Convert.ToString(Calculator.Calculator1(Convert.ToDouble(Abox.Text), Convert.ToDouble(Bbox.Text), Convert.ToDouble(Hbox.Text)));


        }

        private void Clearbtn_Click(object sender, RoutedEventArgs e)
        {

            Abox.Clear();
            Bbox.Clear();
            Hbox.Clear();
            Resultbox.Clear();

        }

        private void Closebtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}



