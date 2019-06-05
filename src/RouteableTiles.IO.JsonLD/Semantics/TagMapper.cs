using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Tags;

namespace RouteableTiles.IO.JsonLD.Semantics
{
    internal static class TagMapper
    {
        /// <summary>
        /// Maps the given tag using the given semantic mappings.
        /// </summary>
        /// <param name="tag">The tag to map.</param>
        /// <param name="mappings">The mappings.</param>
        /// <param name="writer">The json writer.</param>
        /// <returns>True if there was a mapping for this tag.</returns>
        public static bool Map(this Tag tag, Dictionary<string, TagMapperConfig> mappings, JsonWriter writer)
        {
            if (!mappings.TryGetValue(tag.Key, out var mapperConfig)) return false;

            if (mapperConfig.mapping == null)
            {
                writer.WriteProperty(mapperConfig.predicate, tag.Value, true, true);
                return true;
            }
            if (mapperConfig.mapping.TryGetValue(tag.Value, out var mapped))
            {
                var mappedString = string.Empty;
                if (mapped != null) mappedString = mapped.ToInvariantString();
                if (mapped is int ||
                    mapped is long)
                {
                    writer.WriteProperty(mapperConfig.predicate, mappedString, false, false);
                }
                else
                {
                    writer.WriteProperty(mapperConfig.predicate, mappedString, true, true);
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