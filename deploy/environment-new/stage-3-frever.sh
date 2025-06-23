#!/bin/bash

################################################################################################
## Creates EKS cluster, node groups, security roles etc.
################################################################################################

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

S3_BUCKET=$2

if [[ ${S3_BUCKET} == "" ]]; then
    echo "S3 bucket must be specified as second argument"
    exit 1
fi


OPERATION=$3
OPERATION=${OPERATION:-plan}

AUTO_APPROVE="-auto-approve"
if [[ ${OPERATION} == "plan" ]]; then
    AUTO_APPROVE=""
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

export TF_DATA_DIR=${DIR}/stage-3-frever/.terraform
export TF_WORKSPACE=${ENV}

export $(cat ${DIR}/.configs/${ENV}/eks.env | xargs)
export $(cat ${DIR}/.configs/${ENV}/vpc.env | xargs)

mkdir ${TF_DATA_DIR}

cd ${DIR}/stage-3-frever


env TF_DATA_DIR=${TF_DATA_DIR} TF_WORKSPACE=${ENV} \
    terraform init

if [[ $? == "1" ]]; then
    echo "Error init terraform"
    exit 1
fi

env TF_DATA_DIR=${TF_DATA_DIR} TF_WORKSPACE=${ENV} \
    TF_VAR_env=${ENV} \
    TF_VAR_s3_bucket_name=${S3_BUCKET} \
    terraform ${OPERATION} ${AUTO_APPROVE}

if [[ $? == "1" ]]; then
    echo "Error creating environment"
    exit 1
fi


if [[ ${OPERATION} == "apply" ]]; then
   mkdir ${DIR}/.configs

   ## Update
   mkdir ${DIR}/.configs/${ENV}
   ENV_FILE=${DIR}/.configs/${ENV}/frever.env
   rm ${ENV_FILE}

   echo "APPSERVICE_REPOSITORY_URL=$(terraform output -raw docker_repository_url)" >> ${ENV_FILE}
   echo "SSL_CERTIFICATE_ARN=$(terraform output -raw ssl_certificate_arn)" >> ${ENV_FILE}
   echo "REDIS_HOST=$(terraform output -raw redis_host)" >> ${ENV_FILE}
   echo "CDN_DOMAIN=$(terraform output -raw cdn_domain)" >> ${ENV_FILE}
   echo "ASSET_COPYING_SQS_QUEUE=$(terraform output -raw asset_copying_sqs_queue)" >> ${ENV_FILE}
   echo "VIDEO_CONVERSION_JOB_CREATION_QUEUE=$(terraform output -raw video_conversion_job_creation_sqs_queue)" >> ${ENV_FILE}
   echo "VIDEO_CONVERSION_JOB_COMPLETED_QUEUE=$(terraform output -raw video_conversion_job_completed_sqs_queue)" >> ${ENV_FILE}
   echo "MEDIA_CONVERTER_QUEUE=$(terraform output -raw media_converter_queue)" >> ${ENV_FILE}
fi
