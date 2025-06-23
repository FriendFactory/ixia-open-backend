#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"

export $(cat ${ENV_FILE} | xargs)

DOCKER_COMPOSE_FILE="${DIR}/../../application/docker-compose.yml"
DOCKER_REGISTRY=$(dirname $(dirname ${AUTH_DOCKER_REPOSITORY_URL}))

aws ecr get-login-password --profile friendsfactory --region $REGION \
    | docker login --username AWS --password-stdin ${DOCKER_REGISTRY}

docker-compose -f ${DOCKER_COMPOSE_FILE} build pg_copy && \
docker-compose -f ${DOCKER_COMPOSE_FILE} push pg_copy