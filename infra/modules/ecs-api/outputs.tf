output "cluster_name" {
  value       = aws_ecs_cluster.this.name
  description = "ECS cluster name."
}

output "cluster_arn" {
  value       = aws_ecs_cluster.this.arn
  description = "ECS cluster ARN."
}

output "service_name" {
  value       = aws_ecs_service.api.name
  description = "ECS service name."
}

output "service_arn" {
  value       = aws_ecs_service.api.id
  description = "ECS service ARN."
}

output "load_balancer_dns_name" {
  value       = aws_lb.api.dns_name
  description = "API ALB DNS name."
}

output "load_balancer_zone_id" {
  value       = aws_lb.api.zone_id
  description = "API ALB hosted zone id for Route 53 alias records."
}

output "task_definition_family" {
  value       = aws_ecs_task_definition.api.family
  description = "API ECS task definition family."
}

output "task_execution_role_arn" {
  value       = aws_iam_role.task_execution.arn
  description = "API ECS task execution role ARN."
}

output "task_role_arn" {
  value       = aws_iam_role.task.arn
  description = "API ECS task role ARN."
}

output "task_security_group_id" {
  value       = aws_security_group.task.id
  description = "ECS task security group id."
}

output "ecr_repository_url" {
  value       = aws_ecr_repository.api.repository_url
  description = "API ECR repository URL."
}

output "ecr_repository_arn" {
  value       = aws_ecr_repository.api.arn
  description = "API ECR repository ARN."
}
