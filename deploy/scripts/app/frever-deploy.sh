#!/bin/bash

ENV=$1
GIT_COMMIT=$(git show -s --format=%H)
GIT_BRANCH=$(git symbolic-ref --short HEAD)

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
CONFIG_DIR="${DIR}/../../configs"

ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"
FREVER_CHART="${DIR}/../../application/helm-chart/frever-app/Chart.yaml"

if [[ $ACTION_PLAN ]]; then
    FREVER_CHART="${DIR}/../../application/helm-chart/frever-app/Chart.yaml"
    RELEASE_VERSION_NUMBER=$(grep appVersion "$FREVER_CHART" | cut -d '"' -f 2)
    RELEASE=$ENV-$RELEASE_VERSION_NUMBER
    export RELEASE
    if [[ ! $RELEASE_VERSION_NUMBER =~ ^[0-9]+\.[0-9]+ ]]; then
        echo "Version number in appVersion seems broken."
        exit 1
    fi
else
    export RELEASE=${GIT_COMMIT}
fi

# shellcheck source=${ENV_FILE}
source "${ENV_FILE}"

KUBE_NAMESPACE_SUFFIX=$(grep appVersion "$FREVER_CHART" | cut -d '"' -f 2 | cut -d '.' -f1,2)
export KUBE_NAMESPACE_SUFFIX
# If namespace is set in Jenkinsfile ( job defaults ) then run against that namespace instead.
if [[ ! ${KUBE_NAMESPACE} ]]; then
    # namespace should include "major.minor" so that versions can be updated correctly when patching.
    if [[ ! $KUBE_NAMESPACE_SUFFIX  =~ ^[0-9]+\.[0-9]+ ]]; then
        echo "Failed to resolve namespace version from appVersion in $FREVER_CHART"
        exit 1
    fi
    KUBE_NAMESPACE="app-${KUBE_NAMESPACE_SUFFIX/./-}"
else
    KUBE_NAMESPACE_SUFFIX="${KUBE_NAMESPACE}"
fi

echo "Using namespace: $KUBE_NAMESPACE, using KUBE_NAMESPACE_SUFFIX: $KUBE_NAMESPACE_SUFFIX"

# If CLUSTER_NAME is not set, we set it to the APP_ENV.
if [[ ! ${CLUSTER_NAME} ]]; then
    export CLUSTER_NAME=${ENV}
fi

cd "${DIR}/../../application/helm-chart" || exit 1

KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${APP_ENV}"
export KUBECONFIG

latestNameSpaceBefore=$(kubectl get namespaces | grep app | sort -r | head -1 | awk '{print $1}')
latestApiIdBefore=$(echo "$latestNameSpaceBefore" | sed 's/app-//' | sed 's/-/./')

# Check if namespace exists, if it doesn't we create it so we can copy secrets to it.
echo "Checking if namespace (${KUBE_NAMESPACE}) exists in ${CLUSTER_NAME}."
if ! (kubectl get namespace "${KUBE_NAMESPACE}"); then
    echo "Namespace not found, going to create it."
    if (kubectl create namespace "${KUBE_NAMESPACE}"); then
        if ! ("${DIR}"/../../ssm/addSsmParameterStoreToEks.sh "${CLUSTER_NAME}" "${KUBE_NAMESPACE}"); then
            echo "Failed to copy secrets to new namespace, please investigate."
            exit 1
        else
            echo "SSm parameter store Secrets have been setup for the new namespace."
        fi
        if ! (kubectl annotate serviceaccount -n "${KUBE_NAMESPACE}" default "eks.amazonaws.com/role-arn=arn:aws:iam::722913253728:role/aws-eks-app-role-cluster-${CLUSTER_NAME}"); then
            echo "Failed to annotate ServiceAccount, please investigate."
            exit 1
        else
            echo "Service Account has been annotated."
        fi
    else
        echo "Failed to create namespace for new version."
        exit 1
    fi
fi

latestApiId=$(kubectl get namespaces | grep app | sort -r | head -1 | awk '{print $1}' | sed 's/app-//' | sed 's/-/./')

helm upgrade \
    --install \
    --set "copyDb.enabled=true" \
    --set "imageLabel=${RELEASE:-latest}" \
    --set "deployInfo.apiIdentifier=${RELEASE}" \
    --set "deployInfo.branch=${GIT_BRANCH:-$RELEASE}" \
    --set "deployInfo.commit=${GIT_COMMIT}" \
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
    --set "clusterName=${CLUSTER_NAME}" \
    --set "apiIdentifier=${KUBE_NAMESPACE_SUFFIX}" \
    --set "latestApiId=${latestApiId}" \
    --set "jaeger.host=xxxxxxxxx" \
    -f "${CONFIG_DIR}/clusters/frever-${ENV}.yml" \
    --namespace "${KUBE_NAMESPACE}" \
    --create-namespace \
    --description "Commit ${GIT_COMMIT} branch ${GIT_BRANCH}" \
    frever \
    ./frever-app

if [[ $? != "0" ]]
then
    ${DIR}/../../ci/notify-slack.sh ${ENV} "Error deploying new chart version"
    exit 1
fi

if ! [[ "$latestApiId" == "$latestApiIdBefore" ]]; then
  gitCommitInLastNamespace=$(helm -n "$latestApiIdBefore" get values frever | yq -e '.deployInfo.commit')
  echo "Namespace before: $latestNameSpaceBefore has git commit: $gitCommitInLastNamespace"
  git checkout "$gitCommitInLastNamespace"
  echo "Re-pointing latest api endpoint in namespace $latestNameSpaceBefore to $latestApiId"
  helm -n "$latestNameSpaceBefore" upgrade --set "latestApiId=$latestApiId" --reuse-values frever ./frever-app
  git checkout -
fi
