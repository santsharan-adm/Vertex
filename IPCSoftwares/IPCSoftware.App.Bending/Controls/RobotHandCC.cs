using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Robot hand (end effector) with claw - reusable for both robots
    /// </summary>
    public class RobotHandCC : Control
    {
        static RobotHandCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RobotHandCC), new FrameworkPropertyMetadata(typeof(RobotHandCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public void AnimatePosition(double targetLeft, double targetTop, TimeSpan duration)
        {
            var animLeft = new DoubleAnimation
            {
                To = targetLeft,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };

            var animTop = new DoubleAnimation
            {
                To = targetTop,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };

            this.BeginAnimation(Canvas.LeftProperty, animLeft);
            this.BeginAnimation(Canvas.TopProperty, animTop);
        }
    }
}
