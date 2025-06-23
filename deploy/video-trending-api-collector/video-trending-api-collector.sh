#!/bin/bash
API=${FREVER_API:-https://content-stage.frever-api.com/auth}
S3_UPLOAD_PATH=${UPLOAD_PATH:-xxxxxxxxx}
LOGIN=xxxxxxxxx
PASSWORD=xxxxxxxxx
CLIENT_SECRET=xxxxxxxxx
export AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID:-xxxxxxxxx}
export AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY:-xxxxxxxxx}

echo "Authorizing..."

AUTH_RESPONSE=$(
    curl -X POST \
        --data-urlencode "grant_type=password" \
        --data-urlencode "username=${LOGIN}" \
        --data-urlencode "password=${PASSWORD}" \
        --data-urlencode "client_id=Server" \
        --data-urlencode "scope=friends_factory.creators_api offline_access" \
        --data-urlencode "client_secret=${CLIENT_SECRET}" \
        --silent \
        ${API}/connect/token)

AUTH_TOKEN=$(echo ${AUTH_RESPONSE} | jq -r '.access_token')
MAIN_SERVER_URL=$(echo ${AUTH_RESPONSE} | jq -r '.server_url')

if [ "$AUTH_TOKEN" == "null" ]; then
    echo "Authorization failed, terminating..."
    exit 1
fi

echo "Authorization passed, loading trending data..."

TEMPLATE_TRENDING_STATUS=$( 
    curl -H "Authorization: Bearer ${AUTH_TOKEN}" \
        -o response.json \
        -w "%{http_code}" \
        --silent \
        "${MAIN_SERVER_URL}api/Template/trending")

if [ $TEMPLATE_TRENDING_STATUS != "200" ]; then 
    echo "Failed to load trending templates"
    exit 2
fi

echo "Templates loaded..."

UPLOAD_FOLDER=$(date +"%Y-%m-%dT%H:%M:%S%z")

aws s3 cp response.json ${S3_UPLOAD_PATH}/data-${UPLOAD_FOLDER}.json

if [[ $? != 0 ]]; then 
    echo "Uploading failed"
    exit 3
fi

echo "Uploaded successfully!"