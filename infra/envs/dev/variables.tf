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

variable "cognito_domain_prefix" {
  type        = string
  description = "Optional Cognito hosted UI domain prefix."
  default     = null
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

variable "web_domain_aliases" {
  type        = list(string)
  description = "Optional custom domain aliases for the web CloudFront distribution."
  default     = []
}

variable "web_acm_certificate_arn" {
  type        = string
  description = "Optional us-east-1 ACM certificate ARN for the web CloudFront distribution."
  default     = null
}

variable "api_acm_certificate_arn" {
  type        = string
  description = "Optional regional ACM certificate ARN for the API ALB HTTPS listener."
  default     = null
}

variable "hosted_zone_id" {
  type        = string
  description = "Optional Route 53 hosted zone id used to create custom domain alias records."
  default     = null
}

variable "api_domain_name" {
  type        = string
  description = "Optional custom API domain name."
  default     = null
}

variable "premium_subjects" {
  type        = list(string)
  description = "Cognito user subjects granted the mock Premium entitlement in dev."
  default     = []
}
