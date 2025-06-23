data "aws_iam_user" "admin" {
  user_name = var.admin_user_name
}

module "eks" {
  source                    = "terraform-aws-modules/eks/aws"
  cluster_name              = var.env
  cluster_version           = "1.19"
  subnets                   = module.vpc.private_subnets
  cluster_enabled_log_types = []
  enable_irsa               = true

  vpc_id = module.vpc.vpc_id

  map_users = [
    {
      userarn  = data.aws_iam_user.admin.arn
      username = var.admin_user_name,
      groups   = ["system:masters"]
    }
  ]

  node_groups = {
    app = {
      desired_capacity = 2
      max_capacity     = 5
      min_capacity     = 2

      subnets = concat(module.vpc.private_subnets, module.vpc.database_subnets, module.vpc.elasticache_subnets)

      iam_role_name            = "${var.env}-app-eks-role"
      iam_role_use_name_prefix = false

      instance_types = ["c5.xlarge"]
      k8s_labels = {
        Environment = var.env
        AppGroup    = "frever"
      }

      tags = {
        "k8s.io/cluster-autoscaler/${var.env}" = "owned",
        "k8s.io/cluster-autoscaler/enabled"    = "TRUE"
      }
    }
    jaeger = {
      desired_capacity = 1
      max_capacity     = 2
      min_capacity     = 1

      subnets = module.vpc.private_subnets

      iam_role_name            = "${var.env}-jaeger-eks-role"
      iam_role_use_name_prefix = false

      instance_types = ["r5.large"]
      disk_size      = 200
      k8s_labels = {
        Environment = var.env
        AppGroup    = "jaeger"
      }
    }
  }
}


resource "aws_iam_policy" "route53limited" {
  name        = "${var.env}-limited-route53-policy"
  path        = "/"
  description = "Route53 access for external dns"

  policy = jsonencode({
    "Version" : "2012-10-17",
    "Statement" : [
      {
        "Effect" : "Allow",
        "Action" : [
          "route53:ChangeResourceRecordSets"
        ],
        "Resource" : [
          "arn:aws:route53:::hostedzone/*"
        ]
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "route53:ListHostedZones",
          "route53:ListResourceRecordSets"
        ],
        "Resource" : [
          "*"
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "external_dns_policy_attachment" {
  role       = module.eks.worker_iam_role_name
  policy_arn = aws_iam_policy.route53limited.arn
}

provider "kubernetes" {
  host                   = data.aws_eks_cluster.cluster.endpoint
  token                  = data.aws_eks_cluster_auth.cluster.token
  cluster_ca_certificate = base64decode(data.aws_eks_cluster.cluster.certificate_authority.0.data)
}

provider "helm" {
  kubernetes {
    host                   = data.aws_eks_cluster.cluster.endpoint
    cluster_ca_certificate = base64decode(data.aws_eks_cluster.cluster.certificate_authority.0.data)
    token                  = data.aws_eks_cluster_auth.cluster.token
  }
}

module "eks-cluster-autoscaler" {
  source                           = "lablabs/eks-cluster-autoscaler/aws"
  version                          = "1.3.0"
  cluster_identity_oidc_issuer     = data.aws_eks_cluster.cluster.identity[0].oidc[0].issuer
  cluster_identity_oidc_issuer_arn = replace(data.aws_eks_cluster.cluster.identity[0].oidc[0].issuer, "https://", "arn:aws:iam::${data.aws_caller_identity.current.account_id}:oidc-provider/")
  cluster_name                     = var.env
}


data "aws_eks_cluster" "cluster" {
  name = module.eks.cluster_id
}

data "aws_eks_cluster_auth" "cluster" {
  name = module.eks.cluster_id
}

data "aws_caller_identity" "current" {}
