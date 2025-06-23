#!/usr/bin/env bash

set -euo pipefail

CURRENT_CONTEXT=$(kubectl config current-context | awk -F '/' '{print $2}' | sed -e 's/-eks-cluster//')
CLUSTER="${1:-$(kubectl config current-context | awk -F '/' '{print $2}')}"
NAMESPACE="${2:-$(kubectl config view --minify -o jsonpath='{..namespace}')}"

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

if [[ ${CLUSTER} == "" ]]; then
    echo "CLUSTER name must be specified as first argument"
    exit 1
fi

if [[ ${NAMESPACE} == "" ]]; then
    echo "NAMESPACE name must be specified as second argument"
    exit 1
fi

if [[ ${CURRENT_CONTEXT} != "${CLUSTER}" ]]; then
    echo "Current context is ${CURRENT_CONTEXT}, which is not set to ${CLUSTER}. Please set the current context to ${CLUSTER} before running this script."
    exit 1
fi

echo "Will add SecretProviderClass to EKS cluster: '${CLUSTER}' namespace: '${NAMESPACE}'."

sed -E "s/NAMESPACE_NAME/$NAMESPACE/" "$DIR"/SecretProviderClass.yaml | sed -E "s/CLUSTER_NAME/$CLUSTER/" | kubectl apply -f -
