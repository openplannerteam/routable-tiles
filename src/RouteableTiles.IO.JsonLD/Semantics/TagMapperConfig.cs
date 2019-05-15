using System.Collections.Generic;

namespace RouteableTiles.IO.JsonLD.Semantics
{
    /// <summary>
    /// Represents mapping configuration for a single tag.
    /// </summary>
    public class TagMapperConfig
    {
        /// <summary>
        /// Gets or sets the osm key.
        /// </summary>
        public string osm_key { get; set; }
        
        /// <summary>
        /// Gets or sets the predicate.
        /// </summary>
        public string predicate { get; set; }
        
        /// <summary>
        /// Gets or sets the mapping.
        /// </summary>
        public Dictionary<string, object> mapping { get; set; }
    }
}