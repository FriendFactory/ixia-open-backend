#!/bin/bash
FN=$1

echo $FN


dotnet lambda deploy-function $FN \
    --function-runtime ".netcore3.1"
    # --region $AwsRegion \
    # --profile $AwsProfile \
