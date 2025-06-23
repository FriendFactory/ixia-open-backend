provider "kubernetes" {
  host                   = data.aws_eks_cluster.cluster.endpoint
  token                  = data.aws_eks_cluster_auth.cluster.token
  cluster_ca_certificate = base64decode(data.aws_eks_cluster.cluster.certificate_authority.0.data)
}

data "aws_eks_cluster" "cluster" {
  name = var.eks_cluster
}

data "aws_eks_cluster_auth" "cluster" {
  name = var.eks_cluster
}


# # AWS ALB Controller
# # https://registry.terraform.io/modules/iplabs/alb-ingress-controller/kubernetes/latest
# module "alb_ingress_controller" {
#   source  = "iplabs/alb-ingress-controller/kubernetes"
#   version = "3.1.0"

#   providers = {
#     kubernetes = kubernetes
#   }

#   k8s_cluster_type = "eks"
#   k8s_namespace    = "kube-system"

#   aws_region_name  = var.region
#   k8s_cluster_name = data.aws_eks_cluster.cluster.id
#   aws_tags = {
#   }
# }


# External DNS
# https://www.padok.fr/en/blog/external-dns-route53-eks
module "external_dns" {
  source  = "cookielab/external-dns-aws/kubernetes"
  version = "0.9.0"

  policy = "sync"

  domains = [
    "frever-api.com"
  ]

  sources = [
    "ingress"
  ]

  owner_id              = "${var.env}-owner"
  aws_create_iam_policy = true
  aws_iam_policy_name   = "${var.env}-KubernetesExternalDNS"
}


data "aws_acm_certificate" "issued" {
  domain = "frever-api.com"
}
