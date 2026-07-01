output "endpoint" {
  value       = aws_db_instance.this.endpoint
  description = "PostgreSQL endpoint."
}

output "database_name" {
  value       = aws_db_instance.this.db_name
  description = "Application database name."
}

output "security_group_id" {
  value       = aws_security_group.postgres.id
  description = "Database security group id."
}

output "master_user_secret_arn" {
  value       = aws_db_instance.this.master_user_secret[0].secret_arn
  description = "AWS-managed master user secret ARN."
}
