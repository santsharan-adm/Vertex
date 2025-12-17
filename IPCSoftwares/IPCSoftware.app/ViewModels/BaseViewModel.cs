using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.ViewModels
{
    public class BaseViewModel : ObservableObjectVM
    {
        protected readonly IAppLogger _logger;

        public BaseViewModel (IAppLogger logger)
        {
               _logger = logger;
        }



        /// <summary>
        /// Provides access to the AppLogger without requiring Constructor Injection.
        /// This allows derived ViewModels to use 'Logger.LogInfo()' immediately.
        /// </summary>
      //  public IAppLogger Logger
        //{
        //    get
        //    {
        //        // Lazy loading: Only resolve the service when first accessed.
        //        // This prevents issues if the ViewModel is created before the ServiceProvider is fully built.
        //        if (_logger == null && App.ServiceProvider != null)
        //        {
        //            _logger = App.ServiceProvider.GetService<IAppLogger>();
        //        }
        //        return _logger;
        //    }
        //}

        //// Optional: Helper methods to make logging even easier in derived classes
        //protected void LogInfo(string message) => Logger?.LogInfo(message, Shared.Models.ConfigModels.LogType.Production);
        //protected void LogWarning(string message) => Logger?.LogWarning(message, LogType.Audit);
        //protected void LogError(string message) => Logger?.LogError(message, LogType.Error);



    }
}
