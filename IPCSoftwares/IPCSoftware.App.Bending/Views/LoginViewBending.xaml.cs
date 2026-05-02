using IPCSoftware.Shared; // TODO: Review - was using IPCSoftware.App (app-level types)
using IPCSoftware.UI.CommonViews.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    public partial class LoginViewBending : UserControl
    {
        public LoginViewBending(LoginViewModelBase vm)
        {
            InitializeComponent();
            DataContext = vm;
           

        }

       
    }
}

