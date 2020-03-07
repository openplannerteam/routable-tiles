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
using RouteableTiles.API.Controllers;
using RouteableTiles.IO.JsonLD;
using RouteableTiles.IO.JsonLD.Semantics;

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
        
        /// <summary>
        /// Gets or sets the mappings.
        /// </summary>
        internal static Dictionary<string, TagMapperConfig> Mapping { get; set; }
        
        protected override bool CanWriteType(Type type)
        {
            return typeof(TileResponse).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var writer = new StreamWriter(context.HttpContext.Response.Body);

            if (!(context.Object is TileResponse response))
            {
                throw new InvalidOperationException($"The given object cannot be written by {nameof(JsonLDOutputFormatter)}.");
            }
        
            await response.Data.WriteTo(writer, response.Tile, "https://tiles.openplanner.team/planet/", JsonLDOutputFormatter.Mapping);
        }
    }
}