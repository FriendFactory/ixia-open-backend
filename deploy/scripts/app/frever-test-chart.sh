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

cd "${DIR}/../../application/helm-chart"

export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"

helm install \
    --dry-run \
    --set "copyDb.enabled=true" \
    --set "imageLabel=${RELEASE:-latest}" \
    --set "deployInfo.branch=${GIT_BRANCH:-$RELEASE}" \
    --set "deployInfo.commit=${RELEASE}" \
    --set "deployInfo.deployedBy=$(whoami)" \
    --set "deployInfo.deployedAt=$(date)" \
    --set "deployInfo.deployedFrom=$(hostname)" \
    --set "repository=${APPSERVICE_REPOSITORY_URL}" \
    --set "sslCertificateArn=${SSL_CERTIFICATE_ARN}" \
    --set "settings.cdn.host=${CDN_DOMAIN}" \
    --set "settings.cdn.distributionId=${CLOUDFRONT_DISTRIBUTION_ID}" \
    --set "settings.videoConversion.createJobQueue=${VIDEO_CONVERSION_JOB_CREATION_QUEUE}" \
    --set "settings.redis.host=${REDIS_HOST}" \
    --set "serverDomain=frever-api.com" \
    --set "jaeger.host=xxxxxxxxx" \
    -f ${CONFIG_DIR}/clusters/frever-${ENV}.yml \
    --namespace ${ENV} \
    --create-namespace \
    --description "Commit ${GIT_COMMIT} branch ${GIT_BRANCH}" \
    frever \
    ./frever-app

# helm template \
#     --debug \
#     --set "copyDb.enabled=true" \
#     --set "imageLabel=${RELEASE:-latest}" \
#     --set "deployInfo.branch=${GIT_BRANCH:-$RELEASE}" \
#     --set "deployInfo.commit=${RELEASE}" \
#     --set "deployInfo.deployedBy=$(whoami)" \
#     --set "deployInfo.deployedAt=$(date)" \
#     --set "deployInfo.deployedFrom=$(hostname)" \
#     --set "repository=${APPSERVICE_REPOSITORY_URL}" \
#     --set "sslCertificateArn=${SSL_CERTIFICATE_ARN}" \
#     --set "settings.cdn.host=${CDN_DOMAIN}" \
#     --set "settings.cdn.distributionId=${CLOUDFRONT_DISTRIBUTION_ID}" \
#     --set "settings.videoConversion.createJobQueue=${VIDEO_CONVERSION_JOB_CREATION_QUEUE}" \
#     --set "settings.redis.host=${REDIS_HOST}" \
#     --set "serverDomain=frever-api.com" \
#     --set "jaeger.host=xxxxxxxxx" \
#     -f ${CONFIG_DIR}/clusters/frever-${ENV}.yml \
#     --namespace ${ENV} \
#     --create-namespace \
#     --description "Commit ${GIT_COMMIT} branch ${GIT_BRANCH}" \
#     frever \
#     ./frever-app