############################################################
# Dockerfile Downcount ASP.NET Core Service
# Based on Ubuntu
############################################################

# Set the base image to Ubuntu
FROM microsoft/dotnet:2.1-sdk

# File Author / Maintainer
MAINTAINER Anyways BVBA

# copy api assemblies and files
RUN mkdir /var/app
RUN mkdir /var/app/source
RUN mkdir /var/app/target
RUN mkdir /var/app/logs
COPY ./bin/Release/netcoreapp2.1/linux-x64/publish /var/app

# couple data folder data volume.
VOLUME ["/var/app/source"]
VOLUME ["/var/app/target"]
VOLUME ["/var/app/logs"]

# Set the default command to execute when creating a new container
WORKDIR /var/app/
CMD ["dotnet", "Routabletiles.CLI.dll", "/var/app/source/planet-latest.osm.pbf", "/var/app/target/cache/", "14", "/var/app/target/routabletiles/"]