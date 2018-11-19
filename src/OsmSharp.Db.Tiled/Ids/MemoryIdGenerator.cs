using System;
namespace OsmSharp.Db.Tiled.Ids
{
    /// <summary>
    /// An in-memory id generator.
    /// </summary>
    public class MemoryIdGenerator : IIdGenerator
    {
        private long _nextNodeId = -1;
        private long _nextWayId = -1;
        private long _nextRelationId = -1;

        /// <summary>
        /// Creates a new in-memory id generator.
        /// </summary>
        public MemoryIdGenerator(long nextNodeId = -1, long nextWayId = -1, long nextRelationId = -1)
        {
            _nextNodeId = nextNodeId;
            _nextWayId = nextWayId;
            _nextRelationId = nextRelationId;
        }

        /// <summary>
        /// Generates a new id.
        /// </summary>
        public long GenerateNew(OsmGeoType type)
        {
            var res = -1L;
            switch(type)
            {
                case OsmGeoType.Node:
                    res = _nextNodeId;
                    _nextNodeId--;
                    break;
                case OsmGeoType.Way:
                    res = _nextWayId;
                    _nextWayId--;
                    break;
                case OsmGeoType.Relation:
                    res = _nextRelationId;
                    _nextRelationId--;
                    break;
            }
            return res;
        }

        /// <summary>
        /// Saves this generator's state to disk.
        /// </summary>
        public void Save()
        {

        }
    }
}