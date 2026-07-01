variable "aws_region" {
  type        = string
  description = "AWS region for the environment."
}

variable "github_repository" {
  type        = string
  description = "GitHub repository in owner/name format."
}

variable "availability_zones" {
  type        = list(string)
  description = "Availability zones for network resources."
}

variable "container_image" {
  type        = string
  description = "Initial API container image. CI/CD replaces this in S18."
}

variable "callback_urls" {
  type        = list(string)
  description = "Cognito OAuth callback URLs."
}

variable "logout_urls" {
  type        = list(string)
  description = "Cognito OAuth logout URLs."
}

variable "alert_email" {
  type        = string
  description = "Optional alert email address."
  default     = null
}

variable "cloudfront_web_acl_arn" {
  type        = string
  description = "Optional CloudFront-scoped WAF ACL ARN."
  default     = null
}
