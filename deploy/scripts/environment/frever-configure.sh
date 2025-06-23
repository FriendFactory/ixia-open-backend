#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

SECRET_FILE=$2

if [[ ${SECRET_FILE} == "" ]]; then
    echo "Secret file in .env format must be specified as second argument"
    exit 1
fi


DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"
export $(cat ${ENV_FILE} | xargs)
export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"

kubectl delete secret/secret --namespace ${ENV}

kubectl create secret generic secret \
 --namespace ${ENV} \
 --from-env-file ${SECRET_FILE}