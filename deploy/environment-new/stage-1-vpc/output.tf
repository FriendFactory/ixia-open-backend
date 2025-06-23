output "elasticache_subnet_group" {
    value = module.vpc.elasticache_subnet_group_name
}

output "region" {
  value = var.region
}

output "vpc_id" {
  value = module.vpc.vpc_id
}
