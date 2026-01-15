using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Microsoft.Xaml.Behaviors; 

namespace IPCSoftware.App.Behaviors
{
    public class LifecycleBehavior : Behavior<UserControl>
    {
        // This property will hold the command that should run
        // when the UserControl is loaded (appears on screen)

        public static readonly DependencyProperty LoadedCommandProperty =
            DependencyProperty.Register("LoadedCommand", typeof(ICommand), typeof(LifecycleBehavior), new PropertyMetadata(null));

        public ICommand LoadedCommand
        {
            get { return (ICommand)GetValue(LoadedCommandProperty); }
            set { SetValue(LoadedCommandProperty, value); }
        }
        // This property will hold the command that should run
        // when the UserControl is unloaded (removed from screen)

        public static readonly DependencyProperty UnloadedCommandProperty =
            DependencyProperty.Register("UnloadedCommand", typeof(ICommand), typeof(LifecycleBehavior), new PropertyMetadata(null));

        public ICommand UnloadedCommand
        {
            get { return (ICommand)GetValue(UnloadedCommandProperty); }
            set { SetValue(UnloadedCommandProperty, value); }
        }
        // This method is called when the behavior is attached to a UserControl
        // Here we are subscribing to the Loaded and Unloaded events

        protected override void OnAttached()
        {
            base.OnAttached();

            // When the control loads or unloads, call these methods
            AssociatedObject.Loaded += OnControlLoaded;
            AssociatedObject.Unloaded += OnControlUnloaded;
        }
        // This method is called when the behavior is detached from the UserControl
        // Here we are unsubscribing from events to prevent memory leaks
        protected override void OnDetaching()
        {
            base.OnDetaching();
          
            AssociatedObject.Loaded -= OnControlLoaded;
            AssociatedObject.Unloaded -= OnControlUnloaded;
        }
        // This method runs when the UserControl is loaded
        // It checks if a LoadedCommand is assigned and executable, then runs it

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
           
            if (LoadedCommand != null && LoadedCommand.CanExecute(null))
            {
                LoadedCommand.Execute(null);
            }
        }
        // This method runs when the UserControl is unloaded
        // It checks if an UnloadedCommand is assigned and executable, then runs it
        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
          
            if (UnloadedCommand != null && UnloadedCommand.CanExecute(null))
            {
                UnloadedCommand.Execute(null);
            }
        }
    }
}