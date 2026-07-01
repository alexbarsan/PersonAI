output "vpc_id" {
  value       = aws_vpc.this.id
  description = "VPC id."
}

output "public_subnet_ids" {
  value       = values(aws_subnet.public)[*].id
  description = "Public subnet ids."
}

output "private_subnet_ids" {
  value       = values(aws_subnet.private)[*].id
  description = "Private application subnet ids."
}

output "database_subnet_ids" {
  value       = values(aws_subnet.database)[*].id
  description = "Database subnet ids."
}

output "vpc_cidr_block" {
  value       = aws_vpc.this.cidr_block
  description = "VPC CIDR block."
}
