using System.Collections.Generic;
using System.Threading.Tasks;
using OsmSharp;
using OsmSharp.Tags;

namespace RouteableTiles.IO.JsonLD.Semantics
{
    public static class TagMapper
    {
        /// <summary>
        /// Returns true if there is a mapping for any of the tags in the given tags collection.
        /// </summary>
        /// <param name="tags">The tags collection.</param>
        /// <param name="mappings">The mappings.</param>
        /// <returns>True, if this tags collection is relevant.</returns>
        public static bool IsRelevant(this TagsCollectionBase tags, Dictionary<string, TagMapperConfig> mappings)
        {
            if (tags == null) return false;
            
            foreach (var tag in tags)
            {
                if (mappings.TryGetValue(tag.Key, out var mapperConfig)) return true;
            }

            return false;
        }
        
        /// <summary>
        /// Maps the given tag using the given semantic mappings.
        /// </summary>
        /// <param name="tag">The tag to map.</param>
        /// <param name="mappings">The mappings.</param>
        /// <param name="writer">The json writer.</param>
        /// <returns>True if there was a mapping for this tag.</returns>
        internal static async Task<bool> Map(this Tag tag, Dictionary<string, TagMapperConfig> mappings, JsonWriter writer)
        {
            if (!mappings.TryGetValue(tag.Key, out var mapperConfig)) return false;

            if (mapperConfig.mapping == null)
            {
                await writer.WritePropertyAsync(mapperConfig.predicate, tag.Value, true, true);
                return true;
            }
            if (mapperConfig.mapping.TryGetValue(tag.Value, out var mapped))
            {
                var mappedString = string.Empty;
                if (mapped != null) mappedString = mapped.ToInvariantString();
                if (mapped is int ||
                    mapped is long)
                {
                    await writer.WritePropertyAsync(mapperConfig.predicate, mappedString, false, false);
                }
                else
                {
                    await writer.WritePropertyAsync(mapperConfig.predicate, mappedString, true, true);
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}