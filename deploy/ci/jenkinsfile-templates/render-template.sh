#!/bin/bash

### Renders Jenkinsfile for each environment 
### by substituting %VAR% patterns with values VAR=val
### from corresponding .env.Environment file


for f in .env.*; do 
    ENV=$(echo $f | sed 's/.env.//g')

    echo "Rendering Jenkinsfile template for $ENV:"

    ENV_FILE=".env.${ENV}"
    export $(cat ${ENV_FILE} | xargs)
    TEMPLATE=$(cat Jenkinsfile.template)

    while IFS='' read -r line || [[ -n "$line" ]]; do
       PARAM_NAME=$(echo $line | awk -F"=" '{print $1}')
       PARAM_VALUE=$(echo $line | awk -F"=" '{print $2}')

       PARAM_VALUE=$(sed 's/\//\\\//g' <<< "$PARAM_VALUE")

       echo "    Name=${PARAM_NAME}, Value=${PARAM_VALUE}"

       TEMPLATE=$(echo "$TEMPLATE" | sed "s/%${PARAM_NAME}%/${PARAM_VALUE}/g")
    
    done < ${ENV_FILE}

    echo "$TEMPLATE" > "../envs/${ENV}/Jenkinsfile"
done