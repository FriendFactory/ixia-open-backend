#!/bin/bash
ENV=$1
REV=$2
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi
if [[ ${REV} == "" ]]; then
    echo "Branch or commit must be specified as second argument"
    exit 1
fi

${DIR}/notify-slack.sh ${ENV} "Build completed SUCCESSFULLY"
