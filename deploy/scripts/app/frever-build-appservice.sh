#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

SLN_DIR="${DIR}/../../.deploy/sln"

# Recursive copy the project files with keeping directory structure
# That's required for installing packages

# Added two dummy levels (lvl1/lvl2) to path since
# find command run two level deeper than root
# and returns paths with leading ../..
rm -rf "${SLN_DIR}"

ROOT_DIR="${DIR}/../.."
cd ${ROOT_DIR}
find .. -name '*.csproj' | cpio -pdm  "${SLN_DIR}/lvl1/"
find .. -name '*.sln' | cpio -pdm  "${SLN_DIR}/lvl1/"

#####
## Build docker images
#####
APP_DIR="${DIR}/../../application"

## Build basic docker image for all services to prevent running dotnet restore in each services which taking ages
# docker build -f "${APP_DIR}/netservice_base.Dockerfile" --tag frever/netservice-base --progress plain .

ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"

export $(cat ${ENV_FILE} | xargs)

DOCKER_COMPOSE_FILE="${APP_DIR}/docker-compose.yml"
DOCKER_REGISTRY=$(dirname $(dirname ${AUTH_DOCKER_REPOSITORY_URL}))

aws ecr get-login-password --profile friendsfactory --region $REGION | docker login --username AWS --password-stdin ${DOCKER_REGISTRY}

SVCS=("appservice")
#SVCS=("assetmanager" "auth" "notification" "asset" "main" "video" "social")

for S in "${SVCS[@]}"; do
    docker-compose -f application/docker-compose.yml build --pull --progress plain ${S} && docker-compose -f application/docker-compose.yml push ${S}
    if [[ $? == "1" ]]
    then
        echo "Error building ${S}"
        exit 1
    fi
done

