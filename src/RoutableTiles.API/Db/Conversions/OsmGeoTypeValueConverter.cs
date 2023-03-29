using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OsmSharp;

namespace RoutableTiles.API.Db.Conversions;

internal class OsmGeoTypeValueConverter : ValueConverter<OsmGeoType, int>
{
    public OsmGeoTypeValueConverter() :
        base(t => FromOsmGeoType(t),
            t => ToOsmGeoType(t))
    {

    }

    private static int FromOsmGeoType(OsmGeoType type)
    {
        switch (type)
        {
            case OsmGeoType.Node:
                return 1;
            case OsmGeoType.Way:
                return 2;
            case OsmGeoType.Relation:
                return 3;
        }

        throw new ArgumentOutOfRangeException(nameof(type), $"Invalid {nameof(OsmGeoType)}");
    }

    private static OsmGeoType ToOsmGeoType(int type)
    {
        switch (type)
        {
            case 1:
                return OsmGeoType.Node;
            case 2:
                return OsmGeoType.Way;
            case 3:
                return OsmGeoType.Relation;
        }

        throw new ArgumentOutOfRangeException(nameof(type), $"Invalid type id.");
    }
}
