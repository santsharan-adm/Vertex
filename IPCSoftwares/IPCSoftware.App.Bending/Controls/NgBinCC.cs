using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// NG Bin for rejected parts - reusable for NG BIN 1 and NG BIN 2
    /// </summary>
    public class NgBinCC : Control
    {
        static NgBinCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NgBinCC), new FrameworkPropertyMetadata(typeof(NgBinCC)));
        }
        #region Dependency Propety
        #region BinNumber
        public static readonly DependencyProperty BinNumberProperty =
            DependencyProperty.Register(nameof(BinNumber), typeof(int), typeof(NgBinCC), new PropertyMetadata(1));

        public int BinNumber
        {
            get => (int)GetValue(BinNumberProperty);
            set => SetValue(BinNumberProperty, value);
        }
        #endregion BinNumber
        
        #endregion Dependency Propety
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
