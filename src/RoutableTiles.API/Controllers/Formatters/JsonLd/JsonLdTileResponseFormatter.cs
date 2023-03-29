using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using OsmSharp;
using RoutableTiles.API.Controllers.Formatters.JsonLd.Semantics;

namespace RoutableTiles.API.Controllers.Formatters.JsonLd;


internal class JsonLdTileResponseFormatter : TextOutputFormatter
{
    public JsonLdTileResponseFormatter()
    {
        this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/html"));
        this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ld+json"));

        this.SupportedEncodings.Add(Encoding.UTF8);
        this.SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type? type)
    {
        return typeof(TileResponse).IsAssignableFrom(type);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var baseUrl = context.HttpContext.Request.BasePath();

        if (context.Object is not TileResponse response)
        {
            throw new InvalidOperationException($"The given object cannot be written by {nameof(JsonLdTileResponseFormatter)}.");
        }

        context.HttpContext.Response.Headers[HeaderNames.ContentType] = "application/ld+json";

        await using var jsonWriter = new Utf8JsonWriter(context.HttpContext.Response.Body);
        await response.Data.WriteTo(jsonWriter, response.Tile, baseUrl, TagMapper.DefaultMappingConfigs);
    }
}
