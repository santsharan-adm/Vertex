using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.Helpers
{
    /// Provides an attached property that automatically wires up a ViewModel 
    /// to a View (such as a UserControl or Window) using dependency injection.
    /// When a view sets the "AutoWireViewModel" attached property to a ViewModel type,
    /// this class will resolve that ViewModel from the application's ServiceProvider
    /// and assign it to the view's DataContext automatically.
    public static class ViewModelLocator
    {
        /// Attached property definition that holds the ViewModel type to wire.
        /// Example (in XAML):
        ///     helpers:ViewModelLocator.AutoWireViewModel="{x:Type vm:MainViewModel}"
        public static readonly DependencyProperty AutoWireViewModelProperty =
            DependencyProperty.RegisterAttached(
                "AutoWireViewModel",
                typeof(Type),
                typeof(ViewModelLocator),
                new PropertyMetadata(null, AutoWireViewModelChanged));

        /// Getter for the AutoWireViewModel attached property.
        public static Type GetAutoWireViewModel(DependencyObject obj)
        {
            return (Type)obj.GetValue(AutoWireViewModelProperty);
        }

        /// Setter for the AutoWireViewModel attached property.
        public static void SetAutoWireViewModel(DependencyObject obj, Type value)
        {
            obj.SetValue(AutoWireViewModelProperty, value);
        }

        /// Called when the AutoWireViewModel property value changes.
        /// This method resolves the specified ViewModel type from the 
        /// application's dependency injection container (App.ServiceProvider)
        /// and assigns it to the view's DataContext.
        private static void AutoWireViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
            // Ensure the new value is a valid Type
            if (e.NewValue is Type viewModelType)
            {
                // Resolve the ViewModel instance from DI container
                var viewModel = App.ServiceProvider.GetService(viewModelType);
                // If the target object is a FrameworkElement, set its DataContext
                if (d is FrameworkElement element)
                {
                    element.DataContext = viewModel;
                }
            }
        }
    }
}
