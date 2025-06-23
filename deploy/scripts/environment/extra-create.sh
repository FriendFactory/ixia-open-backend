#!/bin/bash
ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

kubectl config use-context $( kubectl config get-contexts | grep "${ENV}" | awk '{ print $2 }')

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
ENV_FILE="${DIR}/../../.deploy/${ENV}/.env"

export $(cat ${ENV_FILE} | xargs)

cd "${DIR}/../../environment/extra"

terraform workspace select ${ENV}

if [[ $? == "1" ]]
then
    terraform workspace new ${ENV}
fi

terraform init

if [[ $? == "1" ]]
then
    echo "Error init terraform"
    exit 1
fi

env TF_VAR_env=${ENV} \
    TF_VAR_eks_cluster=${ENV} \
    TF_VAR_cluster_worker_iam_role_name=${CLUSTER_WORKER_IAM_ROLE_NAME} \
    terraform apply -auto-approve

if [[ $? == "1" ]]
then
    echo "Error creating environment"
    exit 1
fi

# Deploy metrics server
# https://docs.aws.amazon.com/eks/latest/userguide/metrics-server.html
# https://github.com/kubernetes-sigs/metrics-server
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# CW Agent for prometheus
# https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/ContainerInsights-Prometheus-Setup.html
# cat ../application/k8s/cwagent-prometheus.yaml | sed "s/<<REGION>>/${REGION}/" > ../.deploy/cwagent-prometheus.yaml
# kubectl apply -f ../.deploy/cwagent-prometheus.yaml