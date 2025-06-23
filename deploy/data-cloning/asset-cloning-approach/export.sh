#!/bin/bash

set -u
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

CONNECTION_STRING=$1

TEMP_DIR=${DIR}/.tmp-data

CONFIG=$(cat ${DIR}/cloning-config.json)
TABLES=($(echo ${CONFIG} | jq --raw-output '.[] | .table'))

mkdir -p ${TEMP_DIR}

for i in ${!TABLES[@]}
do
    TABLE=$(echo ${CONFIG} | jq --raw-output ".[${i}] | .table")
    SCRIPT_PATH=${DIR}/sql-scripts/${TABLE}.sql

    psql --tuples-only --no-align --file=${SCRIPT_PATH} --output=${TEMP_DIR}/${TABLE}.sql ${CONNECTION_STRING}
done;