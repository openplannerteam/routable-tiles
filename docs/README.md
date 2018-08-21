routable-tiles
==============

This is an overview of the design and spec of _routable tiles_.

### Basic idea

- Split the world in tiles.
- Add all edges to a tile that have one or more vertices in the tile. An edge consists of:
  - A first and last vertex.
  - It's geometry.
  - It's attributes.
- A vertex id consists of:
  - Tile id: a unique id per tile.
  - Node id: an ID based on the original OSM node and it's version.

When routeplanning we can download tiles on-the-fly.

### Extensions

#### 'along' tiles

We can also publish tiles that only contain information that can be used for intermediate routing. The idea is to only store connections between vertices that may be on a shortest path when routing through the tile. We only need to store information about the 'connections', we don't need the whole tile. This means we can serve tiles at a higher zoom level for routing and a lower level for start and end points.

#### Customizable contraction hierarchies
