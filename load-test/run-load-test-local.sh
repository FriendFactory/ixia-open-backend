#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

rm -rf reports
mkdir reports

cd docker

docker build \
    --tag jmeter-runner-local \
    --file ./Controller-Local.Dockerfile \
    .

cd ..

docker run \
  --name jmeter-runner-local \
  --rm \
   -v ${DIR}/tests:/tests \
   -v ${DIR}/reports:/reports \
    jmeter-runner-local AllForOldApi.jmx -Jthreads=50 -Jiterations=1 -Jenv=content-stage

if [[ ${exit_code} -ne 0 ]];
then
    exit ${exit_code};
fi

open ${DIR}/reports/index.html