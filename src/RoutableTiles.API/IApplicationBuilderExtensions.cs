using Microsoft.AspNetCore.HttpOverrides;

namespace RoutableTiles.API;

internal static class IApplicationBuilderExtensions
{
    // ReSharper disable once InconsistentNaming
    public static void UseForwardedNGINXHeaders(this IApplicationBuilder app)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
        };
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        app.UseForwardedHeaders(options);
        app.Use((context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBases))
            {
                context.Request.PathBase = pathBases.First();
                if (context.Request.PathBase.Value.EndsWith("/"))
                {
                    context.Request.PathBase =
                        context.Request.PathBase.Value.Substring(0, context.Request.PathBase.Value.Length - 1);
                }
                if (context.Request.Path.Value.StartsWith(context.Request.PathBase.Value))
                {
                    var before = context.Request.Path.Value;
                    var after = context.Request.Path.Value.Substring(
                        context.Request.PathBase.Value.Length,
                        context.Request.Path.Value.Length - context.Request.PathBase.Value.Length);
                    context.Request.Path = after;
                }
            }
            return next();
        });
    }
}
