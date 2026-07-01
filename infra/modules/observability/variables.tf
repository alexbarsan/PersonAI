variable "name_prefix" {
  type        = string
  description = "Prefix used for observability resources."
}

variable "alert_email" {
  type        = string
  description = "Optional email address for alarms."
  default     = null
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to observability resources."
  default     = {}
}
