using System.Collections.ObjectModel;
using System.Windows.Input;
using IPCSoftware.App.Bending.Models;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using System.Windows.Input;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class PostBendingViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        public ICommand PreviousCommand { get; }
        
       // public ICommand PreviousCommand => new RelayCommand(ExecutePrevious);

        private string _batchNo = "1234321";
        public string BatchNo
        {
            get => _batchNo;
            set { _batchNo = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ProductData> ProductList { get; set; }

        public PostBendingViewModel(INavigationService navigationService)
        {
            ProductList = new ObservableCollection<ProductData>
            {
                new ProductData { ProductName = "Product 1", QRCode = "ABCDQWER1234TYUW" },
                new ProductData { ProductName = "Product 2", QRCode = "ABCDQWER1234TYUW" },
                new ProductData { ProductName = "Product 3", QRCode = "ABCDQWER1234TYUW" },
                new ProductData { ProductName = "Product 4", QRCode = "ABCDQWER1234TYUW" }
            };
            _navigationService = navigationService;
            PreviousCommand = new RelayCommand(ExecutePrevious);
        }

       // NextCommand = new RelayCommand(ExecuteNext);
        private void ExecutePrevious()
        {
            _navigationService.NavigateToDashboard3();
        }
    }
}