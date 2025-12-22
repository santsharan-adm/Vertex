using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.Helpers
{

    /// ViewModelLocator helps automatically assign a ViewModel
    /// to a View using an attached property.
    /// Commonly used in MVVM architecture.
    public static class ViewModelLocator
    {
        /// Attached property that holds the ViewModel type
        /// to be resolved and assigned as DataContext.
        
        public static readonly DependencyProperty AutoWireViewModelProperty =
            DependencyProperty.RegisterAttached(
                "AutoWireViewModel",                                                    // Property name
                typeof(Type),                                                           // Property type (ViewModel Type)
                typeof(ViewModelLocator),                                               // Owner type
                new PropertyMetadata(null, AutoWireViewModelChanged));


        /// Gets the AutoWireViewModel attached property value.
        public static Type GetAutoWireViewModel(DependencyObject obj)
        {
            return (Type)obj.GetValue(AutoWireViewModelProperty);
        }

        /// Sets the AutoWireViewModel attached property value.
        public static void SetAutoWireViewModel(DependencyObject obj, Type value)
        {
            obj.SetValue(AutoWireViewModelProperty, value);
        }

        /// Called automatically when AutoWireViewModel property changes.
        /// Resolves the ViewModel from the DI container and assigns it
        /// to the View's DataContext.
        private static void AutoWireViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
            // Ensure the new value is a valid ViewModel type
            if (e.NewValue is Type viewModelType)
            {
                // Resolve the ViewModel instance from the application's service provider
                var viewModel = App.ServiceProvider.GetService(viewModelType);

                // Assign the ViewModel as DataContext if the target is a FrameworkElement
                if (d is FrameworkElement element)
                {
                    element.DataContext = viewModel;
                }
            }
        }
    }
}
