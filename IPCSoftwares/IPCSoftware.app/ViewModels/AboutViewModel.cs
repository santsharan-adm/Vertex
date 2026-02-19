using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private readonly AboutSettings _settings;

        public string ProductName => _settings.ProductName;

        public string ProductVersion { get; }

        public string LicenseTo => _settings.LicenseTo;

        public string LicenseType => _settings.LicenseType;

        public string Copyright => _settings.Copyright;

        // Inject IOptions<AboutSettings> to access data from appsettings.json
        public AboutViewModel(IAppLogger logger, IOptions<AboutSettings> options) : base(logger)
        {
            _settings = options.Value ?? new AboutSettings();

            // Version Logic:
            // If "ProductVersion" is defined in JSON, use it.
            // Otherwise, fallback to the internal Assembly Version (v1.0.0).
            if (!string.IsNullOrWhiteSpace(_settings.ProductVersion))
            {
                ProductVersion = _settings.ProductVersion;
            }
           
        }
    }
}
