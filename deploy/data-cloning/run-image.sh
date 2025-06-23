#!/bin/bash


DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

set -u
set -e

if [[ -z ${BUILD_ID-} ]]; then
    echo "Local run"

    docker run -it --rm  \
        --env-file=.env \
        --name frever-clone \
        --volume "${DIR}/.tmp/":/root/host/ \
        frever-clone:latest ./clone.sh $1 $2
else
    echo "Jenkins run"

    docker ps -q --filter "name=frever-clone" | xargs -r docker stop
    docker run --rm  \
        --name frever-clone \
        --volume "${WORKSPACE}/.tmp/":/root/host/ \
        frever-clone:latest ./clone.sh $1 $2
fi;