using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Output tray for unloading parts
    /// </summary>
    public class OutputTrayCC : Control
    {
        static OutputTrayCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OutputTrayCC), new FrameworkPropertyMetadata(typeof(OutputTrayCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
