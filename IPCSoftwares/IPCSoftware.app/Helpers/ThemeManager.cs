using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.Helpers
{
    public static class ThemeManager
    {
        // Define an Attached Property "ThemeSource"
        public static readonly DependencyProperty ThemeSourceProperty =
            DependencyProperty.RegisterAttached(
                "ThemeSource",
                typeof(string),
                typeof(ThemeManager),
                new PropertyMetadata(null, OnThemeSourceChanged));

        public static string GetThemeSource(DependencyObject obj)
        {
            return (string)obj.GetValue(ThemeSourceProperty);
        }

        public static void SetThemeSource(DependencyObject obj, string value)
        {
            obj.SetValue(ThemeSourceProperty, value);
        }

        // This runs whenever the property changes
        private static void OnThemeSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && e.NewValue is string newSource && !string.IsNullOrEmpty(newSource))
            {
                var newDict = new ResourceDictionary
                {
                    Source = new Uri(newSource, UriKind.RelativeOrAbsolute)
                };

                // Clear old dictionaries and add the new one
                element.Resources.MergedDictionaries.Clear();
                element.Resources.MergedDictionaries.Add(newDict);
            }
        }
    }
}
