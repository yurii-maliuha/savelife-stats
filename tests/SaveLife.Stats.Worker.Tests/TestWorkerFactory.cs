using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveLife.Stats.Worker.Providers;

namespace SaveLife.Stats.Worker.Tests
{
    public static class TestWorkerFactory
    {
        private static Dictionary<string, string> _defaultConfiguration = new Dictionary<string, string>()
        {
            { "Loader:ThrottleSeconds", "1" },
            { "Loader:MaxIterationsCount", "2" },
        };
        public static Loader BuildWorker(Dictionary<string, string>? passedConfig = null)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddInMemoryCollection(BuildConfiguration(passedConfig));
                })
                .ConfigureServices((HostBuilderContext hostContext, IServiceCollection services) =>
                {
                    Program.ConfigureWorkerServices(hostContext, services);
                    services.AddSingleton<IPathResolver, TestPathResolver>();

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
