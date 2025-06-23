variable "env" {
  type        = string
  description = "The name of the environment"
}

variable "region" {
  type    = string
  default = "eu-central-1"
}

variable "ssh_key_pair_name" {
  type    = string
}