# routable-tiles

[![Build status](https://build.anyways.eu/app/rest/builds/buildType:(id:anyways_Openplannerteam_RoutableTiles)/statusIcon)](https://build.anyways.eu/viewType.html?buildTypeId=anyways_Openplannerteam_RoutableTiles)
  
A CLI tool to generate routable tiles. The main goal of this tool is to generate ready to use 'routable tiles' that can be consumed by existing routeplanning apps.

This is an example for the city of Ghent:

![Image of tiles for ghent](gent.png)

Or the Benelux:

![Image of tiles for ghent](benelux.png)

## Generating tiles

First filter out all non-routing data using osmosis:

`osmosis --read-pbf brussels-latest.osm.pbf --lp --tf accept-ways highway=* route=* --tf accept-relations type=route,restriction --used-node --lp --write-pbf brussels-routing.osm.pbf`

## Deployment

This is via docker, start the container like this, with `/path/to/db` the path to the database create by the CLI project:

`docker run -d -v /path/to/db:/var/app/db/ -v /path/to/logs/:/var/app/logs/ -p 5000:5000 --name routeable-tiles-api openplannerteam/routeable-tiles-api`