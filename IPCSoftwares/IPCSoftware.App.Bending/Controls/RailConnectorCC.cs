using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Rail connector between components
    /// </summary>
    public class RailConnectorCC : Control
    {
        static RailConnectorCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RailConnectorCC), new FrameworkPropertyMetadata(typeof(RailConnectorCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
