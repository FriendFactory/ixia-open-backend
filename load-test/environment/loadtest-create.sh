#!/bin/bash

# Creates new environment via terraform
# and outputs .env file required for building and pushing app

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "${DIR}/load-test"

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

env TF_VAR_env=${ENV} terraform apply -auto-approve

if [[ $? == "1" ]]
    then
        echo "Error creating environment"
        exit 1
    fi

aws eks \
     --profile friendsfactory \
     --region $(terraform output -raw region) \
     update-kubeconfig --name $(terraform output -raw cluster_name)

TMP_DIR="${DIR}/../.deploy/${ENV}"
mkdir ${TMP_DIR}

ENV_FILE="${TMP_DIR}/.env"

rm -f ${ENV_FILE}

echo "CLUSTER_ENDPOINT=$(terraform output -raw cluster_endpoint)" >> ${ENV_FILE}
echo "CLUSTER_NAME=$(terraform output -raw cluster_name)" >> ${ENV_FILE}
echo "CLUSTER_WORKER_IAM_ROLE_NAME=$(terraform output -raw cluster_worker_iam_role_name)" >> ${ENV_FILE}
echo "REGION=$(terraform output -raw region)" >> ${ENV_FILE}

echo "Environment creation completed"