#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"


REVISION=$2

if [[ ${REVISION} == "" ]]; then
    echo "Listing all versions:"
    helm history frever -n ${ENV}
    exit 0
else 
    echo "Rolling back to ${REVISION}: "
    helm rollback frever ${REVISION} -n ${ENV}
    exit 0
fi