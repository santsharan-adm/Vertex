
using System;

namespace IPCSoftware.UI.CommonViews
{
    public static class ServiceProvider
    {
        public static IServiceProvider Services { get; set; }

        public static object GetService(Type type)
        {
            return Services.GetService(type);
        }
    }
}