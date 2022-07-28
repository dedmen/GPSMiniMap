using GPSMinimapReceiver;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentDatabase
{
    public static class ServiceManager
    {
        private static ServiceCollection _serviceCollection;
        static ServiceManager()
        {
            _serviceCollection = new ServiceCollection();
            ServiceProvider = _serviceCollection.BuildServiceProvider();
            Program.SetupServices();
        }

        public static void AddInstance<TService>(TService o) where TService : class
        {
            _serviceCollection.AddSingleton<TService>(o);
            ServiceProvider = _serviceCollection.BuildServiceProvider();
        }

        public static TService GetService<TService>() where TService : class
        {
            return (TService)ServiceProvider.GetService(typeof(TService));
        }

        public static ServiceProvider ServiceProvider { get; set; }
    }
}
