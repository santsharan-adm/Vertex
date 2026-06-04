using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Inspection unit for input side with QR and camera icons
    /// </summary>
    public class InspectionUnitInputCC : Control
    {
        static InspectionUnitInputCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InspectionUnitInputCC), new FrameworkPropertyMetadata(typeof(InspectionUnitInputCC)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        #region Dependency Propety
        #region StationIndex
        public static readonly DependencyProperty StationIndexProperty =
            DependencyProperty.Register(nameof(StationIndex), typeof(int), typeof(InspectionUnitInputCC), new PropertyMetadata(0));

        public int StationIndex
        {
            get => (int)GetValue(StationIndexProperty);
            set => SetValue(StationIndexProperty, value);
        }
        #endregion StationIndex
        #region BatchNo
        public static readonly DependencyProperty BatchNoProperty =
            DependencyProperty.Register(nameof(BatchNo), typeof(int), typeof(InspectionUnitInputCC), new PropertyMetadata(0));

        public int BatchNo
        {
            get => (int)GetValue(BatchNoProperty);
            set => SetValue(BatchNoProperty, value);
        }
        #endregion BatchNo
        #endregion Dependency Propety
    }
}
