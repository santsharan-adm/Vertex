using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Bending data table showing heater temp, load, and measurement results
    /// </summary>
    public class BendingDataTableCC2 : Control
    {
        static BendingDataTableCC2()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BendingDataTableCC2), new FrameworkPropertyMetadata(typeof(BendingDataTableCC2)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
