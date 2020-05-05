using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace RouteableTiles.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logFile = Path.Combine("logs", "log-.txt");
            Log.Logger = new LoggerConfiguration()
#if RELEASE
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
#else
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
#endif
                .Enrich.FromLogContext()
                .WriteTo.File(new JsonFormatter(), logFile, rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();
            
            try
            {
                Log.Information("Starting web host...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                        .UseSerilog() // <- Add this line
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseStartup<Startup>();
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddHostedService<DatabaseRefreshService>();
                        });
    }
}