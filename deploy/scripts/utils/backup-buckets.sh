#!/bin/bash

BACKUP_BUCKET="frever-deleted-bucket-backup"

BUCKETS=( \
)

for B in ${BUCKETS[*]}
do
    aws s3 cp s3://${B} s3://${BACKUP_BUCKET}/${B} --recursive
done

exit 1

for B in ${BUCKETS[*]}
do
    aws s3 delete-bucket --bucket ${B}
done
