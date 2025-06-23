#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Upload tests to s3
aws s3 cp ${DIR}/tests/ s3://frever-load-test/tests/ \
     --recursive \
     --exclude "*" \
     --include "*.jmx" --include "*.csv" --include "*.Files/*.*"

# Create job for running load test in k8s
kubectl config use-context $(kubectl config get-contexts | grep "${ENV}" | awk '{ print $2 }')

kubectl apply -f ${DIR}/load-test-controller-job.yaml

kubectl wait --for=condition=complete job/load-test-controller --timeout=3600s

kubectl logs job.batch/load-test-controller

kubectl delete -f ${DIR}/load-test-controller-job.yaml

#
LAST_REPORT=$(aws s3 ls s3://frever-load-test/reports/ | sort | tail -n 1 | awk '{ print $2 }')

# aws s3 cp --recursive s3://frever-load-test/reports/${LAST_REPORT} ./results/${LAST_REPORT}
open https://frever-load-test.s3.eu-central-1.amazonaws.com/reports/${LAST_REPORT}index.html