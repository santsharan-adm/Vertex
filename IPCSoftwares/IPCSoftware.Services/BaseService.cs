using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services
{
    public abstract class BaseService
    {
        protected readonly IAppLogger _logger;

        protected BaseService(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }

}
