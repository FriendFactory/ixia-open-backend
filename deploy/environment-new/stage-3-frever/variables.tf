variable "env" {
  type        = string
  description = "The name of the environment"
}

variable "region" {
  type    = string
  default = "eu-central-1"
}

variable "s3_bucket_name" {
  type = string
}

variable "create_db" {
  type    = bool
  default = true
}