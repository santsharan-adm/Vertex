using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Slot display with rectangles
    /// </summary>
    public class SlotDisplayCC : Control
    {
        static SlotDisplayCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SlotDisplayCC), new FrameworkPropertyMetadata(typeof(SlotDisplayCC)));
        }

        public static readonly DependencyProperty IsVerticalProperty =
            DependencyProperty.Register(nameof(IsVertical), typeof(bool), typeof(SlotDisplayCC), new PropertyMetadata(false));

        public bool IsVertical
        {
            get => (bool)GetValue(IsVerticalProperty);
            set => SetValue(IsVerticalProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
