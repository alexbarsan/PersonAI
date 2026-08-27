variable "name_prefix" {
  type        = string
  description = "Prefix used for web resources."
}

variable "cloudfront_web_acl_arn" {
  type        = string
  description = "Optional CloudFront-scoped WAF ACL ARN."
  default     = null
}

variable "domain_aliases" {
  type        = list(string)
  description = "Optional custom domain aliases for the CloudFront distribution."
  default     = []
}

variable "certificate_arn" {
  type        = string
  description = "Optional ACM certificate ARN in us-east-1 for CloudFront custom domains."
  default     = null

  validation {
    condition     = length(var.domain_aliases) == 0 || (var.certificate_arn != null && var.certificate_arn != "")
    error_message = "certificate_arn is required when domain_aliases is not empty."
  }
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to web resources."
  default     = {}
}
