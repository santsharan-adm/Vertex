using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Transfer module with rotating icon
    /// </summary>
    public class TransferModuleCC : Control
    {
        static TransferModuleCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TransferModuleCC), new FrameworkPropertyMetadata(typeof(TransferModuleCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
