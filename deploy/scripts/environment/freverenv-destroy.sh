#!/bin/bash

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

kubectl config use-context $( kubectl config get-contexts | grep "${ENV}" | awk '{ print $3 }')

terraform workspace select ${ENV}

if [[ $? == "1" ]]
then
    echo "Error: environment ${ENV} is not found"
    exit 1
fi


env TF_VAR_env=${ENV} TF_VAR_s3_bucket_name=${S3_BUCKET} terraform destroy -auto-approve -refresh=true