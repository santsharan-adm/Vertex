using IPCSoftware.Shared;
using System;
using System.Diagnostics;
using System.Windows;

namespace IPCSoftware.Common.WPFExtensions
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
                try
                {
                    var viewModel = ServiceLocator.Current.GetService(viewModelType);
                    if (viewModel == null)
                    {
                        Debug.WriteLine($"[ViewModelLocator] ERROR: Could not resolve {viewModelType.FullName} from DI. Is it registered?");
                        return;
                    }

                    if (d is FrameworkElement element)
                    {
                        element.DataContext = viewModel;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ViewModelLocator] ERROR resolving {viewModelType.FullName}: {ex.Message}");
                }
            }
        }
    }
}
