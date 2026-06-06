using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IPCSoftware.App.Bending.Behaviors
{
    /// <summary>
    /// Attached behavior to control how much a ListView scrolls per mouse-wheel tick.
    /// Usage (XAML):
    /// <!--
    /// <Window xmlns:beh="clr-namespace:IPCSoftware.App.Bending.Behaviors">
    ///   <ListView
    ///       ScrollViewer.CanContentScroll="False" 
    ///       beh:ListViewScrollBehavior.VerticalScrollStep="30"
    ///       ItemsSource="{Binding MyItems}">
    ///       ...
    ///   </ListView>
    /// </Window>
    /// -->
    /// Notes:
    /// - Set ScrollViewer.CanContentScroll="False" to enable pixel-based scrolling (recommended).
    /// - VerticalScrollStep is the pixel amount per mouse-wheel notch (120 delta = 1 notch).
    /// </summary>
    public static class ListViewScrollBehavior
    {
        public static readonly DependencyProperty VerticalScrollStepProperty =
            DependencyProperty.RegisterAttached(
                "VerticalScrollStep",
                typeof(double),
                typeof(ListViewScrollBehavior),
                new PropertyMetadata(0.0, OnVerticalScrollStepChanged));

        public static void SetVerticalScrollStep(DependencyObject element, double value) =>
            element.SetValue(VerticalScrollStepProperty, value);

        public static double GetVerticalScrollStep(DependencyObject element) =>
            (double)element.GetValue(VerticalScrollStepProperty);

        private static void OnVerticalScrollStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListView lv)
                return;

            // detach previous handler (if any) first to avoid duplicates
            lv.PreviewMouseWheel -= OnPreviewMouseWheel;

            double step = (double)e.NewValue;
            if (step > 0)
            {
                // Attach a handler which will use the configured step
                lv.PreviewMouseWheel += OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (sender is not DependencyObject d)
                return;

            double step = GetVerticalScrollStep(d);
            if (step <= 0)
                return;

            // Find ScrollViewer in the visual tree (ListView template)
            var sv = FindDescendant<ScrollViewer>(d);
            if (sv == null)
                return;

            // Convert wheel delta to notches (120 is the default delta for one notch)
            double notches = e.Delta / 120.0;

            // Compute new offset and scroll
            double newOffset = sv.VerticalOffset - notches * step;
            // Clamp
            if (newOffset < 0) newOffset = 0;
            if (newOffset > sv.ScrollableHeight) newOffset = sv.ScrollableHeight;

            sv.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) return t;
                var result = FindDescendant<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}