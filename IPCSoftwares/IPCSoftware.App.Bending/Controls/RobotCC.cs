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
    public class Robot1CC : Control
    {
        static Robot1CC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Robot1CC), new FrameworkPropertyMetadata(typeof(Robot1CC)));
        }

        private Line armLower;

        public static readonly DependencyProperty IsRightSideProperty =
            DependencyProperty.Register(nameof(IsRightSide), typeof(bool), typeof(Robot1CC), new PropertyMetadata(false));

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

        #region Dependency Property
        #region RobotNumber
        public static readonly DependencyProperty RobotNumberProperty =
            DependencyProperty.Register(nameof(RobotNumber), typeof(int), typeof(Robot1CC), new PropertyMetadata(1));

        public int RobotNumber
        {
            get => (int)GetValue(RobotNumberProperty);
            set => SetValue(RobotNumberProperty, value);
        }
        #endregion RobotNumber
        #endregion Dependency Property
    }
}
