using Microsoft.Xaml.Behaviors;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace IPCSoftware.App.Helpers
{
    /// Behavior that allows an Image control to execute an ICommand when clicked.
    /// Useful for MVVM scenarios where Image does not support Command binding natively.
    
    public class ImageClickBehavior : Behavior<System.Windows.Controls.Image>
    {
        /// Command to be executed when the Image is clicked.
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// DependencyProperty for the Command, enabling XAML binding.
        
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ImageClickBehavior));

        /// Called when the behavior is attached to the Image.
        /// Subscribes to the MouseDown event.
        protected override void OnAttached()
        {
            base.OnAttached();

            // Attach mouse click event to the Image
            AssociatedObject.MouseDown += ImageClicked;
        }

        /// Called when the behavior is detached from the Image.
        /// Unsubscribes from the MouseDown event to prevent memory leaks.
        protected override void OnDetaching()
        {
            base.OnDetaching();
            // Detach mouse click event
            AssociatedObject.MouseDown -= ImageClicked;
        }

        /// Handles the Image click event and executes the bound command.
        private void ImageClicked(object sender, MouseButtonEventArgs e)
        {
            // Execute the command using the Image's DataContext as parameter
            if (Command?.CanExecute(AssociatedObject.DataContext) == true)
                Command.Execute(AssociatedObject.DataContext);
        }
    }

    /// Converts a boolean value to its inverse.
    /// true  -> false
    /// false -> true
    public class InverseBooleanConverter : IValueConverter
    {
        /// Inverts the input boolean value.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return the inverted boolean value
            if (value is bool booleanValue) return !booleanValue;

            // Default fallback
            return false;
        }

        /// ConvertBack is not implemented because this converter
        /// is intended for one-way binding only.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
