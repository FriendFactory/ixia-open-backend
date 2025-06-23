#!/bin/bash

pip3 install --target ./package redshift_connector

FUNCTION_NAME=RedshiftImporter

rm redshift-importer.zip
zip -r ./redshift-importer.zip .
aws lambda update-function-code --function-name ${FUNCTION_NAME} --zip-file fileb://redshift-importer.zip

# rm redshift-importer.zip
echo ${FUNCTION_NAME} updated successfully