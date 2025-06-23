resource "aws_db_instance" "db" {
  count             = var.createDb ? 1 : 0
  name              = replace(var.env, "-", "")
  identifier        = replace(var.env, "-", "")
  apply_immediately = true
  # Server
  instance_class       = "db.t2.large"
  engine               = "postgres"
  engine_version       = "12"
  parameter_group_name = "default.postgres12"

  tags = {
    "kubernetes.io/environment" = var.env
  }

  # Storage
  allocated_storage     = 100
  max_allocated_storage = 200
  iops                  = 1000
  # Backup
  backup_retention_period = 7
  backup_window           = "03:00-04:00"
  skip_final_snapshot     = true
  # Networking
  multi_az               = true
  db_subnet_group_name   = module.vpc.database_subnet_group
  vpc_security_group_ids = [aws_security_group.db_security_group.id]
  # Logging
  performance_insights_enabled          = true
  performance_insights_retention_period = 7
  monitoring_interval                   = 30
  monitoring_role_arn                   = data.aws_iam_role.rds_monitoring_role.arn
  # Security
  username = var.db_user_name
  password = var.db_password
}

# TODO: Replace with creating role with policy, the role is automatically created by AWS RDS UI
data "aws_iam_role" "rds_monitoring_role" {
  name = "rds-monitoring-role"
}

resource "aws_security_group" "db_security_group" {
  name_prefix = "db_security_group"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port = 5432
    to_port   = 5432
    protocol  = "tcp"

    cidr_blocks = concat(module.vpc.database_subnets_cidr_blocks, module.vpc.private_subnets_cidr_blocks)
  }
}
