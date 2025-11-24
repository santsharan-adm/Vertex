using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace IPCSoftware.App.Helpers
{
    public class ImageClickBehavior : Behavior<System.Windows.Controls.Image>
    {
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ImageClickBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDown += ImageClicked;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseDown -= ImageClicked;
        }

        private void ImageClicked(object sender, MouseButtonEventArgs e)
        {
            if (Command?.CanExecute(AssociatedObject.DataContext) == true)
                Command.Execute(AssociatedObject.DataContext);
        }
    }
}
