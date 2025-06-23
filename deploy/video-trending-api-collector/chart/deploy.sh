#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"

helm upgrade \
    --install \
    --namespace ${ENV} \
    --create-namespace \
    --description "Commit ${GIT_COMMIT} branch ${GIT_BRANCH}" \
    video-trending-api-collector \
    ./video-trending-api-collector