#!/bin/bash


JSON=$(aws ssm get-parameters-by-path --recursive --path "/data-cloning/stage/")
PARAMETERS=($(echo "${JSON}" | jq --raw-output '.Parameters | .[].Name'))

for i in ${PARAMETERS[@]}
do
    VALUE=$(echo "${JSON}" | jq --raw-output ".Parameters[] | select(.Name == \"${i}\") | .Value")
    NEW_PATH=$(echo ${i} | sed 's/stage/stage-local/g')
    aws ssm put-parameter --name "${NEW_PATH}" --value "${VALUE}" --type String
    echo ${i} ${NEW_PATH} ${VALUE}
done;