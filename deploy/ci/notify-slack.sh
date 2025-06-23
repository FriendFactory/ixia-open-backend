#!/bin/bash

### Posts message in slack channel
ENV=$1
TEXT=$2

GIT_COMMIT=$(git show -s --format=%H)
GIT_BRANCH=$(git symbolic-ref --short HEAD)

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

if [[ ${TEXT} == "" ]]; then
    echo "Slack message must be specified as second argument"
    exit 1
fi

HEADER_MRKDWN="Frever-Backend: *${ENV}* build <${BUILD_URL}|#${BUILD_NUMBER}>"
GIT_INFO_MRKDWN="Commit _${GIT_COMMIT}_ (branch _${GIT_BRANCH}_)"

BLOCKS="[{ \"type\": \"divider\" }"
BLOCKS="${BLOCKS}, { \"type\": \"section\", \"text\": { \"type\": \"mrkdwn\", \"text\": \"${HEADER_MRKDWN}\" } }"
BLOCKS="${BLOCKS}, { \"type\": \"section\", \"text\": { \"type\": \"mrkdwn\", \"text\": \"${GIT_INFO_MRKDWN}\" } }"
BLOCKS="${BLOCKS}, { \"type\": \"section\", \"text\": { \"type\": \"mrkdwn\", \"text\": \"${TEXT}\" } }"
BLOCKS="${BLOCKS} ]"

echo ${BLOCKS}

MESSAGE="Backend: build ${BUILD_NUMBER} of ${APP_ENV}: deploying commit ${GIT_COMMIT} branch ${GIT_BRANCH}"
curl -X POST \
    -H 'Content-type: application/json' \
    --data "{\"text\": \"${MESSAGE}\", \"blocks\": ${BLOCKS} }" \
    xxxxxxxxx
