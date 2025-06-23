#!/bin/bash


DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

SRC=$1
DST=$2

declare -A envs
envs=(["prod"]=10 ["stage"]=5 ["test"]=3 ["dev"]=1)

SRC_LEVEL=${envs[$SRC]}
DST_LEVEL=${envs[$DST]}

if ! [[ -v envs[$SRC] ]]; then
    echo "${SRC} is not a known environment";
    exit 1;
fi

if ! [[ -v envs[$DST] ]]; then
    echo "${DST} is not a known environment";
    exit 1;
fi

if [[ SRC_LEVEL -le DST_LEVEL ]]; then
    echo "Cloning from ${SRC} to ${DST} is not allowed";
    exit 1;
fi

set -u
set -e

echo "Clone from ${SRC} to ${DST}"

mkdir -p /root/.ssh

aws ssm get-parameter --name "/data-cloning/${SRC}/ssh-config" --query "Parameter.Value" --output text >> /root/.ssh/config
echo "" >> /root/.ssh/config
aws ssm get-parameter --name "/data-cloning/${SRC}/ssh-key-base64" --query "Parameter.Value" --output text | \
        base64 -d > /root/.ssh/${SRC}_id_pkey
chmod 700 /root/.ssh/${SRC}_id_pkey

aws ssm get-parameter --name "/data-cloning/${DST}/ssh-config" --query "Parameter.Value" --output text >> /root/.ssh/config
aws ssm get-parameter --name "/data-cloning/${DST}/ssh-key-base64" --query "Parameter.Value" --output text | \
        base64 -d > /root/.ssh/${DST}_id_pkey
chmod 700 /root/.ssh/${DST}_id_pkey


AUTH_DUMP=/root/host/${SRC}_auth/
MAIN_DUMP=/root/host/${SRC}_main/

### EXPORT DUMP FROM SOURCE ENV

SRC_AUTH_DB_LOGIN=$(aws ssm get-parameter --name "/data-cloning/${SRC}/db/auth/login" --query "Parameter.Value" --output text)
SRC_AUTH_DB_PASSWORD=$(aws ssm get-parameter --name "/data-cloning/${SRC}/db/auth/password" --query "Parameter.Value" --output text)
SRC_AUTH_DB_NAME=$(aws ssm get-parameter --name "/data-cloning/${SRC}/db/auth/db-name" --query "Parameter.Value" --output text)

SRC_MAIN_DB_LOGIN=$(aws ssm get-parameter --name "/data-cloning/${SRC}/db/main/login" --query "Parameter.Value" --output text)
SRC_MAIN_DB_PASSWORD=$(aws ssm get-parameter --name "/data-cloning/${SRC}/db/main/password" --query "Parameter.Value" --output text)
SRC_MAIN_DB_NAME=$(aws ssm get-parameter --name "/data-cloning/${SRC}/db/main/db-name" --query "Parameter.Value" --output text)

SRC_AUTH_CONNECTION="postgresql://${SRC_AUTH_DB_LOGIN}:${SRC_AUTH_DB_PASSWORD}@127.0.0.1:5443/${SRC_AUTH_DB_NAME}"
SRC_MAIN_CONNECTION="postgresql://${SRC_MAIN_DB_LOGIN}:${SRC_MAIN_DB_PASSWORD}@127.0.0.1:5444/${SRC_MAIN_DB_NAME}"

echo "Source DB: auth=${SRC_AUTH_DB_NAME} main=${SRC_MAIN_DB_NAME}"

ssh -S /tmp/${SRC}.socket -f -N -M -o ExitOnForwardFailure=yes -o StrictHostKeyChecking=no ${SRC}

echo "Dump Auth DB"

rm -rf "${AUTH_DUMP}"
pg_dump --file="${AUTH_DUMP}" \
        --clean --no-owner --no-privileges --no-acl --if-exists --load-via-partition-root --no-comments --verbose \
        --schema=public \
        --format=directory --compress=4 --jobs=4 \
        ${SRC_AUTH_CONNECTION}

echo ""
echo ""
echo ""
echo "Dump Main DB"

rm -rf "${MAIN_DUMP}"
pg_dump --file="${MAIN_DUMP}" \
        --clean --no-owner --no-privileges --no-acl --if-exists --load-via-partition-root --no-comments --verbose \
        --schema=public --schema=cms --schema=rollup --schema=stats \
        --format=directory --compress=4 --jobs=4 \
        ${SRC_MAIN_CONNECTION}

echo "Finish DB dumps"

ssh -S /tmp/${SRC}.socket -O exit ${SRC}
echo "SSH to ${SRC} closed"

### RESTORE DUMP ON TARGET ENV

echo "Get ${DST} parameters from AWS parameters store"
DST_AUTH_DB_LOGIN=$(aws ssm get-parameter --name "/data-cloning/${DST}/db/auth/login" --query "Parameter.Value" --output text)
DST_AUTH_DB_PASSWORD=$(aws ssm get-parameter --name "/data-cloning/${DST}/db/auth/password" --query "Parameter.Value" --output text)
DST_AUTH_DB_NAME=$(aws ssm get-parameter --name "/data-cloning/${DST}/db/auth/db-name" --query "Parameter.Value" --output text)
DST_MAIN_DB_LOGIN=$(aws ssm get-parameter --name "/data-cloning/${DST}/db/main/login" --query "Parameter.Value" --output text)
DST_MAIN_DB_PASSWORD=$(aws ssm get-parameter --name "/data-cloning/${DST}/db/main/password" --query "Parameter.Value" --output text)
DST_MAIN_DB_NAME=$(aws ssm get-parameter --name "/data-cloning/${DST}/db/main/db-name" --query "Parameter.Value" --output text)

DST_AUTH_CONNECTION="postgresql://${DST_AUTH_DB_LOGIN}:${DST_AUTH_DB_PASSWORD}@127.0.0.1:5443/${DST_AUTH_DB_NAME}"
DST_MAIN_CONNECTION="postgresql://${DST_MAIN_DB_LOGIN}:${DST_MAIN_DB_PASSWORD}@127.0.0.1:5444/${DST_MAIN_DB_NAME}"


echo "Destination DB: auth=${DST_AUTH_DB_NAME} main=${DST_MAIN_DB_NAME}"

echo "Open SSH to ${DST}..."
ssh -S /tmp/${DST}.socket -f -N -M -o ExitOnForwardFailure=yes -o StrictHostKeyChecking=no ${DST}

export PGPASSWORD=${DST_AUTH_DB_PASSWORD}

set +e

echo "Clearing Auth database..."

psql --no-password --single-transaction --file="${DIR}/clear-db.sql"  "${DST_AUTH_CONNECTION}"
echo "Clearing Auth database completed"

psql --no-password --single-transaction --file="${DIR}/clear-db.sql"  "${DST_MAIN_CONNECTION}"
echo "Clearing Main database completed"

echo "Restoring Auth database..."

pg_restore \
        --host=127.0.0.1 --port=5443 --username=${DST_AUTH_DB_LOGIN} --dbname=${DST_AUTH_DB_NAME} --no-password \
        --clean --if-exists --no-owner --no-privileges --no-acl --verbose\
        --schema=public \
        --jobs=4 \
        "${AUTH_DUMP}"

echo "Auth database restored"

echo ""
echo ""
echo ""

echo "Restoring Main database..."

export PGPASSWORD=${DST_MAIN_DB_PASSWORD}
pg_restore \
        --host=127.0.0.1 --port=5443 --username=${DST_MAIN_DB_LOGIN} --dbname=${DST_MAIN_DB_NAME} --no-password \
        --clean --if-exists --no-owner --no-privileges --no-acl --verbose \
        --schema=public --schema=cms --schema=rollup --schema=stats \
        --jobs=4 \
        "${MAIN_DUMP}"

echo "Main database restored"
echo "All databases restored"

set -e

ssh -S /tmp/${DST}.socket -O exit ${DST}
echo "SSH to ${DST} closed"

# ASSET FILES COPYING

SRC_S3_BUCKET=$(aws ssm get-parameter --name "/data-cloning/${SRC}/s3-bucket" --query "Parameter.Value" --output text)
DST_S3_BUCKET=$(aws ssm get-parameter --name "/data-cloning/${DST}/s3-bucket" --query "Parameter.Value" --output text)

FOLDERS=("Assets" "Files" "Video")

echo "Start asset copying..."

for F in ${FOLDERS[@]}; do
        echo "Syncing s3://${SRC_S3_BUCKET}/${F} with s3://${DST_S3_BUCKET}/${F}"
        aws s3 sync "s3://${SRC_S3_BUCKET}/${F}" "s3://${DST_S3_BUCKET}/${F}"
done;

echo "Asset copying completed"

echo "CLONING COMPLETED"