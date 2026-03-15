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

namespace IPCSoftware.UI.CommonViews
{
    public partial class LoginView : UserControl
    {
        public LoginView(LoginViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
           

        }

       
    }
}

