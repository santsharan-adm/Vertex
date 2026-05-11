using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Tearing and Flipping status indicators
    /// </summary>
    public class TearingFlippingStatusCC : Control
    {
        static TearingFlippingStatusCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TearingFlippingStatusCC), new FrameworkPropertyMetadata(typeof(TearingFlippingStatusCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
