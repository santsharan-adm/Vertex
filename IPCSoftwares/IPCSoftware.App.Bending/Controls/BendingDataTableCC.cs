using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Bending data table showing heater temp, load, and measurement results
    /// </summary>
    public class BendingDataTableCC : Control
    {
        static BendingDataTableCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BendingDataTableCC), new FrameworkPropertyMetadata(typeof(BendingDataTableCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
