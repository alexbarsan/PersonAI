output "api_load_balancer_dns_name" {
  value       = module.api.load_balancer_dns_name
  description = "API load balancer DNS name."
}

output "api_ecr_repository_url" {
  value       = module.api.ecr_repository_url
  description = "API ECR repository URL."
}

output "cognito_user_pool_id" {
  value       = module.cognito.user_pool_id
  description = "Cognito user pool id."
}

output "cognito_user_pool_client_id" {
  value       = module.cognito.user_pool_client_id
  description = "Cognito user pool client id."
}

output "web_bucket_name" {
  value       = module.web.bucket_name
  description = "S3 web bucket name."
}

output "web_cloudfront_distribution_id" {
  value       = module.web.cloudfront_distribution_id
  description = "CloudFront distribution id."
}

output "github_deploy_role_arn" {
  value       = module.security.github_deploy_role_arn
  description = "GitHub Actions OIDC deployment role ARN."
}
