using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveLife.Stats.Downloader;
using SaveLife.Stats.Downloader.Mappers;
using SaveLife.Stats.Downloader.Models;
using SaveLife.Stats.Downloader.Providers;
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
            Log.Error(ex, "Unhandled exception in SaveLife.Stats.Worker");
        }
        finally
        {
            Log.Information("Stopping SaveLife.Stats.Worker");
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
        services.Configure<LoaderConfig>(hostContext.Configuration.GetSection(LoaderConfig.DisplayName));

        services.AddSingleton<IPathResolver, PathResolver>();
        services.AddSingleton<FileManager>();
        services.AddSingleton<HistoryManager>();

        services.AddAutoMapper(typeof(MapperProfile));

        services.AddHttpClient<ISaveLifeDataProvider, SaveLifeDataProvider>();
        services.AddSingleton<SaveLifeDataThrottler>();

        services.AddHostedService<Loader>();
    }
}