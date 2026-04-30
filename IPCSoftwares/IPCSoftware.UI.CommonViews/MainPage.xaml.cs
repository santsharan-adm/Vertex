using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IPCSoftware.UI.CommonViews
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // Expose internal controls for NavigationService configuration
        public ContentControl GetMainContent() => MainContent;
        public ContentControl GetRibbonHost() => RibbonHost;

        public void SetContent(UserControl content)
        {
            MainContent.Content = content;
        }

        public void SetTopBarVisibility(Visibility visibility)
        {
            TopBar.Visibility = visibility;
        }

    }
}
