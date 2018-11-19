using System;
using System.Collections.Generic;
using System.Text;

namespace OsmSharp.Db.Tiled.Ids
{
    /// <summary>
    /// An id generator.
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// Saves the state of this generator.
        /// </summary>
        void Save();

        /// <summary>
        /// Generates a new if for the given type of object.
        /// </summary>
        long GenerateNew(OsmGeoType type);
    }
}