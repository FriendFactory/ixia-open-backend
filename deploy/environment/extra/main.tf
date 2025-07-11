terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 3.0"
    }
  }

  backend "s3" {
    bucket  = "frever-terraform"
    key     = "frever-main-extra"
    region  = "eu-central-1"
    profile = "friendsfactory"
  }
}

provider "aws" {
  profile = "friendsfactory"
  region  = var.region
}
