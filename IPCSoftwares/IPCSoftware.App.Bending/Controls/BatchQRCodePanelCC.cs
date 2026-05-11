using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Batch number and QR code display panel
    /// </summary>
    public class BatchQRCodePanelCC : Control
    {
        static BatchQRCodePanelCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BatchQRCodePanelCC), new FrameworkPropertyMetadata(typeof(BatchQRCodePanelCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
