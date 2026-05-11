using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    public enum PidTableType
    {
        Heater,
        Force
    }

    /// <summary>
    /// PID table display - reusable for both Heater and Force tables
    /// </summary>
    public class PidTableCC : Control
    {
        static PidTableCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PidTableCC), new FrameworkPropertyMetadata(typeof(PidTableCC)));
        }

        public static readonly DependencyProperty TableTypeProperty =
            DependencyProperty.Register(nameof(TableType), typeof(PidTableType), typeof(PidTableCC), 
                new PropertyMetadata(PidTableType.Heater));

        public PidTableType TableType
        {
            get => (PidTableType)GetValue(TableTypeProperty);
            set => SetValue(TableTypeProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
