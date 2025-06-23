#!/bin/bash
ENV=$1
BUCKET=$2
PREFIX=$3

declare -A PORT_MAPPING
PORT_MAPPING[dev-1]=15432
PORT_MAPPING[dev-2]=15433
PORT_MAPPING[content-test]=15434
PORT_MAPPING[content-stage]=15435
PORT_MAPPING[content-prod]=15436
PORT_MAPPING[dev]=15437
PORT_MAPPING[test]=15438
PORT_MAPPING[stage]=15439

######### INTRO
command -v aws >/dev/null 2>&1 || { echo >&2 "Need AWS CLI to run the script. Aborting."; exit 1; }
command -v pg_dump >/dev/null 2>&1 || { echo >&2 "Need pg_dump to run the script. Aborting."; exit 1; }

if [[ ${ENV} == "" ]]; then
    echo "Please provide ENV name as first argument"
    exit 1
fi

if ! [[ ${PORT_MAPPING[$ENV]+_} ]]; then
    echo "ENV should be one of 'dev-1', 'dev-2', 'content-test', 'content-stage', 'content-prod', 'dev', 'test', 'stage'"
    exit 1
fi

if [[ ${BUCKET} == "" ]]; then
    echo "Please provide S3 bucket name as second argument"
    exit 1
fi

if [[ ${PREFIX} == "" ]]; then
    PREFIX=$(date +"%Y-%m-%d_%H-%M")
fi

echo "Backing up databases used in $ENV to bucket $BUCKET"

dbs=(main auth video)

namespace=$(kubectl get namespaces | grep app | sort -r | head -1 | awk '{print $1}')

for db in "${dbs[@]}"
do
    secret=$(kubectl get --namespace "$namespace" secrets ssm-secrets -o jsonpath="{.data.cs\.$db}" | base64 --decode | awk -F ';' '{print $1, $2, $3, $4}')
    IFS=' '
    declare -A connection
    for pair in ${secret}
    do
        key=$(awk -F '=' '{print $1}' <<< "${pair}")
        value=$(awk -F '=' '{print $2}' <<< "${pair}")
        connection[${key}]=${value}
    done

    ssh -M -S /var/lib/jenkins/.ssh/$ENV-ctrl-socket -fNT -i /var/lib/jenkins/.ssh/jenkins.pem -L ${PORT_MAPPING[$ENV]}:${connection[Host]}:5432 jenkins@ssh-$ENV.frever-api.com

    echo "Backing up to $(pwd)/$db-$ENV.sql"
    PGPASSWORD=${connection[Password]} pg_dump -h localhost -p ${PORT_MAPPING[$ENV]} -U ${connection[Username]} --clean --if-exists --file=$db-$ENV.sql ${connection[Database]}
    echo "Backed up to $(pwd)/$db-$ENV.sql"

    aws --region eu-central-1 s3 cp $db-$ENV.sql s3://${BUCKET}/${PREFIX}/
    echo "Uploaded to S3 path s3://${BUCKET}/${PREFIX}/$db-$ENV.sql"
    rm $db-$ENV.sql

    ssh -S /var/lib/jenkins/.ssh/$ENV-ctrl-socket -O exit ec2-user@ssh-$ENV.frever-api.com
    while [ -e /var/lib/jenkins/.ssh/$ENV-ctrl-socket ]; do sleep 0.1; done
done

