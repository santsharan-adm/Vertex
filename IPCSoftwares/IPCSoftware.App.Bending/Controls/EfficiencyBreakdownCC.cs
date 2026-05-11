using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Efficiency breakdown panel showing OEE metrics
    /// </summary>
    public class EfficiencyBreakdownCC : Control
    {
        static EfficiencyBreakdownCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EfficiencyBreakdownCC), new FrameworkPropertyMetadata(typeof(EfficiencyBreakdownCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
