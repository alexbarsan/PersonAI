variable "name_prefix" {
  type        = string
  description = "Prefix used for security resources."
}

variable "github_repository" {
  type        = string
  description = "GitHub repository in owner/name format allowed to assume deployment roles."
}

variable "secret_names" {
  type        = list(string)
  description = "Application secret placeholders to create in Secrets Manager."
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to security resources."
  default     = {}
}
