output "region" {
  value = var.region
}

output "docker_repository_url" {
  description = "URL of docker image repository"
  value       = aws_ecr_repository.ecr_repository.repository_url
}

output "ssl_certificate_arn" {
  value = data.aws_acm_certificate.issued.arn
}

output "cdn_domain" {
  value = "${var.env}.frever-content.com"
}
output "redis_host" {
  value = aws_elasticache_cluster.redis.cache_nodes[0].address
}

output "asset_copying_sqs_queue" {
  value = aws_sqs_queue.asset-copying-queue.id
}

output "video_conversion_job_creation_sqs_queue" {
  value = aws_sqs_queue.video_conversion_job_creation.id
}
output "video_conversion_job_completed_sqs_queue" {
  value = aws_sqs_queue.video_conversion_job_completed.id
}
output "media_converter_queue" {
  value = aws_media_convert_queue.media_converter_queue.id
}
