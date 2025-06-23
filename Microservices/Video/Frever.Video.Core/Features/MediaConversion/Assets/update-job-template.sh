#!/bin/bash

TEMPLATE=$1

aws mediaconvert update-job-template \
    --name ${TEMPLATE} \
    --endpoint-url https://yk2lhke4b.mediaconvert.eu-central-1.amazonaws.com \
    --cli-input-json file://aws-mediaconvert-job-template.json