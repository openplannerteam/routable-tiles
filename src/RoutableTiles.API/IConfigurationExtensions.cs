namespace RoutableTiles.API;

internal static class IConfigurationExtensions
{
    /// <summary>
    /// Loads deploy time app settings using a path defined in the build-time settings.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder.</param>
    /// <returns>The deploy time settings.</returns>
    public static (string deployTimeSettings, string envVarPrefix) GetDeployTimeSettings(
        this IConfigurationBuilder configurationBuilder)
    {
        // get deploy time settings if present.
        var configuration = configurationBuilder.Build();
        var deployTimeSettings = configuration["deploy-time-settings"] ?? "/var/app/config/appsettings.json";
        configurationBuilder = configurationBuilder.AddJsonFile(deployTimeSettings, true, true);

        // get environment variable prefix.
        // do this after the deploy time settings to make sure this is configurable at deploytime.
        configuration = configurationBuilder.Build();
        var envVarPrefix = configuration["env-var-prefix"] ?? "OPT_";

        return (deployTimeSettings, envVarPrefix);
    }

    /// <summary>
    /// Gets a connection string either from a file or from individually configured keys.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="keyPrefix">The key prefix.</param>
    /// <param name="commandTimeout">The command timeout, by default disabled at 0. Won't take any effect when the raw connection string is in the appsettings.</param>
    /// <returns>The connection string.</returns>
    public static async Task<string?> GetPostgresConnectionString(this IConfiguration configuration, string keyPrefix,
        int commandTimeout = 0)
    {
        var connectionString = configuration[keyPrefix];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var file = configuration[$"{keyPrefix}_FILE"];
        if (!string.IsNullOrWhiteSpace(file))
        {
            return await File.ReadAllTextAsync(file);
        }

        var hasUser = configuration.TryGetValueOrDefault<string>($"{keyPrefix}_USER", out var user, "postgres");
        var hasPass = configuration.TryGetValueOrDefault<string>($"{keyPrefix}_PASS", out var pass);
        if (string.IsNullOrWhiteSpace(pass))
        {
            hasPass = configuration.TryGetValueOrDefault<string>($"{keyPrefix}_PASS_FILE", out pass);
            if (!string.IsNullOrWhiteSpace(pass)) pass = await File.ReadAllTextAsync(pass);
        }

        var hasDb = configuration.TryGetValueOrDefault<string>($"{keyPrefix}_DB", out var db, "db");
        var hasHost = configuration.TryGetValueOrDefault<string>($"{keyPrefix}_HOST", out var host, "localhost");
        var hasPort = configuration.TryGetValueOrDefault<string>($"{keyPrefix}_PORT", out var port, "5432");

        if (!hasDb && !hasPass && !hasUser && !hasHost && !hasPort)
        {
            // make sure there is no value when nothing has been set.
            return null;
        }

        if (string.IsNullOrWhiteSpace(pass))
        {
            return $"Host={host};Port={port};Database={db};Username={user};Command Timeout={commandTimeout};";
        }

        return
            $"Host={host};Port={port};Database={db};Username={user};Password={pass};Command Timeout={commandTimeout};";
    }

    /// <summary>
    /// Tries to get a typed value, if not found returns false and a default value.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The default.</param>
    /// <param name="value">The value.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>True if a value was found, false otherwise.</returns>
    public static bool TryGetValueOrDefault<T>(this IConfiguration configuration, string key, out T value,
        T defaultValue = default)
    {
        var stringValue = configuration[key];
        if (stringValue == null)
        {
            value = defaultValue;
            return false;
        }

        value = configuration.GetValue<T>(key);
        return true;
    }
}
