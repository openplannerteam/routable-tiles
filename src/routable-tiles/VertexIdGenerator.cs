using System;

namespace routable_tiles
{
    public static class VertexIdGenerator
    {
        /// <summary>
        /// The maximum possible node id.
        /// </summary>
        public const long MaxNodeId = 2 ^ 47 - 1;

        /// <summary>
        /// The minimum possible node id.
        /// </summary>
        public const long MinNodeId = -(MaxNodeId);

        /// <summary>
        /// Builds a stable id, taking into account both the node id and it's version.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="version">The version.</param>
        /// <returns>An signed 64 ID, consisting of a sign byte, 47 bits of node id and 16 bits over version.</returns>
        public static long BuildVertexId(long node, ushort version)
        {
            if (node > MaxNodeId || node < MinNodeId) throw new ArgumentOutOfRangeException($"Cannot properly generate an id for a node with id: {node} - should be in the range [{MinNodeId},{MaxNodeId}].");
            
            if (node > 0)
            {
                return (node << 16) + version;
            }
            return (node << 16) - version;
        }
    }
}