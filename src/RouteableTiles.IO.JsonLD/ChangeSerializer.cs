using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RouteableTiles.IO.JsonLD
{
    public static class ChangeSerializer
    {
        /// <summary>
        /// Writes the changed tiles in JSON-LD format.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="url">The url.</param>
        /// <param name="baseUrl">The base url.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="next">The next page url.</param>
        /// <param name="previous">The previous page url.</param>
        public static async Task WriteTo(this IEnumerable<(string timestamp, uint x, uint y, uint z)>? data, 
            TextWriter writer, string url, string baseUrl, string? previous = null, string? next = null)
        {
            if (!baseUrl.EndsWith("/")) baseUrl += '/';
            
            var jsonWriter = new JsonWriter(writer);
            await jsonWriter.WriteOpenAsync();
            
            await jsonWriter.WriteContextAsync(baseUrl, url, next, previous);
            
            await jsonWriter.WritePropertyNameAsync("@graph");
            await jsonWriter.WriteArrayOpenAsync();
            // @type	"hydra:PartialCollectionView"
            if (data == null)
            {
                await jsonWriter.WriteOpenAsync();
                await jsonWriter.WritePropertyAsync("@id", $"{baseUrl}", true);
                await jsonWriter.WriteCloseAsync();
            }
            else
            {
                foreach (var tile in data)
                {
                    await jsonWriter.WriteOpenAsync();
                    await jsonWriter.WritePropertyAsync("@id", $"{baseUrl}{tile.timestamp}/{tile.z}/{tile.x}/{tile.y}/", true);
                    await jsonWriter.WriteCloseAsync();
                }
            }

            await jsonWriter.WriteArrayCloseAsync();
            await jsonWriter.WriteCloseAsync();
            await jsonWriter.FlushAsync();
        }
        
        internal static async Task WriteContextAsync(this JsonWriter writer, string baseUrl, string url, string? next, string? previous)
        {
            await writer.WritePropertyNameAsync("@context");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("hydra", "http://www.w3.org/ns/hydra/core#", true);
            
            await writer.WritePropertyNameAsync("hydra:variableRepresentation", true);
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            await writer.WritePropertyNameAsync("hydra:property");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            await writer.WritePropertyNameAsync("hydra:next");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            await writer.WritePropertyNameAsync("hydra:previous");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            await writer.WriteCloseAsync();
            
            await writer.WritePropertyAsync("@id", url, true);
            await writer.WritePropertyAsync("@type", "hydra:PartialCollectionView", true);
            if (previous != null) await writer.WritePropertyAsync("hydra:previous", $"{baseUrl}/changes/{previous}", true);
            if (next != null) await writer.WritePropertyAsync("hydra:next", $"{baseUrl}/changes/{next}", true);
        }
    }
}