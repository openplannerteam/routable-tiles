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

```dotnet run /home/xivk/data/wechel.osm.pbf ./output/ 12```

A complete example for Belgium:

```
wget http://download.geofabrik.de/europe/belgium-latest.osm.pbf
mkdir output
dotnet run belgium-latest.osm.pbf ./output 12
```
