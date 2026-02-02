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

    }
}
