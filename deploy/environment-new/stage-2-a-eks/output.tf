output "region" {
  value = var.region
}

output "cluster_name" {
  value = var.env
}

output "app_node_group_role_arn" {
  value = module.eks.eks_managed_node_groups.app.iam_role_arn
}
