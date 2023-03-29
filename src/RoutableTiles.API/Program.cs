using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OsmSharp.IO.Json.Converters;
using RoutableTiles.API.Controllers.Formatters.JsonLd;
using RoutableTiles.API.Controllers.Formatters.JsonLd.Semantics;
using RoutableTiles.API.Db;
using RoutableTiles.API.Db.Caches;
using RoutableTiles.API.Db.Caches.Disk;
using RoutableTiles.API.Services.LatestCommit;
using Serilog;
using Serilog.Formatting.Json;

namespace RoutableTiles.API;

public class Program
{
    private const string AllowAllOrigins = "_AllowAllOrigins";

    public static async Task Main(string[] args)
    {
        // hardcode configuration before the configured logging can be bootstrapped.
        var logFile = Path.Combine("logs", "boot-log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(new JsonFormatter(), logFile, rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true);

            // get deploy time setting.
            var (deployTimeSettings, envVarPrefix) = configurationBuilder.GetDeployTimeSettings();

            var host = WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    Log.Information("Env: {EnvironmentName}",
                        hostingContext.HostingEnvironment.EnvironmentName);

                    config.AddJsonFile(deployTimeSettings, true, true);
                    Log.Logger.Debug("Env configuration prefix: {EnvVarPrefix}", envVarPrefix);
                    config.AddEnvironmentVariables((c) => { c.Prefix = envVarPrefix; });
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    try
                    {
                        // setup database.
                        var connectionString = hostingContext.Configuration.GetPostgresConnectionString("OSM_DB")
                            .Result;
                        Log.Logger.Debug("Connection string: {ConnectionString}", connectionString);
                        services.AddDbContext<OsmDbContext>(o => o.UseNpgsql(connectionString));

                        // add caches.
                        services.AddSingleton(new SnapshotCommitTilesDiskCacheSettings()
                        {
                            CachePath = hostingContext.Configuration["CACHE_PATH"],
                        });
                        services.AddSingleton<SnapshotCommitTilesDiskCache>();
                        services.AddSingleton(new SnapshotCommitTilesCacheSettings()
                        {
                            CacheSize = 1024
                        });
                        services.AddSingleton<SnapshotCommitTilesCache>();
                        services.AddSingleton<SnapshotCommitIdsCache>();
                        services.AddSingleton(new SnapshotCommitIdsCacheSettings());
                        services.AddSingleton<SnapshotCommitsByTimestampCache>();
                        services.AddSingleton<LatestCommitStore>();

                        // add cors.
                        services.AddCors(options =>
                        {
                            options.AddPolicy(name: AllowAllOrigins,
                                builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
                        });

                        // add controllers and formatters.
                        services.AddMvc(opt =>
                        {
                            opt.OutputFormatters.Insert(0,new JsonLdTileResponseFormatter());
                        }).AddJsonOptions(opt =>
                        {
                            opt.JsonSerializerOptions.Converters.Add(new OsmJsonConverter());
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Fatal(e, "Unhandled exception during configuration");
                        throw;
                    }
                })
                .Configure((builder, app) =>
                {
                    app.UseForwardedNGINXHeaders();

                    app.UseCors(AllowAllOrigins);

                    app.UseRouting();

                    app.UseAuthorization();

                    app.UseStaticFiles();

                    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
                })
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext())
                .Build();

            // run!
            await host.RunAsync();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Unhandled exception");
            throw;
        }
    }
}
