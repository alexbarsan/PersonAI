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

resource "aws_iam_role" "production_dns_manager" {
  count = var.production_account_id == null ? 0 : 1

  name = "${local.project}-prod-dns-manager"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Principal = {
        AWS = "arn:aws:iam::${var.production_account_id}:root"
      }
      Action = "sts:AssumeRole"
    }]
  })

  tags = local.tags
}

resource "aws_iam_role_policy" "production_dns_manager" {
  count = var.production_account_id == null ? 0 : 1

  name = "${local.project}-prod-dns-manager"
  role = aws_iam_role.production_dns_manager[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "ManageDreamDnaRecords"
        Effect = "Allow"
        Action = [
          "route53:ChangeResourceRecordSets",
          "route53:GetHostedZone",
          "route53:ListResourceRecordSets"
        ]
        Resource = "arn:aws:route53:::hostedzone/${module.domain.hosted_zone_id}"
      },
      {
        Sid      = "FindHostedZone"
        Effect   = "Allow"
        Action   = "route53:ListHostedZonesByName"
        Resource = "*"
      }
    ]
  })
}
