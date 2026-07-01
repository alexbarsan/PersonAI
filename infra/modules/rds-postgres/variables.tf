variable "name_prefix" {
  type        = string
  description = "Prefix used for database resources."
}

variable "vpc_id" {
  type        = string
  description = "VPC id."
}

variable "database_subnet_ids" {
  type        = list(string)
  description = "Subnet ids for the RDS subnet group."
}

variable "allowed_security_group_ids" {
  type        = list(string)
  description = "Security groups allowed to connect to PostgreSQL."
  default     = []
}

variable "allowed_cidr_blocks" {
  type        = list(string)
  description = "CIDR blocks allowed to connect to PostgreSQL."
  default     = []
}

variable "engine_version" {
  type        = string
  description = "PostgreSQL engine version."
  default     = "16"
}

variable "instance_class" {
  type        = string
  description = "RDS instance class."
}

variable "allocated_storage_gb" {
  type        = number
  description = "Initial allocated storage in GB."
}

variable "backup_retention_days" {
  type        = number
  description = "Automated backup retention in days."
}

variable "multi_az" {
  type        = bool
  description = "Whether the database should run Multi-AZ."
}

variable "deletion_protection" {
  type        = bool
  description = "Whether deletion protection is enabled."
}

variable "kms_key_arn" {
  type        = string
  description = "KMS key ARN for storage encryption."
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to database resources."
  default     = {}
}
