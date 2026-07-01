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

variable "tags" {
  type        = map(string)
  description = "Tags applied to Cognito resources."
  default     = {}
}
