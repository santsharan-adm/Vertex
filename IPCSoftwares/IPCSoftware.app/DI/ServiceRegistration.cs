using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.AppLogger.Interfaces;
using IPCSoftware.AppLogger.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IPCSoftware.App.DI
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services)
        {
            //Auth service
            services.AddSingleton<IAuthService, AuthService>();
            //Credentials
            services.AddSingleton<ICredentialsService, CredentialsService>();
            //Navigation
            services.AddSingleton<INavigationService, NavigationService>();
            //Dialog service
            services.AddSingleton<IDialogService, DialogService>();
            //AppLogger 
            services.AddSingleton<IAppLogger, IPCSoftware.AppLogger.Services.AppLogger>();

            services.AddSingleton<LogConfigService>();
            services.AddSingleton<LogManager>();
            services.AddSingleton<IAppLogger, IPCSoftware.AppLogger.Services.AppLogger>();

            //ViewModels
            services.AddSingleton<RibbonViewModel>();
            services.AddSingleton<MainWindowViewModel>();

            // Views
            services.AddTransient<LoginView>();
            services.AddTransient<RibbonView>();
            services.AddTransient<DashboardView>();
            //services.AddTransient<SettingsView>();
            //services.AddTransient<LogsView>();
            //services.AddTransient<UserMgmtView>();




        }
    }
}
