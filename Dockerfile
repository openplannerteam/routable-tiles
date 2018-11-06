# Set the base image to Ubuntu
FROM microsoft/dotnet:2.1-aspnetcore-runtime

# File Author / Maintainer
MAINTAINER Anyways BVBA

# copy api assemblies and files
RUN mkdir /var/app
RUN mkdir /var/app/data
RUN mkdir /var/app/logs
COPY ./bin/release/netcoreapp2.1/publish /var/app
COPY appsettings.Docker.json /var/app/appsettings.json

# install cron.
RUN apt-get update
RUN apt-get install -y cron
ADD docker-crontab /
RUN crontab /docker-crontab

# Set the default command to execute when creating a new container
WORKDIR /var/app/
ENTRYPOINT cron -f

# couple data folder data volume.
VOLUME ["/var/app/data"]
VOLUME ["/var/app/logs"]

# Set the default command to execute when creating a new container
WORKDIR /var/app/
ENTRYPOINT cron -f