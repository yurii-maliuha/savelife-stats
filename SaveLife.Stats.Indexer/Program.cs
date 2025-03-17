using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Indexer;
using SaveLife.Stats.Indexer.Extensions;
using SaveLife.Stats.Indexer.Models;
using SaveLife.Stats.Indexer.Providers;
using Serilog;
using System.Reflection;
using System.Text;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
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
        var indexerSection = hostContext.Configuration.GetSection(IndexerConfig.DisplayName);
        var indexerConfig = indexerSection.Get<IndexerConfig>();
        services.Configure<IndexerConfig>(indexerSection);
        services.Configure<AggregatorConfig>(hostContext.Configuration.GetSection(nameof(AggregatorConfig)));
        services.AddElasticSearchProviders(hostContext.Configuration);


        services.AddSingleton<ElasticsearchScaffolder>();
        services.AddSingleton<ElasticsearchProvider>(); ;
        services.AddSingleton<TransactionsQueueProvider>();
        services.AddSingleton<MD5HashProvider>();
        services.AddSLDomainServices();


        services.AddSingleton<MongoDbProvider>();
        var mongoDbConfig = hostContext.Configuration.GetSection(nameof(MongoDbConfig)).Get<MongoDbConfig>();
        var mongoClient = new MongoClient(new MongoClientSettings
        {
            RetryWrites = false,
            ReadPreference = ReadPreference.SecondaryPreferred,
            UseTls = false,
            AllowInsecureTls = true,
            Server = new MongoServerAddress(mongoDbConfig.HostName, mongoDbConfig.Port)
        });

        services.AddSingleton(mongoClient);
        services.AddSingleton(mongoClient.GetDatabase(mongoDbConfig.DatabaseName));

        if (indexerConfig.Enable)
        {
            services.AddHostedService<PendingTransactionsPublisher>();
            services.AddHostedService<PendingTransactionConsumer>();
        }
        else
        {
            services.AddHostedService<TransactionsDataAggregator>();
        }
    }
}