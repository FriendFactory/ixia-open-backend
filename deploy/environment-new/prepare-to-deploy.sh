#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi


DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

export $(cat ${DIR}/.configs/${ENV}/vpc.env | xargs)
export $(cat ${DIR}/.configs/${ENV}/eks.env | xargs)
export $(cat ${DIR}/.configs/${ENV}/frever.env | xargs)

### Copy cubeconfig
cp ${DIR}/.configs/kubeconfig_${ENV} ${DIR}/../environment/frever/kubeconfig_${ENV}

### Create .env file
ENV_FILE=${DIR}/../.deploy/${ENV}/.env

rm ${ENV_FILE}

echo "APPSERVICE_REPOSITORY_URL=${APPSERVICE_REPOSITORY_URL}" >> ${ENV_FILE}
echo "REGION=${REGION}" >> ${ENV_FILE}
echo "SSL_CERTIFICATE_ARN=${SSL_CERTIFICATE_ARN}" >> ${ENV_FILE}
echo "CDN_DOMAIN=${ENV}.xxxxxxxxx" >> ${ENV_FILE}
echo "VIDEO_CONVERSION_JOB_CREATION_QUEUE=${VIDEO_CONVERSION_JOB_CREATION_QUEUE}" >> ${ENV_FILE}
echo "REDIS_HOST=${REDIS_HOST}" >> ${ENV_FILE}
echo "CLOUDFRONT_DISTRIBUTION_ID=ABCD123" >> ${ENV_FILE}