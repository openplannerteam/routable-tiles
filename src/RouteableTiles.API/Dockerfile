############################################################
# Dockerfile Downcount ASP.NET Core Service
# Based on Ubuntu
############################################################

# Set the base image to Ubuntu
FROM mcr.microsoft.com/dotnet/core/sdk:3.1

# File Author / Maintainer
MAINTAINER ANYWAYS BV

# copy api assemblies and files
RUN mkdir /var/app
RUN mkdir /var/app/data
RUN mkdir /var/app/logs
COPY ./bin/release/netcoreapp3.1/publish /var/app
COPY appsettings.Docker.json /var/app/appsettings.json

# couple data folder data volume.
VOLUME ["/var/app/db"]
VOLUME ["/var/app/logs"]

# SET base url
ENV ASPNETCORE_URLS http://+:5000

# Expose ports
EXPOSE 5000

# Set the default command to execute when creating a new container
WORKDIR /var/app/
CMD ["dotnet", "RouteableTiles.API.dll"]