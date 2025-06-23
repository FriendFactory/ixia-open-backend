#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

GIT_COMMIT=$(git show -s --format=%H)

echo ${GIT_BRANCH}
echo ${GIT_COMMIT}

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"



export RELEASE=${GIT_COMMIT} && \
    ${DIR}/frever-build.sh ${ENV} ${GIT_COMMIT} && \
    ${DIR}/frever-deploy.sh ${ENV} ${GIT_COMMIT} && \
    kubectl rollout restart deployment -n ${ENV}
