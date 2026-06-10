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

        public static readonly DependencyProperty ComponentCountProperty =
            DependencyProperty.Register(nameof(ComponentCount), typeof(int), typeof(InputTrayCC), new PropertyMetadata(0));

        public int ComponentCount
        {
            get => (int)GetValue(ComponentCountProperty);
            set => SetValue(ComponentCountProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        #region Dependency Propety
        #region StationIndex
        public static readonly DependencyProperty StationIndexProperty =
            DependencyProperty.Register(nameof(StationIndex), typeof(int), typeof(InputTrayCC), new PropertyMetadata(0));

        public int StationIndex
        {
            get => (int)GetValue(StationIndexProperty);
            set => SetValue(StationIndexProperty, value);
        }
        #endregion StationIndex
        #region BatchNo
        public static readonly DependencyProperty BatchNoProperty =
            DependencyProperty.Register(nameof(BatchNo), typeof(int), typeof(InputTrayCC), new PropertyMetadata(0));

        public int BatchNo
        {
            get => (int)GetValue(BatchNoProperty);
            set => SetValue(BatchNoProperty, value);
        }
        #endregion BatchNo
        #endregion Dependency Propety
    }
}
