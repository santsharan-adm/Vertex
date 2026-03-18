using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.Common.WPFExtensions.Behaviours
{
    public class MomentaryBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(MomentaryBehavior));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(MomentaryBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnDown;
            AssociatedObject.PreviewMouseLeftButtonUp += OnUp;
            AssociatedObject.MouseLeave += OnUp; // Safety
            AssociatedObject.PreviewTouchDown += OnDown;
            AssociatedObject.PreviewTouchUp += OnUp;
            AssociatedObject.LostMouseCapture += OnUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= OnDown;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnUp;
            AssociatedObject.MouseLeave -= OnUp;
            AssociatedObject.PreviewTouchDown -= OnDown;
            AssociatedObject.PreviewTouchUp -= OnUp;
            AssociatedObject.LostMouseCapture -= OnUp;
        }

        private void OnDown(object sender, RoutedEventArgs e) => Execute(true);
        private void OnUp(object sender, RoutedEventArgs e) => Execute(false);

        private void Execute(bool isPressed)
        {
            // We pass the parameter AND the state (Pressed/Released)
            if (Command != null && Command.CanExecute(null))
            {
                // Format: "Parameter|True" or "Parameter|False"
                string param = $"{CommandParameter}|{isPressed}";
                Command.Execute(param);
            }
        }
    }

}
