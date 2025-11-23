using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.Helpers
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BindablePasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindablePassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new PropertyMetadata("", OnBindablePasswordChanged));

        public static string GetBindablePassword(DependencyObject obj)
        {
            return (string)obj.GetValue(BindablePasswordProperty);
        }

        public static void SetBindablePassword(DependencyObject obj, string value)
        {
            obj.SetValue(BindablePasswordProperty, value);
        }

        private static void OnBindablePasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as PasswordBox;
            box.PasswordChanged -= PasswordChanged;

            if (!GetIsUpdating(box))
            {
                box.Password = (string)e.NewValue;
            }

            box.PasswordChanged += PasswordChanged;
        }

        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        public static bool GetBindPassword(DependencyObject obj)
        {
            return (bool)obj.GetValue(BindPasswordProperty);
        }

        public static void SetBindPassword(DependencyObject obj, bool value)
        {
            obj.SetValue(BindPasswordProperty, value);
        }

        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as PasswordBox;

            if ((bool)e.OldValue)
            {
                box.PasswordChanged -= PasswordChanged;
            }

            if ((bool)e.NewValue)
            {
                box.PasswordChanged += PasswordChanged;
            }
        }

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        private static bool GetIsUpdating(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject obj, bool value)
        {
            obj.SetValue(IsUpdatingProperty, value);
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var box = sender as PasswordBox;
            SetIsUpdating(box, true);
            SetBindablePassword(box, box.Password);
            SetIsUpdating(box, false);
        }
    }
}
