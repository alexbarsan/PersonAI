variable "name_prefix" {
  type        = string
  description = "Prefix used for the private asset bucket."
}

variable "kms_key_arn" {
  type        = string
  description = "KMS key used to encrypt objects."
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to bucket resources."
  default     = {}
}
