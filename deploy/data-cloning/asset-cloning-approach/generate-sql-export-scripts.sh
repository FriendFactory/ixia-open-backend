#!/bin/bash
# Generates SQL commands required to export data from tables due environment cloning

set -u
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

CONNECTION_STRING=$1
CONFIG=$(cat ${DIR}/cloning-config.json)

TABLES=($(echo ${CONFIG} | jq --raw-output '.[] | .table'))

for i in ${!TABLES[@]}
do
    TABLE=$(echo ${CONFIG} | jq --raw-output ".[${i}] | .table")
    WHERE=$(echo ${CONFIG} | jq --raw-output ".[${i}] | .where")
    MODE=$(echo ${CONFIG} | jq --raw-output ".[${i}] | .mode")
    ID_COLUMN=$(echo ${CONFIG} | jq --raw-output ".[${i}] | .id")

    if [[ ${WHERE} = "null" ]];
    then
        WHERE=" "
    fi

    if [[ ${ID_COLUMN} = "null" ]];
    then
        ID_COLUMN=" "
    fi

    if [[ ${MODE} = "null" ]]
    then
        MODE="InsertAndUpdate"
    fi

    echo "Generating export script for ${TABLE}..."

    dotnet ${DIR}/script-generator/Frever.Utils.TableDataCloneScriptGenerator/bin/Debug/net8.0/frever.generate-inserts.dll \
         -t ${TABLE} -c=${CONNECTION_STRING} -w="${WHERE}" -m=${MODE} -i="${ID_COLUMN}" > ${DIR}/sql-scripts/${TABLE}.sql

    echo "Done!"
done;