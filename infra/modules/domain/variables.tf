variable "domain_name" {
  type        = string
  description = "Root public domain name."
}

variable "subject_alternative_names" {
  type        = list(string)
  description = "Additional names covered by the public ACM certificate."
  default     = []
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to domain resources."
  default     = {}
}
