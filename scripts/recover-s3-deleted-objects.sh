#!/bin/bash

BUCKET=xxxxxxxxx
DIR_ASSETS=Assets
DIR_VIDEO=Video
DIR_TMP=Test

PREFIXES=($DIR_TMP $DIR_ASSETS $DIR_VIDEO)

for DIR in "${PREFIXES[@]}"
do
    echo "Recovering deleted files in ${DIR}"

    aws s3api list-object-versions \
        --bucket ${BUCKET} \
        --prefix="${DIR}" \
        --query='{Objects: DeleteMarkers[?IsLatest==`true`].{Key:Key,VersionId:VersionId}}' \
        --output text |
    while read KEY
    do

        if [[  "$KEY" == "None" ]]; then
        continue
        else
            FILE_KEY=$(echo ${KEY} | awk '{print $2}')
            FILE_VERSION=$(echo ${KEY} | awk '{print $3}')

            aws s3api delete-object --bucket ${BUCKET} --key "${FILE_KEY}" --version-id "${FILE_VERSION}" > /dev/null

            echo "File ${FILE_KEY} with version ${FILE_VERSION} has been recovered"

        fi

    done

    echo "Recovering completed in ${DIR}"
    echo ""
    echo ""
done