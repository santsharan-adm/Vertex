using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.Helpers
{
    public static class ViewModelLocator
    {
        public static readonly DependencyProperty AutoWireViewModelProperty =
            DependencyProperty.RegisterAttached(
                "AutoWireViewModel",
                typeof(Type),
                typeof(ViewModelLocator),
                new PropertyMetadata(null, AutoWireViewModelChanged));

        public static Type GetAutoWireViewModel(DependencyObject obj)
        {
            return (Type)obj.GetValue(AutoWireViewModelProperty);
        }

        public static void SetAutoWireViewModel(DependencyObject obj, Type value)
        {
            obj.SetValue(AutoWireViewModelProperty, value);
        }

        private static void AutoWireViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Type viewModelType)
            {
                var viewModel = App.ServiceProvider.GetService(viewModelType);
                if (d is FrameworkElement element)
                {
                    element.DataContext = viewModel;
                }
            }
        }
    }
}
