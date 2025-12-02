using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public partial class FullImageViewModel :BaseViewModel
    {

        private string _imagePath;

        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        // 2. The Logic (Close Command)
        // We use an Action to request the View to close itself (MVVM-safe way)
        public Action RequestClose;

        public ICommand CloseCommand { get; }

        // Constructor Injection: Pass the dependency here!
        public FullImageViewModel(string imagePath)
        {
            ImagePath = imagePath;
            CloseCommand = new RelayCommand(Close);
        }

        private void Close()
        {
            RequestClose?.Invoke();
        }
    }
}
