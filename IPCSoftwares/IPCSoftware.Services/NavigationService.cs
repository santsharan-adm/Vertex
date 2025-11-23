using IPCSoftware.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;

namespace IPCSoftware.Services
{
   /* public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _provider;

        private ContentControl _mainHost;
        private ContentControl _topHost;

        public NavigationService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public void Configure(ContentControl mainHost, ContentControl topHost)
        {
            _mainHost = mainHost;
            _topHost = topHost;
        }

        public void NavigateMain<TView>() where TView : class, new()
        {
            if (_mainHost == null) return;

            // View is resolved from DI (BEST PRACTICE)
            var view = _provider.GetService<TView>() ?? new TView();

            _mainHost.Content = view;
        }

        public void NavigateTop(object view)
        {
            if (_topHost == null) return;

            // If view is a TYPE (e.g. typeof(RibbonView))
            if (view is Type type)
            {
                var resolved = _provider.GetService(type);
                _topHost.Content = resolved ?? Activator.CreateInstance(type);
            }
            else
            {
                // If view is already an instance
                _topHost.Content = view;
            }
        }

        public void ClearMain() => _mainHost.Content = null;
        public void ClearTop() => _topHost.Content = null;
    }


*/
}
