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


helm uninstall \
    --namespace ${ENV} \
    frever-copy-db \
    ./frever-copy-db