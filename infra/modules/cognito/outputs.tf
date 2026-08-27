output "user_pool_id" {
  value       = aws_cognito_user_pool.this.id
  description = "Cognito user pool id."
}

output "user_pool_client_id" {
  value       = aws_cognito_user_pool_client.app.id
  description = "Cognito public app client id."
}

output "issuer_url" {
  value       = "https://cognito-idp.${data.aws_region.current.name}.amazonaws.com/${aws_cognito_user_pool.this.id}"
  description = "JWT issuer URL."
}

output "hosted_ui_domain" {
  value       = length(aws_cognito_user_pool_domain.hosted_ui) == 0 ? null : "https://${aws_cognito_user_pool_domain.hosted_ui[0].domain}.auth.${data.aws_region.current.name}.amazoncognito.com"
  description = "Cognito hosted UI base URL."
}

data "aws_region" "current" {}
