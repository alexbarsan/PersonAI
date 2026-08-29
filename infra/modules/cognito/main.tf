resource "aws_cognito_user_pool" "this" {
  name = "${var.name_prefix}-users"

  auto_verified_attributes = ["email"]
  mfa_configuration        = "OPTIONAL"

  password_policy {
    minimum_length                   = 12
    require_lowercase                = true
    require_numbers                  = true
    require_symbols                  = true
    require_uppercase                = true
    temporary_password_validity_days = 7
  }

  software_token_mfa_configuration {
    enabled = true
  }

  username_attributes = ["email"]

  tags = var.tags
}

resource "aws_cognito_user_pool_client" "app" {
  name         = "${var.name_prefix}-app"
  user_pool_id = aws_cognito_user_pool.this.id

  allowed_oauth_flows                  = ["code"]
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_scopes                 = ["email", "openid", "profile"]
  callback_urls                        = var.callback_urls
  logout_urls                          = var.logout_urls
  prevent_user_existence_errors        = "ENABLED"
  supported_identity_providers         = ["COGNITO"]

  explicit_auth_flows = [
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_USER_SRP_AUTH"
  ]

  generate_secret = false
}

resource "aws_cognito_user_group" "privacy_admin" {
  name         = var.privacy_admin_group_name
  user_pool_id = aws_cognito_user_pool.this.id
  description  = "Approves DreamLens user anonymization requests."
}

resource "aws_cognito_user_pool_domain" "hosted_ui" {
  count = var.domain_prefix == null ? 0 : 1

  domain                = var.domain_prefix
  managed_login_version = 1
  user_pool_id          = aws_cognito_user_pool.this.id
}
