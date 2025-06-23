data "aws_vpc" "vpc" {
  filter {
    name   = "tag:Name"
    values = [var.env]
  }
}

data "aws_subnet_ids" "private" {
  vpc_id = data.aws_vpc.vpc.id
  tags = {
    Tier = "Private"
  }
}

data "aws_subnet_ids" "public" {
  vpc_id = data.aws_vpc.vpc.id
  tags = {
    Tier = "Private"
  }
}

data "aws_subnet_ids" "database" {
  vpc_id = data.aws_vpc.vpc.id
  tags = {
    Tier = "Database"
  }
}

data "aws_subnet_ids" "elasticache" {
  vpc_id = data.aws_vpc.vpc.id
  tags = {
    Tier = "ElastiCache"
  }
}

module "eks" {
  source          = "terraform-aws-modules/eks/aws"
  cluster_name    = var.env
  cluster_version = "1.21"

  vpc_id = data.aws_vpc.vpc.id

  # cluster_ip_family          = "ipv6" # WARN: IPv6 will cause errors on load balancer creation. To change that need to change target from instance to IP
  # create_cni_ipv6_iam_policy = false  # TODO: Add extra check with data

  cluster_enabled_log_types              = []
  create_cloudwatch_log_group            = false
  cloudwatch_log_group_retention_in_days = 7
  enable_irsa                            = true

  cluster_endpoint_private_access = true
  cluster_endpoint_public_access  = true

  subnet_ids = concat(sort(data.aws_subnet_ids.private.ids), sort(data.aws_subnet_ids.public.ids))

  cluster_tags = {
    frever-env = var.env
  }

  iam_role_tags = {
    frever-env = var.env
  }

  eks_managed_node_groups = {
    app = {
      name            = "app"
      use_name_prefix = false

      desired_size = 2
      max_size     = 5
      min_size     = 2

      subnets = concat(sort(data.aws_subnet_ids.private.ids), sort(data.aws_subnet_ids.public.ids),
      sort(data.aws_subnet_ids.database.ids), sort(data.aws_subnet_ids.elasticache.ids))

      instance_types = ["t3.medium"]
      labels = {
        Environment = var.env
        AppGroup    = "frever"
      }

      tags = {
        frever-env                             = var.env
        "k8s.io/cluster-autoscaler/${var.env}" = "owned",
        "k8s.io/cluster-autoscaler/enabled"    = "TRUE"
      }
    }

    jaeger = {
      name            = "jaeger"
      use_name_prefix = false

      desired_size = 1
      max_size     = 2
      min_size     = 1

      subnets = concat(sort(data.aws_subnet_ids.private.ids), sort(data.aws_subnet_ids.public.ids))

      instance_types = ["r5.large"]
      disk_size      = 200

      labels = {
        Environment = var.env
        AppGroup    = "jaeger"
      }

      tags = {
        frever-env = var.env
      }
    }
  }

  cluster_security_group_additional_rules = {

    ingress_access = {
      description      = "Full access"
      protocol         = "-1"
      from_port        = 0
      to_port          = 0
      type             = "ingress"
      cidr_blocks      = ["0.0.0.0/0"]
      ipv6_cidr_blocks = ["::/0"]
    }
    egress_access = {
      description      = "Full access"
      protocol         = "-1"
      from_port        = 0
      to_port          = 0
      type             = "egress"
      cidr_blocks      = ["0.0.0.0/0"]
      ipv6_cidr_blocks = ["::/0"]
    }
  }

  # Extend node-to-node security group rules
  node_security_group_additional_rules = {
    ingress_access = {
      description      = "Full access"
      protocol         = "-1"
      from_port        = 0
      to_port          = 0
      type             = "ingress"
      cidr_blocks      = ["0.0.0.0/0"]
      ipv6_cidr_blocks = ["::/0"]
    }
    egress_access = {
      description      = "Full access"
      protocol         = "-1"
      from_port        = 0
      to_port          = 0
      type             = "egress"
      cidr_blocks      = ["0.0.0.0/0"]
      ipv6_cidr_blocks = ["::/0"]
    }

  }
}

# Policy for External DNS
resource "aws_iam_policy" "route53limited" {
  name        = "${var.env}-external-dns"
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
  role       = module.eks.eks_managed_node_groups.app.iam_role_name
  policy_arn = aws_iam_policy.route53limited.arn
}

resource "aws_iam_role_policy_attachment" "external_dns_policy_attachment_jaeger" {
  role       = module.eks.eks_managed_node_groups.jaeger.iam_role_name
  policy_arn = aws_iam_policy.route53limited.arn
}


resource "aws_iam_policy" "cluster_autoscaler" {
  name        = "${var.env}-cluster-autoscaler"
  path        = "/"
  description = "Permissions for cluster autoscaler"

  policy = jsonencode({
    "Version" : "2012-10-17",
    "Statement" : [
      {
        "Effect" : "Allow",
        "Action" : [
          "autoscaling:DescribeAutoScalingGroups",
          "autoscaling:DescribeAutoScalingInstances",
          "autoscaling:DescribeLaunchConfigurations",
          "autoscaling:DescribeTags",
          "autoscaling:SetDesiredCapacity",
          "autoscaling:TerminateInstanceInAutoScalingGroup"
        ],
        "Resource" : "*"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "cluster_autoscaler_policy_attachment" {
  role       = module.eks.eks_managed_node_groups.app.iam_role_name
  policy_arn = aws_iam_policy.cluster_autoscaler.arn
}

resource "aws_iam_role_policy_attachment" "cluster_autoscaler_policy_attachment_jaeger" {
  role       = module.eks.eks_managed_node_groups.jaeger.iam_role_name
  policy_arn = aws_iam_policy.cluster_autoscaler.arn
}


# Policy for ALB
resource "aws_iam_policy" "alb" {
  name        = "${var.env}-alb-controller"
  path        = "/"
  description = "ALB Controller policy"

  policy = jsonencode({
    "Version" : "2012-10-17",
    "Statement" : [
      {
        "Effect" : "Allow",
        "Action" : [
          "iam:CreateServiceLinkedRole",
          "ec2:DescribeAccountAttributes",
          "ec2:DescribeAddresses",
          "ec2:DescribeAvailabilityZones",
          "ec2:DescribeVpcPeeringConnections",
          "ec2:DescribeInternetGateways",
          "ec2:DescribeVpcs",
          "ec2:DescribeSubnets",
          "ec2:DescribeSecurityGroups",
          "ec2:DescribeInstances",
          "ec2:DescribeNetworkInterfaces",
          "ec2:DescribeTags",
          "ec2:GetCoipPoolUsage",
          "ec2:DescribeCoipPools",
          "elasticloadbalancing:DescribeLoadBalancers",
          "elasticloadbalancing:DescribeLoadBalancerAttributes",
          "elasticloadbalancing:DescribeListeners",
          "elasticloadbalancing:DescribeListenerCertificates",
          "elasticloadbalancing:DescribeSSLPolicies",
          "elasticloadbalancing:DescribeRules",
          "elasticloadbalancing:DescribeTargetGroups",
          "elasticloadbalancing:DescribeTargetGroupAttributes",
          "elasticloadbalancing:DescribeTargetHealth",
          "elasticloadbalancing:DescribeTags"
        ],
        "Resource" : "*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "cognito-idp:DescribeUserPoolClient",
          "acm:ListCertificates",
          "acm:DescribeCertificate",
          "iam:ListServerCertificates",
          "iam:GetServerCertificate",
          "waf-regional:GetWebACL",
          "waf-regional:GetWebACLForResource",
          "waf-regional:AssociateWebACL",
          "waf-regional:DisassociateWebACL",
          "wafv2:GetWebACL",
          "wafv2:GetWebACLForResource",
          "wafv2:AssociateWebACL",
          "wafv2:DisassociateWebACL",
          "shield:GetSubscriptionState",
          "shield:DescribeProtection",
          "shield:CreateProtection",
          "shield:DeleteProtection"
        ],
        "Resource" : "*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ec2:AuthorizeSecurityGroupIngress",
          "ec2:RevokeSecurityGroupIngress"
        ],
        "Resource" : "*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ec2:CreateSecurityGroup"
        ],
        "Resource" : "*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ec2:CreateTags"
        ],
        "Resource" : "arn:aws:ec2:*:*:security-group/*",
        "Condition" : {
          "StringEquals" : {
            "ec2:CreateAction" : "CreateSecurityGroup"
          },
          "Null" : {
            "aws:RequestTag/elbv2.k8s.aws/cluster" : "false"
          }
        }
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ec2:CreateTags",
          "ec2:DeleteTags"
        ],
        "Resource" : "arn:aws:ec2:*:*:security-group/*",
        "Condition" : {
          "Null" : {
            "aws:RequestTag/elbv2.k8s.aws/cluster" : "true",
            "aws:ResourceTag/elbv2.k8s.aws/cluster" : "false"
          }
        }
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ec2:AuthorizeSecurityGroupIngress",
          "ec2:RevokeSecurityGroupIngress",
          "ec2:DeleteSecurityGroup"
        ],
        "Resource" : "*",
        "Condition" : {
          "Null" : {
            "aws:ResourceTag/elbv2.k8s.aws/cluster" : "false"
          }
        }
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "elasticloadbalancing:CreateLoadBalancer",
          "elasticloadbalancing:CreateTargetGroup"
        ],
        "Resource" : "*",
        "Condition" : {
          "Null" : {
            "aws:RequestTag/elbv2.k8s.aws/cluster" : "false"
          }
        }
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "elasticloadbalancing:CreateListener",
          "elasticloadbalancing:DeleteListener",
          "elasticloadbalancing:CreateRule",
          "elasticloadbalancing:DeleteRule"
        ],
        "Resource" : "*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "elasticloadbalancing:AddTags",
          "elasticloadbalancing:RemoveTags"
        ],
        "Resource" : [
          "arn:aws:elasticloadbalancing:*:*:targetgroup/*/*",
          "arn:aws:elasticloadbalancing:*:*:loadbalancer/net/*/*",
          "arn:aws:elasticloadbalancing:*:*:loadbalancer/app/*/*"
        ],
        "Condition" : {
          "Null" : {
            "aws:RequestTag/elbv2.k8s.aws/cluster" : "true",
            "aws:ResourceTag/elbv2.k8s.aws/cluster" : "false"
          }
        }
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "elasticloadbalancing:AddTags",
          "elasticloadbalancing:RemoveTags"
        ],
        "Resource" : [
          "arn:aws:elasticloadbalancing:*:*:listener/net/*/*/*",
          "arn:aws:elasticloadbalancing:*:*:listener/app/*/*/*",
          "arn:aws:elasticloadbalancing:*:*:listener-rule/net/*/*/*",
          "arn:aws:elasticloadbalancing:*:*:listener-rule/app/*/*/*"
        ]
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "elasticloadbalancing:ModifyLoadBalancerAttributes",
          "elasticloadbalancing:SetIpAddressType",
          "elasticloadbalancing:SetSecurityGroups",
          "elasticloadbalancing:SetSubnets",
          "elasticloadbalancing:DeleteLoadBalancer",
          "elasticloadbalancing:ModifyTargetGroup",
          "elasticloadbalancing:ModifyTargetGroupAttributes",
          "elasticloadbalancing:DeleteTargetGroup"
        ],
        "Resource" : "*",
        "Condition" : {
          "Null" : {
            "aws:ResourceTag/elbv2.k8s.aws/cluster" : "false"
          }
        }
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "elasticloadbalancing:RegisterTargets",
          "elasticloadbalancing:DeregisterTargets"
        ],
        "Resource" : "arn:aws:elasticloadbalancing:*:*:targetgroup/*/*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "elasticloadbalancing:SetWebAcl",
          "elasticloadbalancing:ModifyListener",
          "elasticloadbalancing:AddListenerCertificates",
          "elasticloadbalancing:RemoveListenerCertificates",
          "elasticloadbalancing:ModifyRule"
        ],
        "Resource" : "*"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "alb_to_node_role_attachment_app" {
  role       = module.eks.eks_managed_node_groups.app.iam_role_name
  policy_arn = aws_iam_policy.alb.arn
}
resource "aws_iam_role_policy_attachment" "alb_to_node_role_attachment_jaeger" {
  role       = module.eks.eks_managed_node_groups.jaeger.iam_role_name
  policy_arn = aws_iam_policy.alb.arn
}
