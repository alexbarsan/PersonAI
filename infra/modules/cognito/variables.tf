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

variable "metrics_admin_group_name" {
  type        = string
  description = "Cognito group allowed to read aggregated business metrics."
  default     = "dreamlens-metrics-admin"
}

variable "google_oauth" {
  type = object({
    client_id     = string
    client_secret = string
  })
  description = "Optional Google OAuth client registered for this Cognito user pool."
  default     = null
  sensitive   = true

  validation {
    condition = var.google_oauth == null || (
      trim(var.google_oauth.client_id) != "" && trim(var.google_oauth.client_secret) != ""
    )
    error_message = "google_oauth requires both a non-empty client_id and client_secret."
  }
}

variable "apple_oauth" {
  type = object({
    client_id   = string
    team_id     = string
    key_id      = string
    private_key = string
  })
  description = "Optional Sign in with Apple Services ID and private key registered for this Cognito user pool."
  default     = null
  sensitive   = true

  validation {
    condition = var.apple_oauth == null || alltrue([
      trim(var.apple_oauth.client_id) != "",
      trim(var.apple_oauth.team_id) != "",
      trim(var.apple_oauth.key_id) != "",
      trim(var.apple_oauth.private_key) != ""
    ])
    error_message = "apple_oauth requires client_id, team_id, key_id, and private_key."
  }
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to Cognito resources."
  default     = {}
}
