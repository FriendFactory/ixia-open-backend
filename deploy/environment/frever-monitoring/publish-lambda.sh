#!/bin/bash

FUNCTION_NAME=AlarmCloudWatchToSlack


cd slack-alarm-lambda
zip -r ../slack-alarm-lambda.zip .

cd ..
aws lambda update-function-code --function-name ${FUNCTION_NAME} --zip-file fileb://slack-alarm-lambda.zip

rm slack-alarm-lambda.zip

echo ${FUNCTION_NAME} updated successfully