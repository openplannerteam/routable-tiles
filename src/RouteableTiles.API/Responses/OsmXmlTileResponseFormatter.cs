using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using OsmSharp.Streams;

namespace RouteableTiles.API.Responses
{
    internal class OsmXmlResponseFormatter : TextOutputFormatter
    {
        public OsmXmlResponseFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }
        
        protected override bool CanWriteType(Type type)
        {
            return typeof(OsmTileResponse).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (!(context.Object is OsmTileResponse response))
            {
                throw new InvalidOperationException($"The given object cannot be written by {nameof(OsmXmlResponseFormatter)}.");
            }
            
            // copy to buffer first, stream target doesn't allow async writing and sync writes are not allowed.
            var memoryStream = new MemoryStream();
            var xmlStreamTarget = new XmlOsmStreamTarget(memoryStream);
            xmlStreamTarget.RegisterSource(response.Data);
            xmlStreamTarget.Pull();
            xmlStreamTarget.Flush();
            
            // copy buffer to body.
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(context.HttpContext.Response.Body);
        }
    }
}