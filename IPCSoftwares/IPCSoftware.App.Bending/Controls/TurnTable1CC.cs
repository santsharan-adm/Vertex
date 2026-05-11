using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Turn Table 1 with rotating ellipses
    /// </summary>
    public class TurnTable1CC : Control
    {
        static TurnTable1CC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TurnTable1CC), new FrameworkPropertyMetadata(typeof(TurnTable1CC)));
        }

        private System.Windows.Media.RotateTransform rotateTransform;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            rotateTransform = GetTemplateChild("Part_TT1RotateTransform") as System.Windows.Media.RotateTransform;
        }

        public void RotateTable(double angle)
        {
            if (rotateTransform == null) return;

            double current = rotateTransform.Angle;
            double target = current + angle;

            var animation = new DoubleAnimation
            {
                From = current,
                To = target,
                Duration = TimeSpan.FromMilliseconds(800),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.2,
                FillBehavior = FillBehavior.HoldEnd
            };

            rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
        }
    }
}
