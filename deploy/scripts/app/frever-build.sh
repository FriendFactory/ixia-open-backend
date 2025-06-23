#!/bin/bash

ENV=$1
GIT_COMMIT=$(git show -s --format=%H)

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
cd "${ROOT_DIR}" || exit
find .. -name '*.csproj' | cpio -pdm  "${SLN_DIR}/lvl1/"
find .. -name '*.sln' | cpio -pdm  "${SLN_DIR}/lvl1/"
find .. -name '*.props' | cpio -pdm  "${SLN_DIR}/lvl1/"
find .. -name 'global.json' | cpio -pdm  "${SLN_DIR}/lvl1/"
find .. -name 'package-lock.json' | cpio -pdm  "${SLN_DIR}/lvl1/"


#####
## Build docker images
#####
APP_DIR="${DIR}/../../application"

## Build basic docker image for all services to prevent running dotnet restore in each services which taking ages
# docker build -f "${APP_DIR}/netservice_base.Dockerfile" --tag frever/netservice-base --progress plain .

ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"

export $(cat ${ENV_FILE} | xargs)

DOCKER_REGISTRY=$(echo ${APPSERVICE_REPOSITORY_URL} | cut -d "/" -f 1)

aws ecr get-login-password --region $REGION --no-verify-ssl | docker login --username AWS --password-stdin ${DOCKER_REGISTRY}

if [[ $ACTION_PLAN ]]; then
    FREVER_CHART="${DIR}/../../application/helm-chart/frever-app/Chart.yaml"
    RELEASE_VERSION_NUMBER=$(grep appVersion "$FREVER_CHART" | cut -d '"' -f 2)
    RELEASE=$ENV-$RELEASE_VERSION_NUMBER
    export RELEASE
    if [[ ! $RELEASE_VERSION_NUMBER =~ ^[0-9]+\.[0-9]+ ]]; then
        echo "Version number in appVersion seems broken."
	exit 1
    fi
else
    export RELEASE=${GIT_COMMIT}
fi

echo "Building version ${RELEASE}."

cd "${APP_DIR}" || exit
docker-compose -f docker-compose.yml build --progress plain appservice
if [[ $? != "0" ]]
then
    ${DIR}/../../ci/notify-slack.sh ${ENV} "Error building appservice image"
    exit 1
fi

docker-compose -f docker-compose.yml push appservice

# tag the images twice, one with the version, one with the git commit
export RELEASE=${GIT_COMMIT}
docker-compose -f docker-compose.yml build --progress plain appservice
docker-compose -f docker-compose.yml push appservice

if [[ $? != "0" ]]
then
    ${DIR}/../../ci/notify-slack.sh ${ENV} "Error pushing appservice image"
    exit 1
fi
