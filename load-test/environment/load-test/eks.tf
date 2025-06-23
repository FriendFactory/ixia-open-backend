data "aws_iam_user" "admin" {
  user_name = var.admin_user_name
}

module "eks" {
  source                    = "terraform-aws-modules/eks/aws"
  cluster_name              = var.env
  cluster_version           = "1.20"
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
      desired_capacity = 20
      max_capacity     = 40
      min_capacity     = 2

      subnets = concat(module.vpc.private_subnets)

      instance_types = ["r5.large"]
      k8s_labels = {
        Environment = var.env
        AppGroup    = "frever-load-test"
      }
    }
  }
}


provider "kubernetes" {
  host                   = data.aws_eks_cluster.cluster.endpoint
  token                  = data.aws_eks_cluster_auth.cluster.token
  cluster_ca_certificate = base64decode(data.aws_eks_cluster.cluster.certificate_authority.0.data)
}

data "aws_eks_cluster" "cluster" {
  name = module.eks.cluster_id
}

data "aws_eks_cluster_auth" "cluster" {
  name = module.eks.cluster_id
}
