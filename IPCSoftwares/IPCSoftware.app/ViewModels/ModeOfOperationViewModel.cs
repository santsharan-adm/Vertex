using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
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
        private readonly IAppLogger _logger;


        public ObservableCollection<AuditLogModel> AuditLogs { get; set; }

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

        public ModeOfOperationViewModel(IAppLogger logger)
        {
            _logger = logger;   
            Modes = new ObservableCollection<OperationMode>
            {
                OperationMode.Auto,
    OperationMode.DryRun,
    OperationMode.Manual,
    OperationMode.CycleStop,
    OperationMode.MassRTO,
    OperationMode.Abort
            };

            AuditLogs = new ObservableCollection<AuditLogModel>();

            // Command initializes once
            ButtonClickCommand = new RelayCommand<object>(OnButtonClicked);
        }

        private void OnButtonClicked(object? param)
        {
            if (param is OperationMode mode)
            {
                // If already selected, deselect
                if (SelectedButton == mode)
                {
                    SelectedButton = null;
                    AddAudit($"{mode} mode stopped!");
                }
                else
                {
                    // Only allow change if current selected is null
                    if (SelectedButton == null)
                    {

                        SelectedButton = mode;
                    AddAudit($"{mode} mode started!");
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddAudit(string message)
        {
            _logger.LogInfo(message, LogType.Audit);

            AuditLogs.Add(new AuditLogModel
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = message
            });
        }
    }
}
