using System.Collections.ObjectModel;
using IPCSoftware.App.Bending.Models;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class PostBendingViewModel : BaseViewModel
    {
        private string _batchNo = "1234321";
        public string BatchNo
        {
            get => _batchNo;
            set { _batchNo = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ProductData> ProductList { get; set; }

        public PostBendingViewModel()
        {
            ProductList = new ObservableCollection<ProductData>
            {
                new ProductData { ProductName = "Product 1", QRCode = "ABCDQWER1234TYUW" },
                new ProductData { ProductName = "Product 2", QRCode = "ABCDQWER1234TYUW" },
                new ProductData { ProductName = "Product 3", QRCode = "ABCDQWER1234TYUW" },
                new ProductData { ProductName = "Product 4", QRCode = "ABCDQWER1234TYUW" }
            };
        }
    }
}