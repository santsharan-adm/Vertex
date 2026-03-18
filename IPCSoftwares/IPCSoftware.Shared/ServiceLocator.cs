using Microsoft.Extensions.DependencyInjection;

namespace IPCSoftware.Shared
{
    /// <summary>
    /// Central service locator that holds the application's IServiceProvider.
    /// Initialized once at app startup; used by libraries that cannot directly
    /// reference the App executable project.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider? _provider;

        public static IServiceProvider Current =>
            _provider ?? throw new InvalidOperationException(
                "ServiceLocator has not been initialized. Call ServiceLocator.Initialize() during app startup.");

        public static void Initialize(IServiceProvider provider)
        {
            _provider = provider;
        }

        public static T? GetService<T>() => Current.GetService<T>();
        public static T GetRequiredService<T>() where T : notnull => Current.GetRequiredService<T>();
    }
}
