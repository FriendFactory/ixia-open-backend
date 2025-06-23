variable "env" {
  type        = string
  description = "The name of the environment"
}

variable "region" {
  type    = string
  default = "eu-central-1"
}

variable "createDb" {
  type    = bool
  default = false
}

variable "admin_user_name" {
  type    = string
  default = "Sergey"
}

variable "s3_bucket_name" {
  type = string
}

variable "db_user_name" {
  type    = string
  default = "root"
}

variable "db_password" {
  type    = string
  default = "R00tAtMs33r"
}
