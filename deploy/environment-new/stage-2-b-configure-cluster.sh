#!/bin/bash

ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

export KUBECONFIG="${DIR}/.configs/kubeconfig_${ENV}"

export $(cat ${DIR}/.configs/${ENV}/vpc.env | xargs)
export $(cat ${DIR}/.configs/${ENV}/eks.env | xargs)

### ALB Ingress Controller
# https://docs.aws.amazon.com/eks/latest/userguide/aws-load-balancer-controller.html
echo "Installing AWS Ingress Controller..."

helm repo add eks https://aws.github.io/eks-charts

# helm search repo eks/aws-load-balancer-controller --versions
# exit

helm uninstall aws-load-balancer-controller -n kube-system

helm upgrade \
    --install \
    aws-load-balancer-controller eks/aws-load-balancer-controller \
    -n kube-system \
    --set clusterName=${ENV} \
    --set vpcId=${VPC_ID} \
    --set region=eu-central-1 \
    --set serviceAccount.create=false \
    --set keepTLSSecret=true

kubectl apply -k "github.com/aws/eks-charts/stable/aws-load-balancer-controller//crds?ref=master"



### External DNS
echo "Installing external DNS..."

helm repo add bitnami https://charts.bitnami.com/bitnami
helm upgrade  \
    external-dns  bitnami/external-dns \
    --install \
    --namespace kube-system \
    --set aws.region=${REGION} \
    --set aws.roleArn=${APP_NODE_GROUP_ROLE_ARN}

### Metrics Server
echo "Installing metrics server..."
helm repo add metrics-server https://kubernetes-sigs.github.io/metrics-server/
helm upgrade metrics-server metrics-server/metrics-server \
    --install \
    --namespace kube-system \
    --set "defaultArgs[0]=--cert-dir=/tmp" \
    --set "defaultArgs[1]=--kubelet-preferred-address-types=Hostname" \
    --set "defaultArgs[2]=--kubelet-use-node-status-port" \
    --set "defaultArgs[3]=--metric-resolution=15s" \
    --set "defaultArgs[4]=--kubelet-insecure-tls=true"

### Cluster autoscaler
echo "Installing cluster autoscaler..."
helm repo add autoscaler https://kubernetes.github.io/autoscaler

helm upgrade cluster-autoscaler autoscaler/cluster-autoscaler \
    --install \
    --namespace kube-system \
    --set 'autoDiscovery.clusterName'=${ENV} \
    --set awsRegion=${REGION}


#####################
echo "Installing Jaeger..."
helm upgrade \
    --install \
    --set "domain=frever-api.com" \
    --set "sslCertificateArn=${SSL_CERTIFICATE_ARN}" \
    --namespace jaeger \
    --create-namespace \
    ${ENV} \
    ${DIR}/../application/helm-chart/jaeger


# # Deploy Prometheus and Grafana
# # https://docs.aws.amazon.com/eks/latest/userguide/prometheus.html
# PROMETHEUS_NAMESPACE=prometheus
# helm repo add kube-state-metrics https://kubernetes.github.io/kube-state-metrics
# helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
# helm repo add grafana https://grafana.github.io/helm-charts
# helm repo update


# PROMETHEUS_CONFIG="${ENV_DIR}/prometheus.yaml"

# cat "${CONFIG_DIR}/prometheus.yaml" | sed "s/<<cluster_name>>/${ENV}/" > ${PROMETHEUS_CONFIG}

# helm upgrade -i prometheus prometheus-community/prometheus \
#     --namespace ${PROMETHEUS_NAMESPACE} \
#     --create-namespace \
#     -f "${PROMETHEUS_CONFIG}" \
#     --set alertmanager.persistentVolume.storageClass="gp2",server.persistentVolume.storageClass="gp2"


# helm upgrade -i grafana grafana/grafana \
#     -f "${CONFIG_DIR}/grafana.yaml" \
#     --namespace ${PROMETHEUS_NAMESPACE} \
#     --create-namespace


# ### Grafana dashboards

# GRAFANA_DASHBOARDS_DIR="${ENV_DIR}/grafana-dashboards"

# mkdir ${GRAFANA_DASHBOARDS_DIR}

# for i in ${CONFIG_DIR}/grafana-dashboards/*.yaml
# do
#     cat ${i} | sed "s/<<env_name>>/${ENV}/" > "${GRAFANA_DASHBOARDS_DIR}/$(basename ${i})"
# done

# kubectl apply -f ${GRAFANA_DASHBOARDS_DIR}