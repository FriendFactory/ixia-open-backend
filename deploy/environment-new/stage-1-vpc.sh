#!/bin/bash

################################################################################################
## Creates common infrastructure for environment: VPC, subnets, roles etc.
##
################################################################################################

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

SSH_KEY_PAIR_NAME=$2

if [[ ${SSH_KEY_PAIR_NAME} == "" ]]; then
    echo "SSH key pair name must be specified as second argument"
    exit 1
fi

OPERATION=$3
OPERATION=${OPERATION:-plan}

AUTO_APPROVE="-auto-approve"
if [[ ${OPERATION} == "plan" ]]; then
    AUTO_APPROVE=""
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

export TF_DATA_DIR=${DIR}/stage-1-vpc/.terraform
export TF_WORKSPACE=${ENV}

mkdir ${TF_DATA_DIR}

cd ${DIR}/stage-1-vpc


env TF_DATA_DIR=${TF_DATA_DIR} TF_WORKSPACE=${ENV} \
    terraform init

if [[ $? == "1" ]]; then
    echo "Error init terraform"
    exit 1
fi

env TF_DATA_DIR=${TF_DATA_DIR} TF_WORKSPACE=${ENV} TF_VAR_env=${ENV} TF_VAR_ssh_key_pair_name=${SSH_KEY_PAIR_NAME} \
    terraform ${OPERATION} ${AUTO_APPROVE}

if [[ $? == "1" ]]; then
    echo "Error creating environment"
    exit 1
fi

if [[ ${OPERATION} == "apply" ]]; then
    mkdir ${DIR}/.configs

    ## Update
    mkdir ${DIR}/.configs/${ENV}
    ENV_FILE=${DIR}/.configs/${ENV}/vpc.env
    rm ${ENV_FILE}

    echo "ELASTICACHE_GROUP_NAME=$(terraform output -raw elasticache_subnet_group)" >> ${ENV_FILE}
    echo "REGION=$(terraform output -raw region)" >> ${ENV_FILE}
    echo "VPC_ID=$(terraform output -raw vpc_id)" >> ${ENV_FILE}
fi
