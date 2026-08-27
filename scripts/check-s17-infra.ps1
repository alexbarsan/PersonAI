$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$infra = Join-Path $root 'infra'

function Assert-Path {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing required path: $Path"
    }
}

function Assert-Contains {
    param(
        [string] $Path,
        [string] $Pattern
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) {
        throw "Expected '$Path' to contain pattern '$Pattern'."
    }
}

$requiredModules = @(
    'domain',
    'network',
    'security',
    'rds-postgres',
    'cognito',
    'ecs-api',
    'web-cdn',
    'observability'
)

$appEnvironmentModules = @(
    'network',
    'security',
    'rds-postgres',
    'cognito',
    'ecs-api',
    'web-cdn',
    'observability'
)

foreach ($module in $requiredModules) {
    $modulePath = Join-Path $infra "modules\$module"
    Assert-Path $modulePath
    Assert-Path (Join-Path $modulePath 'main.tf')
    Assert-Path (Join-Path $modulePath 'variables.tf')
    Assert-Path (Join-Path $modulePath 'outputs.tf')
}

foreach ($environment in @('domain')) {
    $envPath = Join-Path $infra "envs\$environment"
    Assert-Path $envPath
    Assert-Path (Join-Path $envPath 'versions.tf')
    Assert-Path (Join-Path $envPath 'main.tf')
    Assert-Path (Join-Path $envPath 'variables.tf')
    Assert-Path (Join-Path $envPath 'outputs.tf')
    Assert-Path (Join-Path $envPath 'backend.tf.example')
    Assert-Path (Join-Path $envPath 'terraform.tfvars.example')

    Assert-Contains (Join-Path $envPath 'main.tf') 'module\s+"domain"'
    Assert-Contains (Join-Path $envPath 'outputs.tf') 'hosted_zone_name_servers'
    Assert-Contains (Join-Path $envPath 'outputs.tf') 'certificate_arn'
}

foreach ($environment in @('dev', 'qa', 'prod')) {
    $envPath = Join-Path $infra "envs\$environment"
    Assert-Path $envPath
    Assert-Path (Join-Path $envPath 'versions.tf')
    Assert-Path (Join-Path $envPath 'main.tf')
    Assert-Path (Join-Path $envPath 'variables.tf')
    Assert-Path (Join-Path $envPath 'outputs.tf')
    Assert-Path (Join-Path $envPath 'backend.tf.example')
    Assert-Path (Join-Path $envPath 'terraform.tfvars.example')

    $main = Join-Path $envPath 'main.tf'
    foreach ($module in $appEnvironmentModules) {
        $moduleName = if ($module -eq 'rds-postgres') { 'database' } elseif ($module -eq 'ecs-api') { 'api' } elseif ($module -eq 'web-cdn') { 'web' } else { $module }
        Assert-Contains $main "module\s+`"$moduleName`""
    }

    Assert-Contains (Join-Path $envPath 'outputs.tf') 'github_deploy_role_arn'
    Assert-Contains (Join-Path $envPath 'outputs.tf') 'api_ecr_repository_url'
    Assert-Contains (Join-Path $envPath 'outputs.tf') 'web_cloudfront_distribution_id'
}

Assert-Contains (Join-Path $infra 'modules\network\main.tf') 'aws_vpc'
Assert-Contains (Join-Path $infra 'modules\ecs-api\main.tf') 'aws_ecs_service'
Assert-Contains (Join-Path $infra 'modules\ecs-api\main.tf') 'aws_lb'
Assert-Contains (Join-Path $infra 'modules\ecs-api\main.tf') '/health/ready'
Assert-Contains (Join-Path $infra 'modules\rds-postgres\main.tf') 'aws_db_instance'
Assert-Contains (Join-Path $infra 'modules\rds-postgres\main.tf') 'storage_encrypted\s+=\s+true'
Assert-Contains (Join-Path $infra 'modules\cognito\main.tf') 'aws_cognito_user_pool'
Assert-Contains (Join-Path $infra 'modules\web-cdn\main.tf') 'aws_cloudfront_distribution'
Assert-Contains (Join-Path $infra 'modules\web-cdn\main.tf') 'aws_s3_bucket'
Assert-Contains (Join-Path $infra 'modules\security\main.tf') 'aws_wafv2_web_acl'
Assert-Contains (Join-Path $infra 'modules\security\main.tf') 'aws_iam_openid_connect_provider'
Assert-Contains (Join-Path $infra 'modules\security\main.tf') 'aws_secretsmanager_secret'
Assert-Contains (Join-Path $infra 'modules\observability\main.tf') 'aws_cloudwatch_dashboard'
Assert-Contains (Join-Path $infra 'modules\domain\main.tf') 'aws_route53_zone'
Assert-Contains (Join-Path $infra 'modules\domain\main.tf') 'aws_acm_certificate'

Write-Host 'S17 Terraform infrastructure structure check passed.'
