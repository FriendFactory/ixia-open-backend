#!/bin/bash

# Creates new environment via terraform
# and outputs .env file required for building and pushing app

ENV=$1
S3_BUCKET=$2

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

if [[ ${S3_BUCKET} == "" ]]; then
    echo "S3 bucket must be specified as second argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "${DIR}/../../environment/frever"

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

env TF_VAR_env=${ENV} TF_VAR_s3_bucket_name=${S3_BUCKET} terraform apply -auto-approve
#env TF_VAR_env=${ENV} TF_VAR_s3_bucket_name=${S3_BUCKET} terraform plan

if [[ $? == "1" ]]
    then
        echo "Error creating environment"
        exit 1
    fi



aws eks  --profile friendsfactory --region $(terraform output -raw region) update-kubeconfig --name $(terraform output -raw cluster_name)

TMP_DIR="${DIR}/../../.deploy/${ENV}"
mkdir ${TMP_DIR}

ENV_FILE="${TMP_DIR}/.env"

rm -f ${ENV_FILE}

echo "CLUSTER_ENDPOINT=$(terraform output -raw cluster_endpoint)" >> ${ENV_FILE}
echo "CLUSTER_NAME=$(terraform output -raw cluster_name)" >> ${ENV_FILE}
echo "CLUSTER_WORKER_IAM_ROLE_NAME=$(terraform output -raw cluster_worker_iam_role_name)" >> ${ENV_FILE}
echo "AUTH_DOCKER_REPOSITORY_URL=$(terraform output -raw auth_docker_repository_url)" >> ${ENV_FILE}
echo "NOTIFICATION_DOCKER_REPOSITORY_URL=$(terraform output -raw notification_docker_repository_url)" >> ${ENV_FILE}
echo "ASSET_DOCKER_REPOSITORY_URL=$(terraform output -raw asset_docker_repository_url)" >> ${ENV_FILE}
echo "MAIN_DOCKER_REPOSITORY_URL=$(terraform output -raw main_docker_repository_url)" >> ${ENV_FILE}
echo "ASSETMANAGER_DOCKER_REPOSITORY_URL=$(terraform output -raw assetmanager_docker_repository_url)" >> ${ENV_FILE}
echo "VIDEO_DOCKER_REPOSITORY_URL=$(terraform output -raw video_docker_repository_url)" >> ${ENV_FILE}
echo "SOCIAL_DOCKER_REPOSITORY_URL=$(terraform output -raw social_docker_repository_url)" >> ${ENV_FILE}
echo "APPSERVICE_REPOSITORY_URL=$(terraform output -raw appservice_repository_url)" >> ${ENV_FILE}
echo "REGION=$(terraform output -raw region)" >> ${ENV_FILE}
echo "SSL_CERTIFICATE_ARN=$(terraform output -raw ssl_certificate_arn)" >> ${ENV_FILE}
echo "CDN_DOMAIN=$(terraform output -raw cdn_domain)" >> ${ENV_FILE}
echo "CLOUDFRONT_DISTRIBUTION_ID=$(terraform output -raw cloudfront_distribution_id)" >> ${ENV_FILE}
echo "REDIS_HOST=$(terraform output -raw redis_host)" >> ${ENV_FILE}
echo "DB_SERVER=$(terraform output -raw db_address)" >> ${ENV_FILE}
echo "DB_USER=$(terraform output -raw db_user_name)" >> ${ENV_FILE}
echo "DB_PASSWORD=$(terraform output -raw db_password)" >> ${ENV_FILE}
echo "VIDEO_CONVERSION_JOB_CREATION_QUEUE=$(terraform output -raw video-conversion-job-creation-sqs-queue)" >> ${ENV_FILE}
echo "VIDEO_CONVERSION_JOB_CREATION_LAMBDA=$(terraform output -raw video-conversion-job-creator-lambda)" >> ${ENV_FILE}

echo "Environment creation completed"