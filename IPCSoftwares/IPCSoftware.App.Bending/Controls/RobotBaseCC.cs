using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Robot base unit - reusable for ROBOT 1 and ROBOT 2
    /// </summary>
    public class RobotBaseCC : Control
    {
        static RobotBaseCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RobotBaseCC), new FrameworkPropertyMetadata(typeof(RobotBaseCC)));
        }

        public static readonly DependencyProperty RobotNumberProperty =
            DependencyProperty.Register(nameof(RobotNumber), typeof(int), typeof(RobotBaseCC), new PropertyMetadata(1));

        public int RobotNumber
        {
            get => (int)GetValue(RobotNumberProperty);
            set => SetValue(RobotNumberProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
