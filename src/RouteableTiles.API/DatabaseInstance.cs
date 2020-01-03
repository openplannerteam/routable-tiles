using OsmSharp.Db.Tiled;

namespace RouteableTiles.API
{
    internal static class DatabaseInstance
    {
        /// <summary>
        /// Gets or sets the default database instance.
        /// </summary>
        public static OsmDb Default { get; set; }
    }
}