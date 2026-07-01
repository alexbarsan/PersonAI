output "cluster_name" {
  value       = aws_ecs_cluster.this.name
  description = "ECS cluster name."
}

output "service_name" {
  value       = aws_ecs_service.api.name
  description = "ECS service name."
}

output "load_balancer_dns_name" {
  value       = aws_lb.api.dns_name
  description = "API ALB DNS name."
}

output "task_security_group_id" {
  value       = aws_security_group.task.id
  description = "ECS task security group id."
}

output "ecr_repository_url" {
  value       = aws_ecr_repository.api.repository_url
  description = "API ECR repository URL."
}
