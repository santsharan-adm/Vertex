using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Input tray for loading parts
    /// </summary>
    public class InputTrayCC : Control
    {
        static InputTrayCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InputTrayCC), new FrameworkPropertyMetadata(typeof(InputTrayCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
