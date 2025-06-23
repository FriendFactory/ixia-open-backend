#!/bin/bash

ENV=$1
GIT_COMMIT=$(git show -s --format=%H)
GIT_BRANCH=$(git symbolic-ref --short HEAD)
RELEASE=${GIT_COMMIT}


if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi


DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
CONFIG_DIR="${DIR}/../../configs"


ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"

export $(cat ${ENV_FILE} | xargs)

# If CLUSTER_NAME is not set, we set it to the namespae.
if [[ ! ${CLUSTER_NAME} ]]; then
    export CLUSTER_NAME=${ENV}
fi

FREVER_CHART="${DIR}/../../application/helm-chart/frever-app/Chart.yaml"
cd "${DIR}/../../application/helm-chart"

export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"
if [[ ! ${KUBE_NAMESPACE} ]]; then
    # namespace should be "major.minor" so that versions can be updated correctly when patching.
    KUBE_NAMESPACE_SUFFIX=$(grep appVersion $FREVER_CHART | cut -d '"' -f 2 | cut -d '.' -f1,2)
    if [[ ! $KUBE_NAMESPACE_SUFFIX  =~ ^[0-9]+\.[0-9]+ ]]; then
        echo "Failed to resolve namespace version from appVersion in $FREVER_CHART"
        exit 1
    fi
    KUBE_NAMESPACE="app-${KUBE_NAMESPACE_SUFFIX/./-}"
fi

echo "Running rollout restart on all deployments in ${KUBE_NAMESPACE}"

# Uninstall the frever app in the corresponding namespace
kubectl rollout restart deployment -n ${KUBE_NAMESPACE}
if [[ $? != "0" ]]
then
    ${DIR}/../../ci/notify-slack.sh ${ENV} "Error running rollout restart deployment for namespace ${KUBE_NAMESPACE} in cluster ${CLUSTER_NAME}"
    exit 1
fi
