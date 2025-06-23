resource "aws_ecr_repository" "ecr_repository" {
  name                 = "${var.env}/appservice"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = false
  }
}
