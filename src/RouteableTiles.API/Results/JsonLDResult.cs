using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using OsmSharp;
using RouteableTiles.IO.JsonLD;

namespace RouteableTiles.API.Results
{
    internal class JsonLDOutputFormatter : TextOutputFormatter
    {
        public JsonLDOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ld+json"));
            
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }
        
        protected override bool CanWriteType(Type type)
        {
            return typeof(IEnumerable<OsmGeo>).IsAssignableFrom(type);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var writer = new StreamWriter(context.HttpContext.Response.Body);

            if (!(context.Object is IEnumerable<OsmGeo> data))
            {
                throw new InvalidOperationException($"The given object cannot be written by {nameof(JsonLDOutputFormatter)}.");
            }
        
            data.WriteTo(writer);
            
            return Task.CompletedTask;
        }
    }
}