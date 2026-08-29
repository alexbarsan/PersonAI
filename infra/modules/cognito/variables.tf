variable "name_prefix" {
  type        = string
  description = "Prefix used for Cognito resources."
}

variable "callback_urls" {
  type        = list(string)
  description = "Allowed OAuth callback URLs."
}

variable "logout_urls" {
  type        = list(string)
  description = "Allowed OAuth logout URLs."
}

variable "domain_prefix" {
  type        = string
  description = "Optional Cognito hosted UI domain prefix. Use a globally unique lowercase prefix per region."
  default     = null
}

variable "privacy_admin_group_name" {
  type        = string
  description = "Cognito group allowed to approve user anonymization requests."
  default     = "dreamlens-admin"
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to Cognito resources."
  default     = {}
}
