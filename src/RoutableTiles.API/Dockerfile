FROM mcr.microsoft.com/dotnet/sdk:6.0

RUN mkdir /var/app
RUN mkdir /var/app/data
RUN mkdir /var/app/logs
RUN mkdir /var/app/config

VOLUME ["/var/app/config"]
VOLUME ["/var/app/data"]
VOLUME ["/var/app/logs"]

# SET base url
ENV ASPNETCORE_URLS http://+:5000

# Expose ports
EXPOSE 5000

COPY ./bin/release/net6.0/publish /var/app
COPY appsettings.Docker.json /var/app/appsettings.json

# Set the default command to execute when creating a new container
WORKDIR /var/app/
ENTRYPOINT dotnet RoutableTiles.API.dll