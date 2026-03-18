using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IPCSoftware.Common.WPFExtensions.Behaviours
{
    public class ImageClickBehavior : Behavior<Image>
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
