#!/bin/bash

set -u
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

echo ${DIR}

docker build -f "${DIR}/clone.Dockerfile" --platform=linux/amd64 -t frever-clone:latest "${DIR}"