#!/bin/bash
LOG_PATH=s3://frever-lb-logs/content-test/cloudfront/
LOG_DATE="2021-05-11"
REGION=eu-central-1

mkdir logs

aws s3 cp --recursive --region ${REGION} --exclude "*" --include "*${LOG_DATE}*" ${LOG_PATH} logs

LOGS_COMBINED=logs/logs-combined.txt
rm -f ${LOGS_COMBINED}

i=0
T=$(printf "\t")
for f in `ls logs/*.gz | sort -V`; do

if (( !$i )); then
    gunzip -c ${f} | sed 1,1d | tr -s '[:blank:]' '\t' >> ${LOGS_COMBINED}
else
    gunzip -c ${f} | sed 1,2d >> ${LOGS_COMBINED}
fi

i=$((i+1))

done;

cat ${LOGS_COMBINED} > logs/log.txt


