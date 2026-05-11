using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Robot arm with animation support - reusable for both robots
    /// </summary>
    public class RobotArmCC : Control
    {
        static RobotArmCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RobotArmCC), new FrameworkPropertyMetadata(typeof(RobotArmCC)));
        }

        private Line armLower;

        public static readonly DependencyProperty IsRightSideProperty =
            DependencyProperty.Register(nameof(IsRightSide), typeof(bool), typeof(RobotArmCC), new PropertyMetadata(false));

        public bool IsRightSide
        {
            get => (bool)GetValue(IsRightSideProperty);
            set => SetValue(IsRightSideProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            armLower = GetTemplateChild("Part_ArmLower") as Line;
        }

        public void AnimateArm(double targetX2, double targetY2, double targetThickness, TimeSpan duration)
        {
            if (armLower == null) return;

            var animX = new DoubleAnimation
            {
                To = targetX2,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };

            var animY = new DoubleAnimation
            {
                To = targetY2,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };

            var animThickness = new DoubleAnimation
            {
                To = targetThickness,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };

            armLower.BeginAnimation(Line.X2Property, animX);
            armLower.BeginAnimation(Line.Y2Property, animY);
            armLower.BeginAnimation(Line.StrokeThicknessProperty, animThickness);
        }
    }
}
