using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
   
    /// <summary>
    /// PID table display - reusable for both Heater and Force tables
    /// </summary>
    public class PidTableForceCC : Control
    {
        static PidTableForceCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PidTableForceCC), new FrameworkPropertyMetadata(typeof(PidTableForceCC  )));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        #region Dependency Property
        #region TableType
        public static readonly DependencyProperty TableTypeProperty =
            DependencyProperty.Register(nameof(TableType), typeof(PidTableType), typeof(PidTableForceCC), 
                new PropertyMetadata(PidTableType.Heater));

        public PidTableType TableType
        {
            get => (PidTableType)GetValue(TableTypeProperty);
            set => SetValue(TableTypeProperty, value);
        }
        #endregion TableType
        #endregion Dependency Property


    }
}
