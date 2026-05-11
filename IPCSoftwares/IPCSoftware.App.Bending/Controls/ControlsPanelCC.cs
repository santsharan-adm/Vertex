using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Control panel with operation buttons
    /// </summary>
    public class ControlsPanelCC : Control
    {
        static ControlsPanelCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ControlsPanelCC), new FrameworkPropertyMetadata(typeof(ControlsPanelCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
