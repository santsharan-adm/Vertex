using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Inspection unit for input side with QR and camera icons
    /// </summary>
    public class InspectionUnitInputCC : Control
    {
        static InspectionUnitInputCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InspectionUnitInputCC), new FrameworkPropertyMetadata(typeof(InspectionUnitInputCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
