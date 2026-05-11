using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace IPCSoftware.App.Bending.Controls
{
    public class TurnTable1CC : Control
    {
        static TurnTable1CC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TurnTable1CC), new FrameworkPropertyMetadata(typeof(TurnTable1CC)));
        }

        RotateTransform rotateTransform;

        Ellipse ellipseTopRow1;
        Ellipse ellipseTopRow2;
        Ellipse ellipseTopRow3;
        Ellipse ellipseTopRow4;

        Ellipse ellipseBottomRow1;
        Ellipse ellipseBottomRow2;
        Ellipse ellipseBottomRow3;
        Ellipse ellipseBottomRow4;

        Ellipse ellipseLeftCol1;
        Ellipse ellipseLeftCol2;
        Ellipse ellipseLeftCol3;
        Ellipse ellipseLeftCol4;

        Ellipse ellipseRightCol1;
        Ellipse ellipseRightCol2;
        Ellipse ellipseRightCol3;
        Ellipse ellipseRightCol4;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            rotateTransform = GetTemplateChild("Part_TT1RotateTransform") as RotateTransform;

            ellipseTopRow1 = GetTemplateChild("Part_TT1_TopRow1") as Ellipse;
            ellipseTopRow2 = GetTemplateChild("Part_TT1_TopRow2") as Ellipse;
            ellipseTopRow3 = GetTemplateChild("Part_TT1_TopRow3") as Ellipse;
            ellipseTopRow4 = GetTemplateChild("Part_TT1_TopRow4") as Ellipse;

            ellipseBottomRow1 = GetTemplateChild("Part_TT1_BottomRow1") as Ellipse;
            ellipseBottomRow2 = GetTemplateChild("Part_TT1_BottomRow2") as Ellipse;
            ellipseBottomRow3 = GetTemplateChild("Part_TT1_BottomRow3") as Ellipse;
            ellipseBottomRow4 = GetTemplateChild("Part_TT1_BottomRow4") as Ellipse;

            ellipseLeftCol1 = GetTemplateChild("Part_TT1_LeftCol1") as Ellipse;
            ellipseLeftCol2 = GetTemplateChild("Part_TT1_LeftCol2") as Ellipse;
            ellipseLeftCol3 = GetTemplateChild("Part_TT1_LeftCol3") as Ellipse;
            ellipseLeftCol4 = GetTemplateChild("Part_TT1_LeftCol4") as Ellipse;

            ellipseRightCol1 = GetTemplateChild("Part_TT1_RightCol1") as Ellipse;
            ellipseRightCol2 = GetTemplateChild("Part_TT1_RightCol2") as Ellipse;
            ellipseRightCol3 = GetTemplateChild("Part_TT1_RightCol3") as Ellipse;
            ellipseRightCol4 = GetTemplateChild("Part_TT1_RightCol4") as Ellipse;
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
                Duration = TimeSpan.FromMilliseconds(400),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.2,
                FillBehavior = FillBehavior.HoldEnd
            };

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }
    }
}
