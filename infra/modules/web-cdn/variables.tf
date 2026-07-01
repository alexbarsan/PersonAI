variable "name_prefix" {
  type        = string
  description = "Prefix used for web resources."
}

variable "cloudfront_web_acl_arn" {
  type        = string
  description = "Optional CloudFront-scoped WAF ACL ARN."
  default     = null
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to web resources."
  default     = {}
}
