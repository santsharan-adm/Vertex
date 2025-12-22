using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.Helpers
{
    /// Manages dynamic theme switching using an attached property.
    /// Allows loading ResourceDictionaries at runtime via XAML.
    public static class ThemeManager
    {
        /// Attached property that holds the URI of the theme ResourceDictionary.
        /// Changing this value will automatically reload the theme.
        public static readonly DependencyProperty ThemeSourceProperty =
            DependencyProperty.RegisterAttached(
                "ThemeSource",                                                    // Property name
                typeof(string),                                                   // Property type
                typeof(ThemeManager),                                            // Owner type
                new PropertyMetadata(null, OnThemeSourceChanged));


        /// Gets the ThemeSource attached property value.
        public static string GetThemeSource(DependencyObject obj)
        {
            return (string)obj.GetValue(ThemeSourceProperty);
        }

        /// Sets the ThemeSource attached property value.
        public static void SetThemeSource(DependencyObject obj, string value)
        {
            obj.SetValue(ThemeSourceProperty, value);
        }

        /// Called automatically whenever ThemeSource property changes.
        /// Loads and applies the new theme ResourceDictionary.
        private static void OnThemeSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            // Ensure the target is a FrameworkElement and the new value is a valid string
            if (d is FrameworkElement element && e.NewValue is string newSource && !string.IsNullOrEmpty(newSource))
            {
                // Create a new ResourceDictionary from the provided URI
                var newDict = new ResourceDictionary
                {
                    Source = new Uri(newSource, UriKind.RelativeOrAbsolute)
                };

                // Remove existing merged dictionaries (old theme)
                element.Resources.MergedDictionaries.Clear();

                // Apply the new theme dictionary
                element.Resources.MergedDictionaries.Add(newDict);
            }
        }
    }
}
