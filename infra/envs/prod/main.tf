locals {
  environment = "prod"
  project     = "dreamlens"
  name_prefix = "${local.project}-${local.environment}"

  tags = {
    Project     = "DreamLens"
    Environment = local.environment
    ManagedBy   = "Terraform"
  }

  certificate_domains = distinct(concat(var.web_domain_aliases, compact([var.api_domain_name])))
}

resource "aws_acm_certificate" "public" {
  domain_name               = local.certificate_domains[0]
  subject_alternative_names = slice(local.certificate_domains, 1, length(local.certificate_domains))
  validation_method         = "DNS"

  lifecycle {
    create_before_destroy = true
  }

  tags = local.tags
}

resource "aws_route53_record" "certificate_validation" {
  provider = aws.dns

  for_each = {
    for option in aws_acm_certificate.public.domain_validation_options : option.domain_name => {
      name   = option.resource_record_name
      record = option.resource_record_value
      type   = option.resource_record_type
    }
  }

  allow_overwrite = true
  name            = each.value.name
  records         = [each.value.record]
  ttl             = 60
  type            = each.value.type
  zone_id         = var.hosted_zone_id
}

resource "aws_acm_certificate_validation" "public" {
  certificate_arn         = aws_acm_certificate.public.arn
  validation_record_fqdns = [for record in aws_route53_record.certificate_validation : record.fqdn]
}

resource "aws_ses_domain_identity" "premium_email" {
  domain = "dreamdna.world"
}

resource "aws_route53_record" "ses_verification" {
  provider = aws.dns

  allow_overwrite = true
  name            = "_amazonses.dreamdna.world"
  records         = [aws_ses_domain_identity.premium_email.verification_token]
  ttl             = 600
  type            = "TXT"
  zone_id         = var.hosted_zone_id
}

resource "aws_ses_domain_dkim" "premium_email" {
  domain = aws_ses_domain_identity.premium_email.domain
}

resource "aws_route53_record" "ses_dkim" {
  provider = aws.dns

  count = 3

  allow_overwrite = true
  name            = "${aws_ses_domain_dkim.premium_email.dkim_tokens[count.index]}._domainkey.dreamdna.world"
  records         = ["${aws_ses_domain_dkim.premium_email.dkim_tokens[count.index]}.dkim.amazonses.com"]
  ttl             = 600
  type            = "CNAME"
  zone_id         = var.hosted_zone_id
}

module "network" {
  source = "../../modules/network"

  name_prefix           = local.name_prefix
  vpc_cidr              = "10.30.0.0/16"
  availability_zones    = var.availability_zones
  public_subnet_cidrs   = ["10.30.0.0/24", "10.30.1.0/24"]
  private_subnet_cidrs  = ["10.30.10.0/24", "10.30.11.0/24"]
  database_subnet_cidrs = ["10.30.20.0/24", "10.30.21.0/24"]
  tags                  = local.tags
}

module "security" {
  source = "../../modules/security"

  name_prefix       = local.name_prefix
  github_repository = var.github_repository
  secret_names = [
    "deepseek-api-key",
    "openai-api-key",
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

  name_prefix            = local.name_prefix
  vpc_id                 = module.network.vpc_id
  public_subnet_ids      = module.network.public_subnet_ids
  private_subnet_ids     = module.network.private_subnet_ids
  container_image        = var.container_image
  task_cpu               = 1024
  task_memory            = 2048
  desired_count          = 2
  worker_desired_count   = 1
  worker_max_count       = 4
  secret_kms_key_arn     = module.security.kms_key_arn
  regional_waf_acl_arn   = module.security.regional_waf_acl_arn
  certificate_arn        = aws_acm_certificate_validation.public.certificate_arn
  enable_https_listener  = true
  async_queue_arns       = [module.async_jobs.queue_arn, module.async_jobs.dead_letter_queue_arn]
  async_queue_name       = module.async_jobs.queue_name
  asset_bucket_arn       = module.private_assets.bucket_arn
  ses_email_identity_arn = aws_ses_domain_identity.premium_email.arn
  enable_ses_email       = true

  environment_variables = {
    ASPNETCORE_ENVIRONMENT                            = "Production"
    ConnectionStrings__Host                           = module.database.endpoint
    ConnectionStrings__Database                       = module.database.database_name
    Database__ApplyMigrations                         = "true"
    Embedding__Enabled                                = "true"
    Embedding__Provider                               = "bedrock-titan"
    Embedding__Model                                  = "amazon.titan-embed-text-v2:0"
    Embedding__Dimensions                             = "1024"
    Embedding__Version                                = "3"
    Embedding__InputCostPerMillionTokensUsd           = "0.02"
    DeepSeek__Model                                   = "deepseek-v4-flash"
    ChatUsageCost__InputCostPerMillionTokens          = "0.44"
    ChatUsageCost__OutputCostPerMillionTokens         = "1.32"
    ChatResilience__Timeout                           = "00:02:30"
    DeepInterpretation__Enabled                       = "true"
    DeepInterpretation__Model                         = "deepseek-v4-pro"
    DeepInterpretation__DailyLimit                    = "3"
    DeepInterpretation__RetrievalLimit                = "5"
    DeepInterpretation__MaxOutputTokens               = "4096"
    DeepInterpretation__InputCostPerMillionTokensUsd  = "1.32"
    DeepInterpretation__OutputCostPerMillionTokensUsd = "3.96"
    JournalSynthesis__Enabled                         = "true"
    JournalSynthesis__Model                           = "deepseek-v4-pro"
    JournalSynthesis__MinimumCompletedDreams          = "6"
    Authentication__Cognito__Region                   = var.aws_region
    Authentication__Cognito__UserPoolId               = module.cognito.user_pool_id
    Authentication__Cognito__Audience                 = module.cognito.user_pool_client_id
    Authentication__Cognito__ClientId                 = module.cognito.user_pool_client_id
    Cors__AllowedOrigins__0                           = "https://dreamdna.world"
    FriendsAndFamily__AdministratorEmails__0          = "ai.ro.dodoloata@gmail.com"
    PremiumGrantEmail__Enabled                        = "true"
    PremiumGrantEmail__FromAddress                    = "hello@dreamdna.world"
    PremiumGrantEmail__FromName                       = "Dream DNA"
    PremiumGrantEmail__ReplyToAddress                 = "hello@dreamdna.world"
    Jobs__QueueUrl                                    = module.async_jobs.queue_url
    Jobs__Worker__Enabled                             = "true"
    Jobs__EmbeddingBackfill__Enabled                  = "false"
    Assets__BucketName                                = module.private_assets.bucket_name
    ImageGeneration__Free__Enabled                    = "false"
    ImageGeneration__Premium__Enabled                 = "false"
    ImageGeneration__FreeDailyLimit                   = "1"
    ImageGeneration__PremiumDailyLimit                = "5"
    VoiceTranscription__Enabled                       = "false"
    VoiceTranscription__Provider                      = "amazon-transcribe"
    VoiceTranscription__Model                         = "amazon-transcribe-standard"
    VoiceTranscription__DailyLimit                    = "3"
    VoiceTranscription__MaxDurationSeconds            = "180"
    VoiceTranscription__MaxUploadBytes                = "10485760"
    VoiceTranscription__EstimatedCostPerSecondUsd     = "0.0001"
  }

  secret_arns = {
    DeepSeek__ApiKey           = module.security.secret_arns["deepseek-api-key"]
    OpenAI__ApiKey             = module.security.secret_arns["openai-api-key"]
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
  instance_class        = "db.t4g.small"
  allocated_storage_gb  = 100
  backup_retention_days = 14
  multi_az              = true
  deletion_protection   = true
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
  certificate_arn        = aws_acm_certificate_validation.public.certificate_arn
  tags                   = local.tags
}

module "observability" {
  source = "../../modules/observability"

  name_prefix = local.name_prefix
  alert_email = var.alert_email
  tags        = local.tags
}

resource "aws_route53_record" "web_ipv4" {
  provider = aws.dns

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
  provider = aws.dns

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
  provider = aws.dns

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

resource "aws_iam_role_policy_attachment" "github_terraform_view" {
  role       = module.security.github_deploy_role_name
  policy_arn = "arn:aws:iam::aws:policy/job-function/ViewOnlyAccess"
}

resource "aws_iam_role_policy" "github_terraform_refresh" {
  name = "${local.name_prefix}-terraform-refresh"
  role = module.security.github_deploy_role_name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid    = "RefreshManagedInfrastructure"
      Effect = "Allow"
      Action = [
        "acm:DescribeCertificate",
        "acm:ListTagsForCertificate",
        "application-autoscaling:DescribeScalableTargets",
        "application-autoscaling:DescribeScalingPolicies",
        "cloudfront:GetOriginAccessControl",
        "cognito-idp:DescribeUserPoolClient",
        "cognito-idp:DescribeUserPoolDomain",
        "cognito-idp:DescribeUserPool",
        "cognito-idp:GetGroup",
        "cognito-idp:GetUserPoolMfaConfig",
        "cloudwatch:DescribeAlarms",
        "ec2:DescribeAddressesAttribute",
        "ecr:ListTagsForResource",
        "elasticloadbalancing:DescribeListenerAttributes",
        "elasticloadbalancing:DescribeTags",
        "elasticloadbalancing:DescribeTargetGroupAttributes",
        "iam:GetOpenIDConnectProvider",
        "iam:GetRole",
        "iam:GetRolePolicy",
        "kms:DescribeKey",
        "kms:GetKeyPolicy",
        "kms:GetKeyRotationStatus",
        "kms:ListAliases",
        "rds:ListTagsForResource",
        "s3:GetBucketAcl",
        "s3:GetBucketCORS",
        "s3:GetBucketEncryption",
        "s3:GetEncryptionConfiguration",
        "s3:GetBucketIntelligentTieringConfiguration",
        "s3:GetBucketLifecycleConfiguration",
        "s3:GetLifecycleConfiguration",
        "s3:GetBucketLocation",
        "s3:GetBucketLogging",
        "s3:GetBucketNotification",
        "s3:GetBucketObjectLockConfiguration",
        "s3:GetBucketOwnershipControls",
        "s3:GetBucketPolicy",
        "s3:GetBucketPolicyStatus",
        "s3:GetBucketPublicAccessBlock",
        "s3:GetBucketReplication",
        "s3:GetReplicationConfiguration",
        "s3:GetBucketRequestPayment",
        "s3:GetBucketTagging",
        "s3:GetBucketVersioning",
        "s3:GetBucketWebsite",
        "s3:GetAccelerateConfiguration",
        "s3:ListAllMyBuckets",
        "ses:GetIdentityDkimAttributes",
        "ses:GetIdentityVerificationAttributes",
        "sns:GetTopicAttributes",
        "wafv2:GetWebACL",
        "wafv2:GetWebACLForResource"
      ]
      Resource = "*"
      }, {
      Sid      = "AssumeDnsManagementRole"
      Effect   = "Allow"
      Action   = ["sts:AssumeRole"]
      Resource = var.dns_management_role_arn
      }, {
      Sid    = "ReadManagedSecretMetadata"
      Effect = "Allow"
      Action = [
        "secretsmanager:DescribeSecret",
        "secretsmanager:GetResourcePolicy",
        "secretsmanager:ListSecretVersionIds",
        "secretsmanager:ListTagsForResource"
      ]
      Resource = "arn:aws:secretsmanager:${var.aws_region}:097079438907:secret:${local.name_prefix}/*"
    }]
  })
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
        Resource = compact([module.api.service_arn, module.api.worker_service_arn])
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

resource "aws_iam_role_policy" "github_terraform_state" {
  name = "${local.name_prefix}-terraform-state"
  role = module.security.github_deploy_role_name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "TerraformStateBucket"
        Effect   = "Allow"
        Action   = ["s3:ListBucket"]
        Resource = "arn:aws:s3:::dreamlens-prod-tfstate-097079438907-us-east-1"
      },
      {
        Sid      = "TerraformStateObject"
        Effect   = "Allow"
        Action   = ["s3:GetObject", "s3:PutObject"]
        Resource = "arn:aws:s3:::dreamlens-prod-tfstate-097079438907-us-east-1/dreamlens/prod/terraform.tfstate"
      },
      {
        Sid    = "TerraformStateLock"
        Effect = "Allow"
        Action = [
          "dynamodb:DescribeTable",
          "dynamodb:DeleteItem",
          "dynamodb:GetItem",
          "dynamodb:PutItem"
        ]
        Resource = "arn:aws:dynamodb:${var.aws_region}:097079438907:table/dreamlens-prod-tflock"
      }
    ]
  })
}
