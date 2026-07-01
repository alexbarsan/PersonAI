locals {
  environment = "dev"
  project     = "dreamlens"
  name_prefix = "${local.project}-${local.environment}"

  tags = {
    Project     = "DreamLens"
    Environment = local.environment
    ManagedBy   = "Terraform"
  }
}

module "network" {
  source = "../../modules/network"

  name_prefix           = local.name_prefix
  vpc_cidr              = "10.20.0.0/16"
  availability_zones    = var.availability_zones
  public_subnet_cidrs   = ["10.20.0.0/24", "10.20.1.0/24"]
  private_subnet_cidrs  = ["10.20.10.0/24", "10.20.11.0/24"]
  database_subnet_cidrs = ["10.20.20.0/24", "10.20.21.0/24"]
  tags                  = local.tags
}

module "security" {
  source = "../../modules/security"

  name_prefix       = local.name_prefix
  github_repository = var.github_repository
  secret_names = [
    "deepseek-api-key",
    "app-encryption-key",
    "pseudonym-hmac-key"
  ]
  tags = local.tags
}

module "cognito" {
  source = "../../modules/cognito"

  name_prefix   = local.name_prefix
  callback_urls = var.callback_urls
  logout_urls   = var.logout_urls
  tags          = local.tags
}

module "api" {
  source = "../../modules/ecs-api"

  name_prefix          = local.name_prefix
  vpc_id               = module.network.vpc_id
  public_subnet_ids    = module.network.public_subnet_ids
  private_subnet_ids   = module.network.private_subnet_ids
  container_image      = var.container_image
  task_cpu             = 512
  task_memory          = 1024
  desired_count        = 1
  regional_waf_acl_arn = module.security.regional_waf_acl_arn

  environment_variables = {
    ASPNETCORE_ENVIRONMENT     = "Production"
    ConnectionStrings__Host    = module.database.endpoint
    ConnectionStrings__Database = module.database.database_name
    Authentication__Issuer     = module.cognito.issuer_url
    Authentication__Audience   = module.cognito.user_pool_client_id
  }

  secret_arns = merge(module.security.secret_arns, {
    database-master-user = module.database.master_user_secret_arn
  })

  tags = local.tags
}

module "database" {
  source = "../../modules/rds-postgres"

  name_prefix           = local.name_prefix
  vpc_id                = module.network.vpc_id
  database_subnet_ids   = module.network.database_subnet_ids
  allowed_cidr_blocks   = [module.network.vpc_cidr_block]
  instance_class        = "db.t4g.micro"
  allocated_storage_gb  = 20
  backup_retention_days = 3
  multi_az              = false
  deletion_protection   = false
  kms_key_arn           = module.security.kms_key_arn
  tags                  = local.tags
}

module "web" {
  source = "../../modules/web-cdn"

  name_prefix            = local.name_prefix
  cloudfront_web_acl_arn = var.cloudfront_web_acl_arn
  tags                   = local.tags
}

module "observability" {
  source = "../../modules/observability"

  name_prefix = local.name_prefix
  alert_email = var.alert_email
  tags        = local.tags
}
