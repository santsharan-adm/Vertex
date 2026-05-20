
using IPCSoftware.App.Helpers;
// Adjust these namespaces to match your project structure
using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace IPCSoftware.App.ViewModels
{
    public class CustomUserInputBoxViewModel : ObservableObjectVM
    {
        private string _title;
        private string _message;
        private string _yesText;
        private string _noText;
        private bool _isCancelVisible;
        private SolidColorBrush _yesButtonBrush;
        private string _field1Title;
        private string _field2Title;
        private string _field1Value;
        private string _field2Value;

        public Action<bool> CloseRequested;

        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }

        // Constructor
        public CustomUserInputBoxViewModel(string message,
            string title, string yesText, string noText, string field1Title, string field1Value, string field2Title, string field2Value,
            bool isConfirmation)
        {
            _message = message;
            _title = title;
            _yesText = yesText;
            _noText = noText;
            _field1Title = field1Title;
            _field1Value = field1Value;
            _field2Title = field2Title;            
            _field2Value = field2Value;

            // Logic: If it's a confirmation (Yes/No), show Cancel button and Red color.
            // If it's just Info (OK), hide Cancel button and use Blue color.
            _isCancelVisible = isConfirmation;

            // Define Colors manually or load from resources if you prefer strict MVVM. 
            // For simplicity, we set them here.
            if (isConfirmation)
            {
                // Red for Danger/Logout
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
            }
            else
            {
                // Blue for Info/OK (Matches your Theme InfoBrush)
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            }

            YesCommand = new RelayCommand(OnYes);
            NoCommand = new RelayCommand(OnNo);
        }

        // Properties
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public string YesText { get => _yesText; set => SetProperty(ref _yesText, value); }
        public string NoText { get => _noText; set => SetProperty(ref _noText, value); }

        public string Field1Title { get => _field1Title; set => SetProperty(ref _field1Title, value); } 

        public string Field2Title { get => _field2Title; set => SetProperty(ref _field2Title, value); }

        public string Field1Value { get => _field1Value; set => SetProperty(ref _field1Value, value); }

        public string Field2Value { get => _field2Value; set => SetProperty(ref _field2Value,value) ; }

        public bool IsCancelVisible { get => _isCancelVisible; set => SetProperty(ref _isCancelVisible, value); }
        public SolidColorBrush YesButtonBrush { get => _yesButtonBrush; set => SetProperty(ref _yesButtonBrush, value); }

        private void OnYes() => CloseRequested?.Invoke(true);
        private void OnNo() => CloseRequested?.Invoke(false);
    }
}