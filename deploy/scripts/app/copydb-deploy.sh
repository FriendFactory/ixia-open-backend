#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

kubectl config use-context $( kubectl config get-contexts | grep "${ENV}" | awk '{ print $2 }')

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"
export $(cat ${ENV_FILE} | xargs)

cd "${DIR}/../../application/helm-chart"

helm upgrade \
    --install \
    --set "repository=${AUTH_DOCKER_REPOSITORY_URL}" \
    --set "settings.db.server=${DB_SERVER}" \
    --set "settings.db.user=${DB_USER}" \
    --set "settings.db.password=${DB_PASSWORD}" \
    --set "serverDomain=frever-api.com" \
    --namespace ${ENV} \
    --create-namespace \
    frever-copy-db \
    ./frever-copy-db