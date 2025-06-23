variable "eks_cluster" {
  type = string
}

variable "region" {
  type    = string
  default = "eu-central-1"
}

variable "env" {
  type        = string
  description = "The name of the environment"
}

variable "cluster_worker_iam_role_name" {
  type = string
}
