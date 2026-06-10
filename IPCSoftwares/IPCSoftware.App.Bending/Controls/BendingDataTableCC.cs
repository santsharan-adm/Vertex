using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Bending data table showing heater temp, load, and measurement results
    /// </summary>
     [TemplatePart(Name = "Part_BendingListView", Type = typeof(ListView))]
    public class BendingDataTableCC : Control
    {
        static BendingDataTableCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BendingDataTableCC), new FrameworkPropertyMetadata(typeof(BendingDataTableCC)));
        }
        ListView? _batchListView;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _batchListView = GetTemplateChild("Part_BendingListView") as ListView;
        }

        public void ScrollToBatchNo(int batchNo)
        {
            // This method can be called from the button click handler in Dashboard2.xaml.cs
            // It will attempt to find the BatchListView and scroll to the item with the given batchNo
            if (_batchListView != null)
            {
                var item = _batchListView.Items.Cast<object?>()
                            .FirstOrDefault(it => ItemMatchesBatchNo(it, batchNo));
                if (item != null)
                {
                    _batchListView.ScrollIntoView(item);
                }
            }
        }
        // Generic helper: uses reflection to look for a `BatchNo` property (adjust name/type as needed)
        private static bool ItemMatchesBatchNo(object? item, object? batchNo)
        {
            if (item == null) return false;

            var t = item.GetType();
            var prop = t.GetProperty("BatchNo", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                var val = prop.GetValue(item);
                return Equals(val?.ToString(), batchNo?.ToString());
            }

            // If the Items are simple strings, compare string values
            return Equals(item.ToString(), batchNo?.ToString());
        }

        private void StationIndexCC_ButtonClick2(object? sender, RoutedEventArgs e)
        {
            if (sender is not StationIndexCC control) return;

            var batchNo = control.BatchNo; // adjust type if not string

            if (_batchListView?.ItemsSource == null)
            {
                // fallback to Items collection
                var fallback = _batchListView?.Items.Cast<object?>()
                                 .FirstOrDefault(it => ItemMatchesBatchNo(it, batchNo));
                if (fallback != null) _batchListView.ScrollIntoView(fallback);
                return;
            }

            // Try to find an item in ItemsSource with a BatchNo property that equals the control.BatchNo
            var item = _batchListView.Items.Cast<object?>()
                        .FirstOrDefault(it => ItemMatchesBatchNo(it, batchNo));

            if (item != null)
            {
                _batchListView.ScrollIntoView(item);
            }
        }
    }
}
