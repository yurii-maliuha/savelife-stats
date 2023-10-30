using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveLife.Stats.Worker;
using SaveLife.Stats.Worker.Mappers;
using SaveLife.Stats.Worker.Models;
using SaveLife.Stats.Worker.Providers;
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
           .ConfigureServices((hostContext, services) =>
           {
               services.Configure<DataSourceConfig>(hostContext.Configuration.GetSection(DataSourceConfig.DisplayName));
               services.Configure<LoaderConfig>(hostContext.Configuration.GetSection(LoaderConfig.DisplayName));

               services.AddHttpClient<SaveLifeDataProvider>();
               services.AddSingleton<FileManager>();
               services.AddSingleton<HistoryManager>();

               services.AddAutoMapper(typeof(MapperProfile));

               services.AddHostedService<Loader>();
           });
}