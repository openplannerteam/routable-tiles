using System.Linq;
using Microsoft.AspNetCore.Http;

namespace RouteableTiles.API
{
    internal static class HttpRequestExtensions
    {
        public static string PublicPath(this HttpRequest request)
        {
            var domain = request.Host.ToString();

            var requestPath = request.Path;
            return $"https://{domain}{requestPath}";
        }

        /// <summary>
        /// Gets the public URL this service is hosted at.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The url.</returns>
        public static string BasePath(this HttpRequest request)
        {
            var domain = request.Host.ToString();
            var path = string.Empty;
            if (request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBases))
            {
                path = pathBases.First();
            }
            return $"https://{domain}{path}";
        }
    }
}