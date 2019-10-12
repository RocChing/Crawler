using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Crawl.Core;
using System.IO;

using Serilog;
using Serilog.Events;

namespace Crawl.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .WriteTo.File("Logs\\.txt", shared: true, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug, retainedFileCountLimit: null, encoding: System.Text.Encoding.UTF8)
           .CreateLogger();

            var host = new HostBuilder()
                 .ConfigureHostConfiguration(config =>
                 {
                     config.SetBasePath(Directory.GetCurrentDirectory());
                 })
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", false, true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddThinkCrawler(context.Configuration);

                    services.AddHostedService<CrawlHostService>();
                })
                .UseSerilog()
                .Build();

            host.Run();
        }
    }
}
