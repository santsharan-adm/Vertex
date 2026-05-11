using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Status indicators for bend operations (CLAMP, PUNCH, HEAT)
    /// </summary>
    public class BendStatusIndicatorsCC : Control
    {
        static BendStatusIndicatorsCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BendStatusIndicatorsCC), new FrameworkPropertyMetadata(typeof(BendStatusIndicatorsCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
