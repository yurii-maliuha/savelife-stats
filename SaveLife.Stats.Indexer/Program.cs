using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Mappers;
using SaveLife.Stats.Indexer;
using SaveLife.Stats.Indexer.Extensions;
using SaveLife.Stats.Indexer.Models;
using SaveLife.Stats.Indexer.Providers;
using Serilog;
using System.Reflection;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .CreateLogger();

        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception in SaveLife.Stats.Indexer");
        }
        finally
        {
            Log.Information("Stopping SaveLife.Stats.Indexer");
            Log.CloseAndFlush();
            cancellationTokenSource.Cancel();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
       Host.CreateDefaultBuilder(args)
           .ConfigureAppConfiguration((context, builder) =>
           {
               builder
                   .SetBasePath(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName);
           })
           .ConfigureServices(ConfigureWorkerServices);

    public static void ConfigureWorkerServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.Configure<DataSourceConfig>(hostContext.Configuration.GetSection(DataSourceConfig.DisplayName));
        services.AddElasticSearchProviders(hostContext.Configuration);
       

        services.AddSingleton<ElasticsearchScaffolder>();
        services.AddSingleton<ElasticsearchProvider>();;
        services.AddSingleton<TransactionsQueueProvider>();

        services.AddSLDomainServices();

        services.AddAutoMapper(typeof(MapperProfile));

        services.AddHostedService<PendingTransactionsPublisher>();
        services.AddHostedService<PendingTransactionConsumer>();
    }
}