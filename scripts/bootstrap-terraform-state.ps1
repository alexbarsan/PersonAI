param(
    [ValidateSet("dev", "qa", "prod")]
    [string] $Environment = "dev",
    [string] $ProfileName = "dreamlens-dev",
    [string] $Region = "us-east-1",
    [string] $Project = "dreamlens",
    [string] $AccountId = "",
    [switch] $WriteBackendFile
)

$ErrorActionPreference = "Stop"

$aws = Get-Command aws -ErrorAction SilentlyContinue
if (-not $aws) {
    $fallback = "$env:LOCALAPPDATA\Programs\Amazon\AWSCLIV2\aws.exe"
    if (Test-Path $fallback) {
        $aws = [pscustomobject]@{ Source = $fallback }
    }
}

if (-not $aws) {
    throw "AWS CLI was not found. Open a new terminal or reinstall AWS CLI v2."
}

if (-not $AccountId) {
    $identity = & $aws.Source sts get-caller-identity --profile $ProfileName | ConvertFrom-Json
    $AccountId = $identity.Account
}

$bucketName = "$Project-$Environment-tfstate-$AccountId-$Region".ToLowerInvariant()
$lockTableName = "$Project-$Environment-tflock"

Write-Host "Bootstrapping Terraform state for '$Environment'."
Write-Host "State bucket: $bucketName"
Write-Host "Lock table:   $lockTableName"

$existingBucket = & $aws.Source s3api head-bucket --bucket $bucketName --profile $ProfileName 2>$null
if ($LASTEXITCODE -ne 0) {
    if ($Region -eq "us-east-1") {
        & $aws.Source s3api create-bucket --bucket $bucketName --region $Region --profile $ProfileName | Out-Null
    }
    else {
        & $aws.Source s3api create-bucket --bucket $bucketName --region $Region --create-bucket-configuration LocationConstraint=$Region --profile $ProfileName | Out-Null
    }
}

& $aws.Source s3api put-bucket-versioning --bucket $bucketName --versioning-configuration Status=Enabled --profile $ProfileName | Out-Null
& $aws.Source s3api put-bucket-encryption --bucket $bucketName --server-side-encryption-configuration '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}' --profile $ProfileName | Out-Null
& $aws.Source s3api put-public-access-block --bucket $bucketName --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true --profile $ProfileName | Out-Null

$table = & $aws.Source dynamodb describe-table --table-name $lockTableName --region $Region --profile $ProfileName 2>$null
if ($LASTEXITCODE -ne 0) {
    & $aws.Source dynamodb create-table `
        --table-name $lockTableName `
        --attribute-definitions AttributeName=LockID,AttributeType=S `
        --key-schema AttributeName=LockID,KeyType=HASH `
        --billing-mode PAY_PER_REQUEST `
        --region $Region `
        --profile $ProfileName | Out-Null

    & $aws.Source dynamodb wait table-exists --table-name $lockTableName --region $Region --profile $ProfileName
}

if ($WriteBackendFile) {
    $envDir = Join-Path "infra\envs" $Environment
    if (-not (Test-Path $envDir)) {
        throw "Terraform environment directory not found: $envDir"
    }

    $backendPath = Join-Path $envDir "backend.tf"
    $backend = @"
terraform {
  backend "s3" {
    bucket         = "$bucketName"
    key            = "$Project/$Environment/terraform.tfstate"
    region         = "$Region"
    dynamodb_table = "$lockTableName"
    encrypt        = true
  }
}
"@
    Set-Content -Path $backendPath -Value $backend -NoNewline
    Write-Host "Wrote $backendPath"
}

Write-Host "Terraform state bootstrap complete."
