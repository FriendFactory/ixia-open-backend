#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

export KUBECONFIG="${DIR}/.configs/kubeconfig_${ENV}"

#####################
echo "Installing Jaeger..."
helm upgrade \
    --install \
    --set "domain=frever-api.com" \
    --set "sslCertificateArn=xxxxxxxxx" \
    --namespace jaeger \
    --create-namespace \
    ${ENV} \
    ${DIR}/../application/helm-chart/jaeger

