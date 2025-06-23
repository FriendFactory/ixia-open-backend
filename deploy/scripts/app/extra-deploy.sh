#!/bin/bash
ENV=$1

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi

kubectl config use-context $( kubectl config get-contexts | grep "${ENV}" | awk '{ print $3 }')

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

ENV_DIR="${DIR}/../../.deploy/${ENV}"
ENV_FILE="${ENV_DIR}/.env"

export $(cat ${ENV_FILE} | xargs)

CONFIG_DIR="${DIR}/../../configs"

cd "${DIR}/../../application/helm-chart"

export KUBECONFIG="${DIR}/../../environment/frever/kubeconfig_${ENV}"

### ALB Ingress Controller
eksctl utils associate-iam-oidc-provider \
    --region eu-central-1 \
    --cluster ${ENV} \
    --approve

curl -o iam-policy.json https://raw.githubusercontent.com/kubernetes-sigs/aws-load-balancer-controller/v2.2.1/docs/install/iam_policy.json
aws iam create-policy \
    --policy-name AWSLoadBalancerControllerIAMPolicy \
    --policy-document file://iam-policy.json

eksctl create iamserviceaccount \
--cluster=${ENV} \
--namespace=kube-system \
--name=aws-load-balancer-controller \
--attach-policy-arn=xxxxxxxxx \
--override-existing-serviceaccounts \
--approve

helm repo add eks https://aws.github.io/eks-charts
helm install \
    aws-load-balancer-controller eks/aws-load-balancer-controller \
    -n kube-system \
    --set clusterName=${ENV} \
    --set serviceAccount.create=false \
    --set serviceAccount.name=aws-load-balancer-controller


#####################
helm upgrade \
    --install \
    --set "domain=frever-api.com" \
    --set "sslCertificateArn=${SSL_CERTIFICATE_ARN}" \
    --namespace jaeger \
    --create-namespace \
    ${CLUSTER_NAME} \
    ./jaeger

# Deploy Prometheus and Grafana
# https://docs.aws.amazon.com/eks/latest/userguide/prometheus.html
PROMETHEUS_NAMESPACE=prometheus
helm repo add kube-state-metrics https://kubernetes.github.io/kube-state-metrics
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update


PROMETHEUS_CONFIG="${ENV_DIR}/prometheus.yaml"

cat "${CONFIG_DIR}/prometheus.yaml" | sed "s/<<cluster_name>>/${ENV}/" > ${PROMETHEUS_CONFIG}

helm upgrade -i prometheus prometheus-community/prometheus \
    --namespace ${PROMETHEUS_NAMESPACE} \
    --create-namespace \
    -f "${PROMETHEUS_CONFIG}" \
    --set alertmanager.persistentVolume.storageClass="gp2",server.persistentVolume.storageClass="gp2"


helm upgrade -i grafana grafana/grafana \
    -f "${CONFIG_DIR}/grafana.yaml" \
    --namespace ${PROMETHEUS_NAMESPACE} \
    --create-namespace


### Grafana dashboards

GRAFANA_DASHBOARDS_DIR="${ENV_DIR}/grafana-dashboards"

mkdir ${GRAFANA_DASHBOARDS_DIR}

for i in ${CONFIG_DIR}/grafana-dashboards/*.yaml
do
    cat ${i} | sed "s/<<env_name>>/${ENV}/" > "${GRAFANA_DASHBOARDS_DIR}/$(basename ${i})"
done

kubectl apply -f ${GRAFANA_DASHBOARDS_DIR}