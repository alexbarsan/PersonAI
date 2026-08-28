variable "name_prefix" {
  type        = string
  description = "Prefix used for asynchronous job resources."
}

variable "kms_key_arn" {
  type        = string
  description = "KMS key used to encrypt SQS messages."
}

variable "visibility_timeout_seconds" {
  type        = number
  description = "Visibility timeout for a claimed job."
  default     = 300
}

variable "max_receive_count" {
  type        = number
  description = "Maximum receives before a message moves to the DLQ."
  default     = 5
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to queue resources."
  default     = {}
}
