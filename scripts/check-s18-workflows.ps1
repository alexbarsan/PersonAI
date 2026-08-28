$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$workflowRoot = Join-Path $root '.github\workflows'

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

$requiredWorkflows = @(
    'api-deploy.yml',
    'web-deploy.yml',
    'infra.yml',
    'mobile-eas.yml'
)

foreach ($workflow in $requiredWorkflows) {
    Assert-Path (Join-Path $workflowRoot $workflow)
}

$apiWorkflow = Join-Path $workflowRoot 'api-deploy.yml'
Assert-Contains $apiWorkflow 'permissions:\s*(?s).*id-token:\s+write'
Assert-Contains $apiWorkflow 'aws-actions/configure-aws-credentials'
Assert-Contains $apiWorkflow 'aws-actions/amazon-ecr-login'
Assert-Contains $apiWorkflow 'docker/build-push-action'
Assert-Contains $apiWorkflow 'context:\s+\.'
Assert-Contains $apiWorkflow 'file:\s+api/src/DreamLens\.Api/Dockerfile'
Assert-Contains $apiWorkflow 'aws ecs describe-task-definition'
Assert-Contains $apiWorkflow 'aws-actions/amazon-ecs-render-task-definition'
Assert-Contains $apiWorkflow 'aws-actions/amazon-ecs-deploy-task-definition'
Assert-Contains $apiWorkflow 'dotnet test api/DreamLens\.sln --configuration Release'

$webWorkflow = Join-Path $workflowRoot 'web-deploy.yml'
Assert-Contains $webWorkflow 'permissions:\s*(?s).*id-token:\s+write'
Assert-Contains $webWorkflow 'aws-actions/configure-aws-credentials'
Assert-Contains $webWorkflow 'npm run build:web'
Assert-Contains $webWorkflow 'aws s3 sync'
Assert-Contains $webWorkflow 'aws cloudfront create-invalidation'

$infraWorkflow = Join-Path $workflowRoot 'infra.yml'
Assert-Contains $infraWorkflow 'permissions:\s*(?s).*id-token:\s+write'
Assert-Contains $infraWorkflow 'hashicorp/setup-terraform'
Assert-Contains $infraWorkflow '-\s+qa'
Assert-Contains $infraWorkflow 'terraform fmt -check -recursive infra'
Assert-Contains $infraWorkflow 'terraform -chdir=infra/envs/\$\{\{\s*inputs\.environment\s*\}\} plan'
Assert-Contains $infraWorkflow 'terraform -chdir=infra/envs/\$\{\{\s*inputs\.environment\s*\}\} apply'
Assert-Contains $infraWorkflow 'environment: \$\{\{\s*inputs\.environment\s*\}\}'

$mobileWorkflow = Join-Path $workflowRoot 'mobile-eas.yml'
Assert-Contains $mobileWorkflow 'expo/expo-github-action'
Assert-Contains $mobileWorkflow 'eas build'
Assert-Contains $mobileWorkflow 'EAS_TOKEN'

$deploymentDoc = Join-Path $root 'docs\deployment.md'
Assert-Path $deploymentDoc
Assert-Contains $deploymentDoc 'AWS_ROLE_TO_ASSUME'
Assert-Contains $deploymentDoc 'EAS_TOKEN'
Assert-Contains $deploymentDoc 'TERRAFORM_STATE_BUCKET'

Write-Host 'S18 workflow structure check passed.'
