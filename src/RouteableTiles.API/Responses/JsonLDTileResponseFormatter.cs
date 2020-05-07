using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using RouteableTiles.IO.JsonLD;
using RouteableTiles.IO.JsonLD.Semantics;

namespace RouteableTiles.API.Responses
{
    internal class JsonLDTileResponseFormatter : TextOutputFormatter
    {
        public JsonLDTileResponseFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/html"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ld+json"));
            
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }
        
        /// <summary>
        /// Gets or sets the mappings.
        /// </summary>
        internal static Dictionary<string, TagMapperConfig>? Mapping { get; set; }
        
        /// <summary>
        /// Gets or sets the key mappings.
        /// </summary>
        internal static Dictionary<string, TagMapperKey>? MappingKeys { get; set; }
        
        protected override bool CanWriteType(Type type)
        {
            return typeof(OsmTileResponse).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var baseUrl = context.HttpContext.Request.BasePath();
            
            await using var writer = new StreamWriter(context.HttpContext.Response.Body);

            if (!(context.Object is OsmTileResponse response))
            {
                throw new InvalidOperationException($"The given object cannot be written by {nameof(JsonLDTileResponseFormatter)}.");
            }

            context.HttpContext.Response.Headers[HeaderNames.ContentType] = "application/ld+json";
            
            writer.AutoFlush = false;
            await response.Data.WriteTo(writer, response.Tile, baseUrl, Mapping ?? TagMapper.DefaultMappingConfigs);
        }
    }
}