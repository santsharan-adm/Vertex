using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IPCSoftware.App.Bending.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:IPCSoftware.App.Bending.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:IPCSoftware.App.Bending.Controls;assembly=IPCSoftware.App.Bending.Controls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:InspectionUnitCC/>
    ///
    /// </summary>
    [TemplatePart(Name = "Part_NavButtonStationIndex", Type = typeof(StationIndexCC))]
    public class InspectionUnitOutputCC : Control
    {
        static InspectionUnitOutputCC()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InspectionUnitOutputCC), new FrameworkPropertyMetadata(typeof(InspectionUnitOutputCC)));
        }
        /// <summary>
        /// Raises the ButtonClick routed event.
        /// </summary>
        //protected virtual void OnButtonClick()
        //{
        //    RaiseEvent(new RoutedEventArgs(ButtonClickEvent, this));
        //}

        //private StationIndexCC? _partButton;
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
        //    OnButtonClick();
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
        //        typeof(InspectionUnitOutputCC));

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
            DependencyProperty.Register(nameof(StationIndex), typeof(int), typeof(InspectionUnitOutputCC), new PropertyMetadata(0));

        public int StationIndex
        {
            get => (int)GetValue(StationIndexProperty);
            set => SetValue(StationIndexProperty, value);
        }
        #endregion StationIndex
        #region BatchNo
        public static readonly DependencyProperty BatchNoProperty =
            DependencyProperty.Register(nameof(BatchNo), typeof(int), typeof(InspectionUnitOutputCC), new PropertyMetadata(0));

        public int BatchNo
        {
            get => (int)GetValue(BatchNoProperty);
            set => SetValue(BatchNoProperty, value);
        }
        #endregion BatchNo
        #endregion Dependency Propety
    }
}
