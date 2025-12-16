using IPCSoftware.App.Helpers;
// Adjust these namespaces to match your project structure
using IPCSoftware.App.ViewModels;
using IPCSoftware.Shared;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace IPCSoftware.App.ViewModels
{
    public class CustomMessageBoxViewModel : BaseViewModel
    {
        private string _title;
        private string _message;
        private string _yesText;
        private string _noText;
        private bool _isCancelVisible;
        private SolidColorBrush _yesButtonBrush;

        public Action<bool> CloseRequested;

        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }

        // Constructor
        public CustomMessageBoxViewModel(string message, string title, string yesText, string noText, bool isConfirmation)
        {
            try
            {
                _message = message;
                _title = title;
                _yesText = yesText;
                _noText = noText;

                _isCancelVisible = isConfirmation;

                if (isConfirmation)
                {
                    _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                }
                else
                {
                    _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
                }

                YesCommand = new RelayCommand(OnYes);
                NoCommand = new RelayCommand(OnNo);
            }
            catch (Exception)
            {
                
            }
        }

        // Properties
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public string YesText { get => _yesText; set => SetProperty(ref _yesText, value); }
        public string NoText { get => _noText; set => SetProperty(ref _noText, value); }

        public bool IsCancelVisible { get => _isCancelVisible; set => SetProperty(ref _isCancelVisible, value); }
        public SolidColorBrush YesButtonBrush { get => _yesButtonBrush; set => SetProperty(ref _yesButtonBrush, value); }

        private void OnYes() => CloseRequested?.Invoke(true);
        private void OnNo() => CloseRequested?.Invoke(false);
    }
}
