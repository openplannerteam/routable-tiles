#!/usr/bin/env bash

set -e

NAME=$1
PORT=$2
IMAGE="openplannerteam/routeable-tiles-api"

LATEST=`docker inspect --format "{{.Id}}" $IMAGE`
RUNNING=`docker inspect --format "{{.Image}}" $NAME`
echo "Latest:" $LATEST
echo "Running:" $RUNNING

if [ "$RUNNING" != "$LATEST" ];then
  echo "upgrading $NAME"
  ./docker-restart.sh $NAME $PORT $IMAGE
else
  echo "$NAME up to date."
fi