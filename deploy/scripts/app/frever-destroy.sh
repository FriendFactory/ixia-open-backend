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


cd "${DIR}/../../application/helm-chart"

export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"

# Get the oldest namespace in the cluster. The namespaces should have semantic versioning name values.
OLDEST_NAMESPACE=$(kubectl get namespaces --sort-by="metadata.creationTimestamp" | grep "^app-" | head -n1 | awk '{print $1}')

if [[ "$OLDEST_NAMESPACE" == "" ]]; then
    echo "Failed to determine the oldest namespace."
    exit 1
fi

# Uninstall the frever app in the corresponding namespace
helm uninstall frever --namespace ${OLDEST_NAMESPACE}

if [[ $? != "0" ]]
then
    echo "Failed to uninstall helm chart."
    exit 1
else
    kubectl delete namespace ${OLDEST_NAMESPACE}
fi
if [[ $? != "0" ]]
then
    ${DIR}/../../ci/notify-slack.sh ${ENV} "Error uninstalling ${OLDEST_NAMESPACE} from ${ENV}"
    exit 1
fi
