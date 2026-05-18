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

        public static readonly DependencyProperty ComponentCountProperty =
            DependencyProperty.Register(nameof(ComponentCount), typeof(int), typeof(OutputTrayCC), new PropertyMetadata(0));

        public int ComponentCount
        {
            get => (int)GetValue(ComponentCountProperty);
            set => SetValue(ComponentCountProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
