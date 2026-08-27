variable "aws_region" {
  type        = string
  description = "AWS region for domain resources. Use us-east-1 for CloudFront certificates."
}

variable "domain_name" {
  type        = string
  description = "Root public domain name."
}

variable "subject_alternative_names" {
  type        = list(string)
  description = "Additional names covered by the public ACM certificate."
  default     = []
}
