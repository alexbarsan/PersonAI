variable "name_prefix" {
  type        = string
  description = "Prefix used for API resources."
}

variable "vpc_id" {
  type        = string
  description = "VPC id."
}

variable "public_subnet_ids" {
  type        = list(string)
  description = "Public subnet ids for the ALB."
}

variable "private_subnet_ids" {
  type        = list(string)
  description = "Private subnet ids for ECS tasks."
}

variable "container_image" {
  type        = string
  description = "API container image to deploy."
}

variable "container_port" {
  type        = number
  description = "API container port."
  default     = 8080
}

variable "task_cpu" {
  type        = number
  description = "Fargate task CPU units."
}

variable "task_memory" {
  type        = number
  description = "Fargate task memory in MB."
}

variable "desired_count" {
  type        = number
  description = "Desired ECS service task count."
}

variable "environment_variables" {
  type        = map(string)
  description = "Non-secret API environment variables."
  default     = {}
}

variable "secret_arns" {
  type        = map(string)
  description = "Secrets exposed to the task as environment variables."
  default     = {}
}

variable "secret_kms_key_arn" {
  type        = string
  description = "Optional KMS key ARN used to decrypt injected secrets."
  default     = null
}

variable "regional_waf_acl_arn" {
  type        = string
  description = "Regional WAF ACL ARN to attach to the API ALB."
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to API resources."
  default     = {}
}
