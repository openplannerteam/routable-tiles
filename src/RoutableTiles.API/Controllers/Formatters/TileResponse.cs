using OsmSharp;
using RoutableTiles.API.Db.Tiles;

namespace RoutableTiles.API.Controllers.Formatters;

public class TileResponse
{
    public IReadOnlyList<OsmGeo> Data { get; set; }

    public Tile Tile { get; set; }
}
