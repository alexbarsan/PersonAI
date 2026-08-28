locals {
  environment = "qa"
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
  vpc_cidr              = "10.25.0.0/16"
  availability_zones    = var.availability_zones
  public_subnet_cidrs   = ["10.25.0.0/24", "10.25.1.0/24"]
  private_subnet_cidrs  = ["10.25.10.0/24", "10.25.11.0/24"]
  database_subnet_cidrs = ["10.25.20.0/24", "10.25.21.0/24"]
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
  domain_prefix = var.cognito_domain_prefix
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
  secret_kms_key_arn   = module.security.kms_key_arn
  regional_waf_acl_arn = module.security.regional_waf_acl_arn
  certificate_arn      = var.api_acm_certificate_arn
  async_queue_arns     = [module.async_jobs.queue_arn]
  asset_bucket_arn     = module.private_assets.bucket_arn

  environment_variables = {
    ASPNETCORE_ENVIRONMENT              = "Production"
    ConnectionStrings__Host             = module.database.endpoint
    ConnectionStrings__Database         = module.database.database_name
    Authentication__Cognito__Region     = var.aws_region
    Authentication__Cognito__UserPoolId = module.cognito.user_pool_id
    Authentication__Cognito__Audience   = module.cognito.user_pool_client_id
    Authentication__Cognito__ClientId   = module.cognito.user_pool_client_id
    Cors__AllowedOrigins__0             = "https://qa.dreamdna.world"
    Jobs__QueueUrl                      = module.async_jobs.queue_url
    Jobs__Worker__Enabled               = "true"
    Jobs__EmbeddingBackfill__Enabled    = "false"
    Assets__BucketName                  = module.private_assets.bucket_name
  }

  secret_arns = {
    DeepSeek__ApiKey           = module.security.secret_arns["deepseek-api-key"]
    Encryption__LocalKeyBase64 = module.security.secret_arns["app-encryption-key"]
    Pseudonym__SecretBase64    = module.security.secret_arns["pseudonym-hmac-key"]
    Database__MasterUserJson   = module.database.master_user_secret_arn
  }

  tags = local.tags
}

module "database" {
  source = "../../modules/rds-postgres"

  name_prefix           = local.name_prefix
  vpc_id                = module.network.vpc_id
  database_subnet_ids   = module.network.database_subnet_ids
  allowed_cidr_blocks   = [module.network.vpc_cidr_block]
  instance_class        = "db.t4g.micro"
  allocated_storage_gb  = 50
  backup_retention_days = 7
  multi_az              = false
  deletion_protection   = false
  kms_key_arn           = module.security.kms_key_arn
  tags                  = local.tags
}

module "async_jobs" {
  source = "../../modules/async-jobs"

  name_prefix = local.name_prefix
  kms_key_arn = module.security.kms_key_arn
  tags        = local.tags
}

module "private_assets" {
  source = "../../modules/private-assets"

  name_prefix = local.name_prefix
  kms_key_arn = module.security.kms_key_arn
  tags        = local.tags
}

module "web" {
  source = "../../modules/web-cdn"

  name_prefix            = local.name_prefix
  cloudfront_web_acl_arn = var.cloudfront_web_acl_arn
  domain_aliases         = var.web_domain_aliases
  certificate_arn        = var.web_acm_certificate_arn
  tags                   = local.tags
}

module "observability" {
  source = "../../modules/observability"

  name_prefix = local.name_prefix
  alert_email = var.alert_email
  tags        = local.tags
}

resource "aws_route53_record" "web_ipv4" {
  for_each = toset(var.hosted_zone_id == null ? [] : var.web_domain_aliases)

  name    = each.value
  type    = "A"
  zone_id = var.hosted_zone_id

  alias {
    name                   = module.web.cloudfront_domain_name
    zone_id                = module.web.cloudfront_hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "web_ipv6" {
  for_each = toset(var.hosted_zone_id == null ? [] : var.web_domain_aliases)

  name    = each.value
  type    = "AAAA"
  zone_id = var.hosted_zone_id

  alias {
    name                   = module.web.cloudfront_domain_name
    zone_id                = module.web.cloudfront_hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "api_ipv4" {
  count = var.hosted_zone_id == null || var.api_domain_name == null ? 0 : 1

  name    = var.api_domain_name
  type    = "A"
  zone_id = var.hosted_zone_id

  alias {
    name                   = module.api.load_balancer_dns_name
    zone_id                = module.api.load_balancer_zone_id
    evaluate_target_health = true
  }
}

resource "aws_iam_role_policy" "github_deploy" {
  name = "${local.name_prefix}-app-deploy"
  role = module.security.github_deploy_role_name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "EcrAuth"
        Effect   = "Allow"
        Action   = ["ecr:GetAuthorizationToken"]
        Resource = "*"
      },
      {
        Sid    = "EcrPush"
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:BatchGetImage",
          "ecr:CompleteLayerUpload",
          "ecr:DescribeImages",
          "ecr:DescribeRepositories",
          "ecr:GetDownloadUrlForLayer",
          "ecr:InitiateLayerUpload",
          "ecr:PutImage",
          "ecr:UploadLayerPart"
        ]
        Resource = module.api.ecr_repository_arn
      },
      {
        Sid    = "EcsServiceDeploy"
        Effect = "Allow"
        Action = [
          "ecs:DescribeServices",
          "ecs:UpdateService"
        ]
        Resource = module.api.service_arn
      },
      {
        Sid    = "EcsTaskDefinitionDeploy"
        Effect = "Allow"
        Action = [
          "ecs:DescribeTaskDefinition",
          "ecs:RegisterTaskDefinition"
        ]
        Resource = "*"
      },
      {
        Sid    = "PassEcsTaskRoles"
        Effect = "Allow"
        Action = [
          "iam:PassRole"
        ]
        Resource = [
          module.api.task_execution_role_arn,
          module.api.task_role_arn
        ]
        Condition = {
          StringEquals = {
            "iam:PassedToService" = "ecs-tasks.amazonaws.com"
          }
        }
      },
      {
        Sid      = "WebBucketList"
        Effect   = "Allow"
        Action   = ["s3:ListBucket"]
        Resource = module.web.bucket_arn
      },
      {
        Sid    = "WebBucketObjects"
        Effect = "Allow"
        Action = [
          "s3:DeleteObject",
          "s3:GetObject",
          "s3:PutObject"
        ]
        Resource = "${module.web.bucket_arn}/*"
      },
      {
        Sid    = "CloudFrontInvalidation"
        Effect = "Allow"
        Action = [
          "cloudfront:CreateInvalidation",
          "cloudfront:GetDistribution"
        ]
        Resource = module.web.cloudfront_distribution_arn
      }
    ]
  })
}
