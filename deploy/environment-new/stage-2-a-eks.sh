#!/bin/bash

################################################################################################
## Creates EKS cluster, node groups, security roles etc.
################################################################################################

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

OPERATION=$2
OPERATION=${OPERATION:-plan}

AUTO_APPROVE="-auto-approve"
if [[ ${OPERATION} == "plan" ]]; then
    AUTO_APPROVE=""
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

export TF_DATA_DIR=${DIR}/stage-2-a-eks/.terraform
export TF_WORKSPACE=${ENV}

mkdir ${TF_DATA_DIR}

cd ${DIR}/stage-2-a-eks


env TF_DATA_DIR=${TF_DATA_DIR} TF_WORKSPACE=${ENV} \
    terraform init

if [[ $? == "1" ]]; then
    echo "Error init terraform"
    exit 1
fi

env TF_DATA_DIR=${TF_DATA_DIR} TF_WORKSPACE=${ENV} TF_VAR_env=${ENV} \
    terraform ${OPERATION} ${AUTO_APPROVE}

if [[ $? == "1" ]]; then
    echo "Error creating environment"
    exit 1
fi


REGION=$(terraform output -raw region)

if [[ ${OPERATION} == "apply" ]]; then
    mkdir ${DIR}/.configs
    rm ${DIR}/.configs/kubeconfig_${ENV}

    aws eks update-kubeconfig --region ${REGION} --name ${ENV} --kubeconfig ${DIR}/.configs/kubeconfig_${ENV}
    aws eks update-kubeconfig --region ${REGION} --name ${ENV}

    ## Update
    mkdir ${DIR}/.configs/${ENV}
    ENV_FILE=${DIR}/.configs/${ENV}/eks.env
    rm ${ENV_FILE}

    echo "APP_NODE_GROUP_ROLE_ARN=$(terraform output -raw app_node_group_role_arn)" >> ${ENV_FILE}
    echo "REGION=$(terraform output -raw region)" >> ${ENV_FILE}

    ### Copy cubeconfig
    cp ${DIR}/.configs/kubeconfig_${ENV} ${DIR}/../environment/frever/kubeconfig_${ENV}

fi
