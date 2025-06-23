#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "${DIR}/../../environment/extra"

kubectl config use-context $( kubectl config get-contexts | grep "${ENV}" | awk '{ print $3 }')

kubectl delete -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

terraform workspace select ${ENV}

if [[ $? == "1" ]]
then
    echo "Error: environment ${ENV} is not found"
    exit 1
fi

env TF_VAR_env=${ENV} \
    TF_VAR_eks_cluster=${ENV} \
    TF_VAR_cluster_worker_iam_role_name=${CLUSTER_WORKER_IAM_ROLE_NAME} \
    terraform destroy -auto-approve -refresh=true