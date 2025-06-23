module "auth_service" {
  source       = "./net-service"
  service_name = "auth"
  env          = var.env
}


module "notification_service" {
  source       = "./net-service"
  service_name = "notification"
  env          = var.env
}

module "asset_service" {
  source       = "./net-service"
  service_name = "asset"
  env          = var.env
}

module "main_service" {
  source       = "./net-service"
  service_name = "main"
  env          = var.env
}

module "assetmanager_service" {
  source       = "./net-service"
  service_name = "assetmanager"
  env          = var.env
}

module "video_service" {
  source       = "./net-service"
  service_name = "video"
  env          = var.env
}

module "social_service" {
  source       = "./net-service"
  service_name = "social"
  env          = var.env
}

resource "aws_ecr_repository" "appservice" {
  name                 = "${var.env}/appservice"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = false
  }
}
