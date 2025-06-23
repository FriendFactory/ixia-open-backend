#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
ENV_FILE="${DIR}/.deploy/${ENV}/.env"

export $(cat ${ENV_FILE} | xargs)

aws ecr get-login-password --region $REGION | docker login --username AWS --password-stdin 722913253728.dkr.ecr.eu-central-1.amazonaws.com

# Copy files
rm -rf "${DIR}/docker/files"
mkdir "${DIR}/docker/files"
cp -R "${DIR}/tests/files/" "${DIR}/docker/"

SVCS=("worker" "controller")

cd "${DIR}/docker"

for S in "${SVCS[@]}"; do
    docker-compose build ${S} && docker-compose push ${S}
    if [[ $? == "1" ]]
    then
        echo "Error building ${S}"
        exit 1
    fi
done

