resource "aws_elasticache_cluster" "redis" {
  cluster_id           = "${var.env}-cache"
  engine               = "redis"
  node_type            = "cache.t2.medium"
  num_cache_nodes      = 1
  parameter_group_name = "default.redis6.x"
  engine_version       = "6.x"
  # Networking
  subnet_group_name  = module.vpc.elasticache_subnet_group_name
  security_group_ids = [aws_security_group.redis_security_group.id]
}

resource "aws_security_group" "redis_security_group" {
  name_prefix = "redis-security-group"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port = 6379
    to_port   = 6379
    protocol  = "tcp"

    cidr_blocks = ["0.0.0.0/0"]
  }
}
