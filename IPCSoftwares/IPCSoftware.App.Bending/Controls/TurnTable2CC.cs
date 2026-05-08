using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:IPCSoftware.App.Bending.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:IPCSoftware.App.Bending.Controls;assembly=IPCSoftware.App.Bending.Controls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:TurnTable2CC/>
    ///
    /// </summary>
    public class TurnTable2CC : Control
    {
        static TurnTable2CC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TurnTable2CC), new FrameworkPropertyMetadata(typeof(TurnTable2CC)));
        }
        RotateTransform rotateTransform;
        Ellipse ellipseP1F1;
        Ellipse ellipseP1F2;
        Ellipse ellipseP1F3;
        Ellipse ellipseP1F4;

        Ellipse ellipseP2F1;
        Ellipse ellipseP2F2;
        Ellipse ellipseP2F3;
        Ellipse ellipseP2F4;

        Ellipse ellipseP3F1;
        Ellipse ellipseP3F2;
        Ellipse ellipseP3F3;
        Ellipse ellipseP3F4;

       
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            rotateTransform = GetTemplateChild("Part_TriangleRotate") as RotateTransform;
            ellipseP1F1 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP1F2 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP1F3 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP1F4 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;

            ellipseP2F1 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP2F2 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP2F3 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP2F4 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;

            ellipseP3F1 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP3F2 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP3F3 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;
            ellipseP3F4 = GetTemplateChild("Part_ElipsePos1Flex1") as Ellipse;

            SetEllipseOffset(ellipseP1F1, 4, 0);
            SetEllipseOffset(ellipseP1F2, 4, 0);
            SetEllipseOffset(ellipseP1F3, 4, 0);
            SetEllipseOffset(ellipseP1F4, 4, 0);

            SetEllipseOffset(ellipseP2F1, -4, 0);
            SetEllipseOffset(ellipseP2F2, -4, 0);
            SetEllipseOffset(ellipseP2F3, -4, 0);
            SetEllipseOffset(ellipseP2F4, -4, 0);

            SetEllipseOffset(ellipseP3F1,0, -4.2);
            SetEllipseOffset(ellipseP3F2, 0, -4.2);
            SetEllipseOffset(ellipseP3F3, 0, -4.2);
            SetEllipseOffset(ellipseP3F4, 0, -4.2);

           


        }

        void SetEllipseOffset(Ellipse ellpse,double x, double y)
        {
            double x1 = Canvas.GetLeft(ellpse);
            double y1 = Canvas.GetTop(ellpse);
            Canvas.SetLeft(ellpse, x1 + x);
            Canvas.SetTop(ellpse, y1 + y);
        }
        public void RotateTable(double angle)
        {
            // Read current angle
            double current = rotateTransform.Angle;

            // Compute target (rotate by +120 degrees)
            double target = current + 120.0;

            // Create animation
            var animation = new DoubleAnimation
            {
                From = current,
                To = target,
                Duration = TimeSpan.FromMilliseconds(400),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.2,
                FillBehavior = FillBehavior.HoldEnd
            };

            // Start animation on the RotateTransform's Angle property
            rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
        }
    }
}
