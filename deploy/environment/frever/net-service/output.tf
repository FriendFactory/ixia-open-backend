output "docker_repository_url" {
  description = "URL of docker image repository"
  value       = aws_ecr_repository.ecr_repository.repository_url
}
