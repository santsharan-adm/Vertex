using IPCSoftware.App.Views;
using IPCSoftware.AppLogger.Interfaces;
using IPCSoftware.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{


    public class GroupAttribute : Attribute
    {
        public string Name { get; }
        public GroupAttribute(string name) => Name = name;
    }

    public class ModeItem
    {
        public ManualOperationMode Mode { get; set; }
        public string Group { get; set; }
    }

    public enum ManualOperationMode
    {
        // Tray lift
        [Group("Tray Lift")]
        TrayLiftDown,
        [Group("Tray Lift")]
        TrayLiftUp,

        // Move to Position
        [Group("Move to Position")]
        MoveToPos0,
        [Group("Move to Position")]
        MoveToPos1,
        [Group("Move to Position")]
        MoveToPos2,
        [Group("Move to Position")]
        MoveToPos3,
        [Group("Move to Position")]
        MoveToPos4,
        [Group("Move to Position")]
        MoveToPos5,
        [Group("Move to Position")]
        MoveToPos6,
        [Group("Move to Position")]
        MoveToPos7,
        [Group("Move to Position")]
        MoveToPos8,
        [Group("Move to Position")]
        MoveToPos9,
        [Group("Move to Position")]
        MoveToPos10,
        [Group("Move to Position")]
        MoveToPos11,
        [Group("Move to Position")]
        MoveToPos12,

        // Positioning Cylinder
        [Group("Positioning Cylinder")]
        PositioningCylinderUp,
        [Group("Positioning Cylinder")]
        PositioningCylinderDown,

        // Transport Conveyor
        [Group("Transport Conveyor")]
        TransportConveyorForward,
        [Group("Transport Conveyor")]
        TransportConveyorReverse,
        [Group("Transport Conveyor")]
        TransportConveyorStop,
        [Group("Transport Conveyor")]
        TransportConveyorSpeedSwitching,

        // Manual X-Axis Jog
        [Group("Manual X-Axis Jog")]
        ManualXAxisJogForward,
        [Group("Manual X-Axis Jog")]
        ManualXAxisJogBackward,
        [Group("Manual X-Axis Jog")]
        XAxisJogSpeedSwitching,

        // Manual Y-Axis Jog
        [Group("Manual Y-Axis Jog")]
        ManualYAxisJogForward,
        [Group("Manual Y-Axis Jog")]
        ManualYAxisJogBackward,
        [Group("Manual Y-Axis Jog")]
        YAxisJogSpeedSwitching
    }


    public class ManualOperationViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, ManualOperationMode?> _selectedGroupButtons
    = new Dictionary<string, ManualOperationMode?>();

        public Dictionary<string, ManualOperationMode?> SelectedGroupButtons
    => _selectedGroupButtons;


        private ManualOperationMode? _selectedButton;

        public ManualOperationMode? SelectedButton
        {
            get => _selectedButton;
            set
            {
                if (_selectedButton != value)
                {
                    _selectedButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ButtonClickCommand { get; }

        public ObservableCollection<ModeItem> Modes { get; }

        public ManualOperationViewModel(IAppLogger logger)
        {

            Modes = new ObservableCollection<ModeItem>(
        Enum.GetValues(typeof(ManualOperationMode))
            .Cast<ManualOperationMode>()
            .Select(mode =>
            {
                var group = mode.GetType()
                    .GetMember(mode.ToString())[0]
                    .GetCustomAttributes(typeof(GroupAttribute), false)
                    .Cast<GroupAttribute>()
                    .FirstOrDefault()?.Name ?? "Other";

                return new ModeItem
                {
                    Mode = mode,
                    Group = group
                };
            }));

           
            // Command initializes once
            ButtonClickCommand = new RelayCommand<object>(OnButtonClicked);
        }

        //private void OnButtonClicked(object? param)
        //{
        //    if (param is ManualOperationMode mode)
        //    {
        //        // If already selected, deselect
        //        if (SelectedButton == mode)
        //            SelectedButton = null;
        //        else
        //        {
        //            // Only allow change if current selected is null
        //            if (SelectedButton == null)
        //                SelectedButton = mode;
        //        }
        //    }
        //}

        private void OnButtonClicked(object? param)
        {
            if (param is not ManualOperationMode clickedMode)
                return;

            string group = GetGroup(clickedMode);

            // Initialize group tracking if required
            if (!_selectedGroupButtons.ContainsKey(group))
                _selectedGroupButtons[group] = null;

            // 1️⃣ If clicking the SAME button → deselect
            if (_selectedGroupButtons[group] == clickedMode)
            {
                _selectedGroupButtons[group] = null;
                OnPropertyChanged(nameof(SelectedGroupButtons));
                return;
            }

            // 2️⃣ If another button in the SAME group is already selected → do NOT allow switching
            if (_selectedGroupButtons[group] != null)
            {
                // Button blocked until user deselects the currently active one
                return;
            }

            // 3️⃣ No selection in this group → allow selecting
            _selectedGroupButtons[group] = clickedMode;
            OnPropertyChanged(nameof(SelectedGroupButtons));
        }



        //private void OnButtonClicked(object? param)
        //{
        //    if (param is not ManualOperationMode clickedMode)
        //        return;

        //    // 1. If clicking the same button → deselect
        //    if (SelectedButton == clickedMode)
        //    {
        //        SelectedButton = null;
        //        return;
        //    }

        //    // If no button is selected yet → select it
        //    if (SelectedButton == null)
        //    {
        //        SelectedButton = clickedMode;
        //        return;
        //    }

        //    // Compare groups
        //    string currentGroup = GetGroup(SelectedButton.Value);
        //    string clickedGroup = GetGroup(clickedMode);

        //    // 2. Same group → DO NOTHING
        //    if (currentGroup == clickedGroup)
        //        return;

        //    // 3. Different group → switch green highlight to new button
        //    SelectedButton = clickedMode;
        //}

        private string GetGroup(ManualOperationMode mode)
        {
            var attr = mode.GetType()
                .GetMember(mode.ToString())[0]
                .GetCustomAttributes(typeof(GroupAttribute), false)
                .Cast<GroupAttribute>()
                .FirstOrDefault();

            return attr?.Name ?? "";
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
