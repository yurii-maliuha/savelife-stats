using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveLife.Stats.Worker.Providers;
using SaveLife.Stats.Worker.Tests.Stubs;

namespace SaveLife.Stats.Worker.Tests
{
    public static class TestWorkerFactory
    {
        private static Dictionary<string, string> _defaultConfiguration = new Dictionary<string, string>()
        {
            { "DataSource:BatchSize", "5" },
            { "Loader:ThrottleSeconds", "1" },
            { "Loader:MaxIterationsCount", "10" },
            { "Loader:LoadFrom", "2023-01-01T00:00:00" },
            { "Loader:LoadTo", "2023-01-31T23:59:59" },
            { "Loader:MaxSeccondsPerOperation", "100" }
        };
        public static Loader BuildWorker<T>(Func<IServiceProvider, T> dataStubFactory, Dictionary<string, string>? passedConfig = null)
            where T : class, ISaveLifeDataProvider
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddInMemoryCollection(BuildConfiguration(passedConfig));
                })
                .ConfigureServices((HostBuilderContext hostContext, IServiceCollection services) =>
                {
                    Program.ConfigureWorkerServices(hostContext, services);
                    services.AddSingleton<IPathResolver, PathResolverStub>();
                    services.AddSingleton<ISaveLifeDataProvider, T>(dataStubFactory);

                    // move SaveLifeDataProviderStub here and define stub per test scenario
                })
                .Build();

            var hm = host.Services.GetRequiredService<HistoryManager>();
            return host.Services.GetRequiredService<IHostedService>() as Loader;
        }

        private static Dictionary<string, string> BuildConfiguration(Dictionary<string, string>? passedConfig = null)
        {
            var configuration = new Dictionary<string, string>(_defaultConfiguration);
            if (passedConfig != null)
            {
                foreach (var item in passedConfig)
                {

                    var existingItem = _defaultConfiguration.TryGetValue(item.Key, out var value);
                    if (existingItem)
                    {
                        configuration[item.Key] = item.Value;
                    }
                    else
                    {
                        configuration.Add(item.Key, item.Value);
                    }
                }
            }

            return configuration;
        }
    }
}
