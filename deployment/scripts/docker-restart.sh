#!/usr/bin/env bash

NAME=$1
PORT=$2
IMAGE=$3

echo "restart $NAME"
docker stop $NAME
docker rm $NAME
docker run -d -v /var/services/data/db/:/var/app/db/ -v /var/services/api/logs/:/var/app/logs/ -p $PORT:5000 --name $NAME $IMAGE