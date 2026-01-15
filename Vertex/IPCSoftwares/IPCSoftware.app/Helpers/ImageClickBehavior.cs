using Microsoft.Xaml.Behaviors;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace IPCSoftware.App.Helpers
{
    /// Behavior that allows an Image control to act like a button.
    /// When the image is clicked, it executes a bound ICommand from the ViewModel.
    
    public class ImageClickBehavior : Behavior<System.Windows.Controls.Image>
    {
        /// The command that will be executed when the image is clicked.
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        // DependencyProperty to allow binding the command in XAML

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ImageClickBehavior));

        /// Called when the behavior is attached to the Image control.
        /// Subscribes to the MouseDown event.
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDown += ImageClicked;              // Attach click handler
        }

        /// Called when the behavior is detached from the Image control.
        /// Unsubscribes from the MouseDown event to prevent memory leaks.
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseDown -= ImageClicked;              // Remove click handler
        }

        /// Executes the bound command when the image is clicked.
        /// Passes the Image’s DataContext as the command parameter.
        private void ImageClicked(object sender, MouseButtonEventArgs e)
        {
            if (Command?.CanExecute(AssociatedObject.DataContext) == true)
                Command.Execute(AssociatedObject.DataContext);
        }
    }
    /// Converts a boolean value to its inverse.
    /// True → False, False → True.
    /// Commonly used in bindings to reverse enable/disable logic.
    public class InverseBooleanConverter : IValueConverter
    {
        /// Inverts a boolean value (True becomes False and vice versa).
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If value is a bool, return its opposite; otherwise, return false
            if (value is bool booleanValue) return !booleanValue;
            return false;
        }

        /// Reverse conversion is not implemented (one-way use only).
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
