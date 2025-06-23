#!/bin/bash

TEST_NAME=$1
if [[ ${TEST_NAME} == "" ]]; then
    echo "Test name must be specified as first argument"
    exit 1
fi


# Execute JMeter command
set -e
freeMem=`awk '/MemFree/ { print int($2/1024) }' /proc/meminfo`
s=$(($freeMem/10*8))
x=$(($freeMem/10*8))
n=$(($freeMem/10*2))
export JVM_ARGS="-Xmn${n}m -Xms${s}m -Xmx${x}m"

echo "START Running Jmeter on `date`"
echo "JVM_ARGS=${JVM_ARGS}"

echo Test parameters: ${@:2}

# Run tests
jmeter -n \
    -t /tests/${TEST_NAME} \
    -l /reports/${TEST_NAME}-report.jtl \
    -e \
    -o /reports/ \
    ${@:2}

# Upload report
# REPORT_DIR=$(date '+%Y-%m-%d--%H-%M-%S')-Local-${TEST_NAME}
# aws s3 cp --recursive /reports s3://${BUCKET}/reports/${REPORT_DIR}