using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Inspection unit for input side with QR and camera icons
    /// </summary>
    [TemplatePart(Name = "Part_NavButtonStationIndex", Type = typeof(StationIndexCC))]
    public class InspectionUnitInputCC : Control
    {
        static InspectionUnitInputCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InspectionUnitInputCC), new FrameworkPropertyMetadata(typeof(InspectionUnitInputCC)));
        }
        /// <summary>
        /// Raises the ButtonClick routed event.
        /// </summary>
        //protected virtual void OnButtonClick(object? sender, RoutedEventArgs e)
        //{
        //    RaiseEvent(new RoutedEventArgs(ButtonClickEvent, sender));
        //}

        private StationIndexCC? _partButton;
        public override void OnApplyTemplate()
        {
            // Detach previous handler if present
            //if (_partButton != null)
            //{
            //    _partButton.ButtonClick -= PartButton_Click;
            //    _partButton = null;
            //}
            base.OnApplyTemplate();
            // Find the button in the control template and attach handler
            //_partButton = GetTemplateChild("Part_NavButtonStationIndex") as StationIndexCC;
            //if (_partButton != null)
            //{
            //    _partButton.ButtonClick += PartButton_Click;
            //}
        }
        //private void PartButton_Click(object? sender, RoutedEventArgs e)
        //{
        //    // Bubble up as the control's routed event
        //    //OnButtonClick(sender,e);
        //}
        //#region RoutedEvent

        ///// <summary>
        ///// Routed event raised when the template button is clicked.
        ///// Consumers can subscribe to the ButtonClick CLR event or add a handler for the routed event.
        ///// </summary>
        //public static readonly RoutedEvent ButtonClickEvent =
        //    EventManager.RegisterRoutedEvent(
        //        nameof(ButtonClick),
        //        RoutingStrategy.Bubble,
        //        typeof(RoutedEventHandler),
        //        typeof(InspectionUnitInputCC));

        ///// <summary>
        ///// CLR wrapper for the ButtonClick routed event.
        ///// </summary>
        //public event RoutedEventHandler ButtonClick
        //{
        //    add => AddHandler(ButtonClickEvent, value);
        //    remove => RemoveHandler(ButtonClickEvent, value);
        //}



        //#endregion RoutedEvent
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
