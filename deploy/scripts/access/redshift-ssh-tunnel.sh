#!/bin/bash

SSH_PEM=$1

if [[ ${SSH_PEM} == "" ]]; then
    echo "Please specify SSH key file as first argument"
    exit 1
fi

chmod 400 ${SSH_PEM}

CLUSTER=redshift-cluster-1.cyqr6i0oac6y.eu-central-1.redshift.amazonaws.com

ssh -i ${SSH_PEM} \
    -N \
    -l ec2-user \
    -L 5439:${CLUSTER}:5439 ssh-default.frever-api.com -v