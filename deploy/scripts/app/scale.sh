#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

REPLICAS=$2
if [[ ${REPLICAS} == "" ]]; then
    echo "Replica count must be specified as second argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"

SVCS=("assetmanager" "auth" "notification" "asset" "main" "video" "social")

for S in "${SVCS[@]}"; do
    kubectl scale deployment/${S}-deployment --replicas ${REPLICAS} -n ${ENV}
    if [[ $? == "1" ]]
    then
        echo "Error scaling ${S}"
        exit 1
    fi
done