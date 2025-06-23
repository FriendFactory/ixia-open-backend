#!/bin/bash
ENV=$1
REV=$2

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

cd "${DIR}/../.." || exit 1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi
if [[ ${REV} == "" ]]; then
    echo "Branch or commit must be specified as second argument"
    exit 1
fi

git config user.name "xxxxxxxxx"
git config user.email "xxxxxxxxx"
git config pull.ff no

git pull --rebase

FREVER_CHART="${DIR}/../application/helm-chart/frever-app/Chart.yaml"

KUBE_NAMESPACE_SUFFIX=$(grep appVersion "$FREVER_CHART" | cut -d '"' -f 2 | cut -d '.' -f1,2)
if [[ ! $KUBE_NAMESPACE_SUFFIX  =~ ^[0-9]+\.[0-9]+ ]]; then
    echo "Failed to resolve namespace version from appVersion in $FREVER_CHART"
    exit 1
fi
KUBE_NAMESPACE="app-${KUBE_NAMESPACE_SUFFIX/./-}"
export KUBECONFIG="${DIR}/../environment/frever/kubeconfig_${ENV}"
CURRENT_RELEASE_GIT_COMMIT=$(helm -n $KUBE_NAMESPACE get values frever | yq -e '.deployInfo.commit')
echo "$REV: $REV, KUBE_NAMESPACE: $KUBE_NAMESPACE, CURRENT_RELEASE_GIT_COMMIT: $CURRENT_RELEASE_GIT_COMMIT"
GIT_LOGS=$(git log --oneline $CURRENT_RELEASE_GIT_COMMIT..$REV)
if echo "$GIT_LOGS" | grep -q "\[BREAKING\]"; then
    exit 151
elif echo "$GIT_LOGS" | grep -q "\[MINOR\]"; then
    exit 152
else
    exit 153
fi