
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveLife.Stats.DataParser;
using SaveLife.Stats.Domain.Extensions;
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
            Log.Error(ex, "Unhandled exception in SaveLife.Stats.DataParser");
        }
        finally
        {
            Log.Information("Stopping SaveLife.Stats.DataParser");
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
               services.AddSLDomainServices();
               services.AddHostedService<Worker>();
           });
}