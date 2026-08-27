locals {
  environment = "domain"
  project     = "dreamlens"
  name_prefix = "${local.project}-${local.environment}"

  tags = {
    Project     = "DreamLens"
    Environment = local.environment
    ManagedBy   = "Terraform"
  }
}

module "domain" {
  source = "../../modules/domain"

  domain_name               = var.domain_name
  subject_alternative_names = var.subject_alternative_names
  tags                      = local.tags
}
