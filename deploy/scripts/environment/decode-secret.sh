#!/bin/bash

### Decodes base64 values in key-value pairs from .env file

INPUT=$1
OUTPUT=$2
CERT_PATH=$3
CERT_PASSWORD=$4

rm ${OUTPUT} > /dev/null

while IFS='' read -r line || [[ -n "$line" ]]; do

    PARAM_NAME=$(echo $line | awk -F": " '{print $1}')
    PARAM_VALUE_ENC=$(echo $line | awk -F": " '{print $2}')

    PARAM_VALUE=$(echo ${PARAM_VALUE_ENC} | base64 --decode)

    echo "${PARAM_NAME}=${PARAM_VALUE}" >> ${OUTPUT}

done < ${INPUT}

CERT_CONTENT=$(base64 ${CERT_PATH})

echo "auth.certificate=${CERT_CONTENT}" >> ${OUTPUT}
echo "auth.certificatePassword=${CERT_PASSWORD}" >> ${OUTPUT}