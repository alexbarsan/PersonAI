output "api_load_balancer_dns_name" {
  value       = module.api.load_balancer_dns_name
  description = "API load balancer DNS name."
}

output "api_load_balancer_zone_id" {
  value       = module.api.load_balancer_zone_id
  description = "API load balancer hosted zone id."
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

output "web_cloudfront_domain_name" {
  value       = module.web.cloudfront_domain_name
  description = "CloudFront distribution domain name."
}

output "web_cloudfront_hosted_zone_id" {
  value       = module.web.cloudfront_hosted_zone_id
  description = "CloudFront hosted zone id for Route 53 alias records."
}

output "github_deploy_role_arn" {
  value       = module.security.github_deploy_role_arn
  description = "GitHub Actions OIDC deployment role ARN."
}
