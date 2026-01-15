using IPCSoftware.App.Helpers;
// Adjust these namespaces to match your project structure
using IPCSoftware.App.ViewModels;
using IPCSoftware.Shared;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace IPCSoftware.App.ViewModels
{
    /// ViewModel for a custom, reusable message box dialog.
    /// Supports both informational (OK) and confirmation (Yes/No) message types.
    /// Controls button visibility, text, and color dynamically based on context.
    public class CustomMessageBoxViewModel : BaseViewModel
    {

        // Private Backing Fields

        private string _title;                          // Title of the message box (e.g., "Warning", "Info")
        private string _message;                        // Main message text displayed in the dialog
        private string _yesText;                        // Text for Yes/OK button (e.g., "Yes" or "OK")
        private string _noText;                         // Text for No/Cancel button
        private bool _isCancelVisible;                  // Controls visibility of No/Cancel button
        private SolidColorBrush _yesButtonBrush;        // Button color depending on type (Info = Blue, Warning = Red)



        // --------------------Events-------------------//

        /// Action event triggered when the dialog should close.
        /// Parameter: true = Yes/OK, false = No/Cancel.
        /// 
        public Action<bool> CloseRequested;

        // --------------------- Commands (Bound to UI Buttons) ---------------------//
        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }

        // --------------------------Constructor --------------------------//

        /// Initializes a new instance of the CustomMessageBoxViewModel.
        /// Configures title, message, button labels, visibility, and colors
        /// based on whether it's a confirmation (Yes/No) or info (OK) message.
        public CustomMessageBoxViewModel(string message, string title, string yesText, string noText, bool isConfirmation)
        {
            _message = message;
            _title = title;
            _yesText = yesText;
            _noText = noText;

            // If confirmation dialog, show Cancel button and use Red theme.
            // If info-only dialog, hide Cancel button and use Blue theme.
            _isCancelVisible = isConfirmation;

            // Define button color (could be loaded from ResourceDictionary in MVVM-friendly setup)
            if (isConfirmation)
            {
                // Red for danger or critical confirmation (e.g., delete, logout)
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
            }
            else
            {
                // Blue for informational or success confirmation
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            }
            // Initialize commands
            YesCommand = new RelayCommand(OnYes);
            NoCommand = new RelayCommand(OnNo);
        }

        // --------------------Public Properties (Bindable)---------------------//


        /// Title displayed at the top of the dialog.
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        /// Main content/message shown in the dialog body.
        public string Message { get => _message; set => SetProperty(ref _message, value); }

        /// Text label for the primary action button (e.g., Yes/OK).
        public string YesText { get => _yesText; set => SetProperty(ref _yesText, value); }

        /// Text label for the secondary button (e.g., No/Cancel).
        public string NoText { get => _noText; set => SetProperty(ref _noText, value); }

        /// Determines whether the Cancel/No button is visible.
        /// True for confirmation dialogs; false for info-only popups.

        public bool IsCancelVisible { get => _isCancelVisible; set => SetProperty(ref _isCancelVisible, value); }

        /// Brush used to style the Yes/OK button background color.
        public SolidColorBrush YesButtonBrush { get => _yesButtonBrush; set => SetProperty(ref _yesButtonBrush, value); }


        // -------------------Command Handlers-------------------//

        /// Handles click event for the Yes/OK button.
        /// Invokes CloseRequested(true) to signal confirmation.
        private void OnYes() => CloseRequested?.Invoke(true);

        /// Handles click event for the No/Cancel button.
        /// Invokes CloseRequested(false) to signal cancellation.
        private void OnNo() => CloseRequested?.Invoke(false);
    }
}