
data "aws_availability_zones" "available" {}

module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "3.11.5"

  name                            = var.env
  cidr                            = "10.0.0.0/16"
  azs                             = data.aws_availability_zones.available.names
  private_subnets                 = ["10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24"]
  public_subnets                  = ["10.0.4.0/24", "10.0.5.0/24", "10.0.6.0/24"]
  create_database_subnet_group    = true
  database_subnets                = ["10.0.8.0/24", "10.0.9.0/24", "10.0.10.0/24"]
  create_elasticache_subnet_group = true
  elasticache_subnets             = ["10.0.11.0/24", "10.0.12.0/24", "10.0.13.0/24"]
  enable_nat_gateway              = true
  single_nat_gateway              = true
  enable_dns_hostnames            = true


  enable_ipv6                     = true
  assign_ipv6_address_on_creation = true

  private_subnet_assign_ipv6_address_on_creation = true

  create_egress_only_igw           = true
  public_subnet_ipv6_prefixes      = [0, 1, 2]
  private_subnet_ipv6_prefixes     = [3, 4, 5]
  database_subnet_ipv6_prefixes    = [6, 7, 8]
  elasticache_subnet_ipv6_prefixes = [9, 10, 11]


  tags = {
    "kubernetes.io/cluster/${var.env}" = "shared"
    "frever-env"                       = "${var.env}"
  }

  public_subnet_tags = {
    "kubernetes.io/cluster/${var.env}" = "shared"
    "kubernetes.io/role/elb"           = "1"
    "Tier"                             = "Public"
  }

  private_subnet_tags = {
    "kubernetes.io/cluster/${var.env}" = "shared"
    "kubernetes.io/role/internal-elb"  = "1"
    "Tier"                             = "Private"
  }

  database_subnet_tags = {
    "Tier" = "Database"
  }

  elasticache_subnet_tags = {
    "Tier" = "ElastiCache"
  }
}
