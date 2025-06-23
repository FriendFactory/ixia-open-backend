#!/bin/bash

VPC_ID=$1

if [[ ${VPC_ID} == "" ]]; then
    echo "VPC ID must be specified as first argument"
    exit 1
fi


aws ec2 describe-network-interfaces --filters Name=vpc-id,Values=${VPC_ID}