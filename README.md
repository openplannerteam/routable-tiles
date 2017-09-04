# routable-tiles

A CLI tool to generate routable tiles.

The main goal of this tool is to generate ready to use 'routable tiles' that can be consumed by existing routeplanning apps.

This is an example for the city of Ghent:

![Image of tiles for ghent](gent.png)

Or the Benelux:

![Image of tiles for ghent](benelux.png)

### Usage

- Install [.NET core]().
- Run ```publish.sh```

Now a built version should be available in the publish folder:

```.\bin\release\netcoreapp2.0\ubuntu.16.04-x64\publish\routable-tiles \path\to\source-file.osm.pbf \path\to\output-folder 12```
