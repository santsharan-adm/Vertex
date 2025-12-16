using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
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
        [Group("Tray Lift")] TrayLiftUp,
        [Group("Tray Lift")] TrayLiftDown,

        [Group("Move to Position")] MoveToPos0,
        [Group("Move to Position")] MoveToPos1,
        [Group("Move to Position")] MoveToPos2,
        [Group("Move to Position")] MoveToPos3,
        [Group("Move to Position")] MoveToPos4,
        [Group("Move to Position")] MoveToPos5,
        [Group("Move to Position")] MoveToPos6,
        [Group("Move to Position")] MoveToPos7,
        [Group("Move to Position")] MoveToPos8,
        [Group("Move to Position")] MoveToPos9,
        [Group("Move to Position")] MoveToPos10,
        [Group("Move to Position")] MoveToPos11,
        [Group("Move to Position")] MoveToPos12,

        [Group("Positioning Cylinder")] PositioningCylinderUp,
        [Group("Positioning Cylinder")] PositioningCylinderDown,

        [Group("Transport Conveyor")] TransportConveyorReverse,
        [Group("Transport Conveyor")] TransportConveyorForward,
        [Group("Transport Conveyor")] TransportConveyorStop,
        [Group("Transport Conveyor")] TransportConveyorSpeedSwitching,

        [Group("Manual X-Axis Jog")] ManualXAxisJogBackward,
        [Group("Manual X-Axis Jog")] ManualXAxisJogForward,
        [Group("Manual X-Axis Jog")] XAxisJogSpeedSwitching,

        [Group("Manual Y-Axis Jog")] ManualYAxisJogBackward,
        [Group("Manual Y-Axis Jog")] ManualYAxisJogForward,
        [Group("Manual Y-Axis Jog")] YAxisJogSpeedSwitching
    }

    public class ManualOperationViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, ManualOperationMode?> _selectedGroupButtons
            = new Dictionary<string, ManualOperationMode?>();

        public Dictionary<string, ManualOperationMode?> SelectedGroupButtons => _selectedGroupButtons;

        public IEnumerable<ModeItem> TrayModes => Modes.Where(x => x.Group == "Tray Lift");
        public IEnumerable<ModeItem> CylinderModes => Modes.Where(x => x.Group == "Positioning Cylinder");
        public IEnumerable<ModeItem> ConveyorModes => Modes.Where(x => x.Group == "Transport Conveyor");
        public IEnumerable<ModeItem> XAxisModes => Modes.Where(x => x.Group == "Manual X-Axis Jog");
        public IEnumerable<ModeItem> YAxisModes => Modes.Where(x => x.Group == "Manual Y-Axis Jog");

        public ModeItem HomePositionMode =>
            Modes.FirstOrDefault(x => x.Mode == ManualOperationMode.MoveToPos0);

        public IEnumerable<ModeItem> GridPositionModes =>
            Modes.Where(x => x.Group == "Move to Position" && x.Mode != ManualOperationMode.MoveToPos0);

        private readonly IAppLogger _logger;

        public ICommand ButtonClickCommand { get; }

        public ObservableCollection<ModeItem> Modes { get; }

        public ManualOperationViewModel(IAppLogger logger)
        {
            try
            {
                _logger = logger;

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

                ButtonClickCommand = new RelayCommand<object>(OnButtonClicked);
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private void OnButtonClicked(object? param)
        {
            try
            {
                _logger.LogInfo($"{param} button pressed", LogType.Production);

                if (param is not ManualOperationMode clickedMode)
                    return;

                string group = GetGroup(clickedMode);

                if (!_selectedGroupButtons.ContainsKey(group))
                    _selectedGroupButtons[group] = null;

                if (_selectedGroupButtons[group] == clickedMode)
                {
                    _selectedGroupButtons[group] = null;
                    OnPropertyChanged(nameof(SelectedGroupButtons));
                    return;
                }

                if (_selectedGroupButtons[group] != null)
                {
                    return;
                }

                _selectedGroupButtons[group] = clickedMode;
                OnPropertyChanged(nameof(SelectedGroupButtons));
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private string GetGroup(ManualOperationMode mode)
        {
            try
            {
                var attr = mode.GetType()
                    .GetMember(mode.ToString())[0]
                    .GetCustomAttributes(typeof(GroupAttribute), false)
                    .Cast<GroupAttribute>()
                    .FirstOrDefault();

                return attr?.Name ?? "";
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }
    }
}
