variable "name_prefix" {
  type        = string
  description = "Prefix used for observability resources."
}

variable "alert_email" {
  type        = string
  description = "Optional email address for alarms."
  default     = null
}

variable "error_rate_alarm_threshold" {
  type        = number
  description = "API 5xx alarm threshold over the evaluation window."
  default     = 5
}

variable "ai_cost_alarm_threshold_usd" {
  type        = number
  description = "Estimated AI cost alarm threshold in USD over the evaluation window."
  default     = 10
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to observability resources."
  default     = {}
}
