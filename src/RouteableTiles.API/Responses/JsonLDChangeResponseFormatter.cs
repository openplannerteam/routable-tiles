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
    public class JsonLDChangeResponseFormatter : TextOutputFormatter
    {
        public JsonLDChangeResponseFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/html"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ld+json"));
            
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }
        
        internal static Dictionary<string, TagMapperKey>? MappingKeys { get; set; }
        
        protected override bool CanWriteType(Type type)
        {
            return typeof(ChangeResponse).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var baseUrl = context.HttpContext.Request.BasePath();
            var publicPath = context.HttpContext.Request.PublicPath();
            
            await using var writer = new StreamWriter(context.HttpContext.Response.Body);

            if (!(context.Object is ChangeResponse response))
            {
                throw new InvalidOperationException($"The given object cannot be written by {nameof(JsonLDChangeResponseFormatter)}.");
            }

            context.HttpContext.Response.Headers[HeaderNames.ContentType] = "application/ld+json";
            
            writer.AutoFlush = false;
            await response.Tiles.WriteTo(writer, publicPath, baseUrl, response.Previous, response.Next);
        }
    }
}