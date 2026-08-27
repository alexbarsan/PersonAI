output "kms_key_arn" {
  value       = aws_kms_key.app.arn
  description = "Application KMS key ARN."
}

output "secret_arns" {
  value       = { for name, secret in aws_secretsmanager_secret.app : name => secret.arn }
  description = "Secrets Manager placeholder ARNs keyed by logical secret name."
}

output "github_deploy_role_arn" {
  value       = aws_iam_role.github_deploy.arn
  description = "GitHub Actions OIDC deployment role ARN."
}

output "github_deploy_role_name" {
  value       = aws_iam_role.github_deploy.name
  description = "GitHub Actions OIDC deployment role name."
}

output "regional_waf_acl_arn" {
  value       = aws_wafv2_web_acl.regional.arn
  description = "Regional WAF ACL ARN for ALB association."
}
