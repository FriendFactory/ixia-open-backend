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

# Cleanup
BUCKET=frever-load-test

mkdir /reports
mkdir /logs
rm -rf /tests
rm -rf /reports
rm -rf /logs
aws s3 cp --recursive s3://${BUCKET}/tests /tests

# Collect IP address of worker pods
TMP=$(kubectl get pods  -l app=load-test-worker -o jsonpath="{range .items[*].status}{.podIP}{','}{end}")
REMOTE_WORKERS=${TMP%?}

echo Test parameters: ${@:2}
TITLE=${*:2}

# Run tests
jmeter -n \
    -R${REMOTE_WORKERS} \
    -t /tests/${TEST_NAME} \
    -l /reports/${TEST_NAME}-report.jtl \
    -j /logs/jmeter.log \
    -e \
    -o /reports/ \
    -Jjmeter.reportgenerator.report_title="${TITLE// /_}" \
    ${@:2}

# Upload report
REPORT_DIR=$(date '+%Y-%m-%d--%H-%M-%S')-${TEST_NAME}
aws s3 cp --recursive /reports s3://${BUCKET}/reports/${REPORT_DIR}
aws s3 cp --recursive /logs s3://${BUCKET}/reports/${REPORT_DIR}/logs