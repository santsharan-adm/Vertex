using IPCSoftware.App.Views;
using IPCSoftware.Shared;
using Microsoft.VisualBasic.Devices;
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
    public enum OperationMode
    {
        Auto,
        DryRun,
        Manual,
        CycleStop,
        MassRTO,
        Abort
    }


    public class ModeOfOperationViewModel : INotifyPropertyChanged
    {
        private OperationMode? _selectedButton;

        public OperationMode? SelectedButton
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

        public ObservableCollection<OperationMode> Modes { get; }

        public ModeOfOperationViewModel()
        {
            Modes = new ObservableCollection<OperationMode>
            {
                OperationMode.Auto,
    OperationMode.DryRun,
    OperationMode.Manual,
    OperationMode.CycleStop,
    OperationMode.MassRTO,
    OperationMode.Abort
            };

            // Command initializes once
            ButtonClickCommand = new RelayCommand<object>(OnButtonClicked);
        }

        private void OnButtonClicked(object? param)
        {
            if (param is OperationMode mode)
            {
                // If already selected, deselect
                if (SelectedButton == mode)
                    SelectedButton = null;
                else
                {
                    // Only allow change if current selected is null
                    if (SelectedButton == null)
                        SelectedButton = mode;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
