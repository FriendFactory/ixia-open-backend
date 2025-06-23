#!/bin/bash

DOCKER_REGISTRY=722913253728.dkr.ecr.eu-central-1.amazonaws.com

aws ecr get-login-password --no-verify-ssl | sudo docker login --username AWS --password-stdin ${DOCKER_REGISTRY}

sudo docker-compose build 
sudo docker-compose push