#!/bin/bash

PATH_TO_LOGS=$1

echo ${PATH_TO_LOGS}
if [[ ${PATH_TO_LOGS} == "" ]]; then
    echo "s3:// path to logs must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# aws s3 cp $PATH_TO_LOGS ${DIR}/.data/raw-logs --recursive

FULL_LOG_FILE=${DIR}/.data/vpc-full.log

rm -f ${FULL_LOG_FILE}

find ${DIR}/.data/raw-logs -name "*.gz" \
     | xargs -I {} sh -c "gzip -d -k -c {} | tail -n +2" | grep -v NODATA | grep -v SKIPDATA >> ${FULL_LOG_FILE}

echo "Complete combining logs into single file"