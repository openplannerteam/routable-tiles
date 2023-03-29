# routable-tiles

[![Production](https://github.com/openplannerteam/routable-tiles/actions/workflows/production.yml/badge.svg)](https://github.com/openplannerteam/routable-tiles/actions/workflows/production.yml) 
  
A CLI tool to generate routable tiles. The main goal of this tool is to generate ready to use 'routable tiles' that can be consumed by existing routeplanning apps.

This is an example for the city of Ghent:

![Image of tiles for ghent](gent.png)

Or the Benelux:

![Image of tiles for ghent](benelux.png)

## Generating tiles

First filter out all non-routing data using osmosis:

`osmosis --read-pbf brussels-latest.osm.pbf --lp --tf accept-ways highway=* route=* --tf accept-relations type=route,restriction --used-node --lp --write-pbf brussels-routing.osm.pbf`

On a planet scale you can do this in three steps if it fails:

1. Extract ways: `osmosis --read-pbf planet-latest.osm.pbf --lp --tf accept-ways highway=* route=* --lp --write-pbf planet-1-highways.osm.pbf`
2. Only keep used nodes: `osmosis --read-pbf planet-1-highways.osm.pbf --lp --used-node --lp --write-pbf planet-1-used-nodes.osm.pbf`
3. Only keep routing related relations: `osmosis --read-pbf planet-1-used-nodes.osm.pbf --tf accept-relations type=route,restriction --lp --write-pbf planet-routing.osm.pbf`

REMARK: this can probably be optimized by tuning osmosis or using another tool to extract the routing data.

Second step is to convert the planet file into a tiled OSM database. This 'database' is just a collection of files on disk containing the OSM data per tile.



## Deployment

This is via docker, start the container like this, with `/path/to/db` the path to the database create by the CLI project:

`docker run -d -v /path/to/db:/var/app/db/ -v /path/to/logs/:/var/app/logs/ -p 5000:5000 --name routeable-tiles-api openplannerteam/routeable-tiles-api`