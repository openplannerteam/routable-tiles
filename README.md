# routable-tiles

A CLI tool to generate routable tiles.

The main goal of this tool is to generate ready to use 'routable tiles' that can be consumed by existing routeplanning apps.

This is an example for the city of Ghent:

![Image of tiles for ghent](gent.png)

Or the Benelux:

![Image of tiles for ghent](benelux.png)

### Usage

- Install [.NET core](https://www.microsoft.com/net/download/core).
- Clone this repo.

Now run the following:

```dotnet run /path/to/source-file.osm.pbf ./output/ 12```

A complete example splitting Belgium in tiles for zoom-level 12:

```
wget http://download.geofabrik.de/europe/belgium-latest.osm.pbf
mkdir output
dotnet run belgium-latest.osm.pbf ./output 12
```

The result of a split can be seen in an example area uploaded [here](https://github.com/openplannerteam/routable-tiles/tree/master/examples/gent/14).

