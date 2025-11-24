using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IPCSoftware.App.Views
{
    public partial class FullImageView : Window
    {
        public FullImageView(string imagePath)
        {
            InitializeComponent();
            FullImage.Source = new BitmapImage(new Uri(imagePath));
        }
    }

}
