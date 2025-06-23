#!/bin/bash

PRESET_FILE=$1

PRESET_NAME=$(jq -r '.Name' ${PRESET_FILE})
echo $PRESET_NAME

aws mediaconvert get-preset \
     --name ${PRESET_NAME} \
     --endpoint-url https://yk2lhke4b.mediaconvert.eu-central-1.amazonaws.com \
     > \
     ${PRESET_FILE}.old.json

aws mediaconvert update-preset \
     --name ${PRESET_NAME} \
     --endpoint-url https://yk2lhke4b.mediaconvert.eu-central-1.amazonaws.com \
     --cli-input-json file://${PRESET_FILE}