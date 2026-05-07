using System.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IPCSoftware.App.Bending.Models;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.UI.CommonViews.ViewModels;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class Dashboard1ViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        public Dashboard1ViewModel(IAppLogger logger) : base(logger)
        {
        }

        #region Commands
        public ICommand ToggleSidebarCommand { get; }
        public ICommand AutoRunCommand { get; }
        public ICommand DryRunCommand { get; }
        public ICommand CycleStartStopCommand { get; }
        public ICommand WorkPayoutStartCommand { get; }
        #endregion

      



      
    }
}