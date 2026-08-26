param(
    [ValidateSet("dev", "qa", "prod")]
    [string] $Environment = "dev",
    [string] $ProfileName = "dreamlens-dev"
)

$ErrorActionPreference = "Stop"

$terraform = Get-Command terraform -ErrorAction SilentlyContinue
if (-not $terraform) {
    $fallback = "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Hashicorp.Terraform_Microsoft.Winget.Source_8wekyb3d8bbwe\terraform.exe"
    if (Test-Path $fallback) {
        $terraform = [pscustomobject]@{ Source = $fallback }
    }
}

if (-not $terraform) {
    throw "Terraform was not found. Open a new terminal or reinstall Terraform."
}

$envDir = Join-Path "infra\envs" $Environment
if (-not (Test-Path $envDir)) {
    throw "Terraform environment directory not found: $envDir"
}

$backendPath = Join-Path $envDir "backend.tf"
if (-not (Test-Path $backendPath)) {
    throw "Missing $backendPath. Run scripts\bootstrap-terraform-state.ps1 with -WriteBackendFile first."
}

$tfvarsPath = Join-Path $envDir "terraform.tfvars"
if (-not (Test-Path $tfvarsPath)) {
    $examplePath = Join-Path $envDir "terraform.tfvars.example"
    Copy-Item $examplePath $tfvarsPath
    Write-Host "Created $tfvarsPath from example. Review github_repository before applying."
}

Push-Location $envDir
try {
    $env:AWS_PROFILE = $ProfileName
    & $terraform.Source init
    & $terraform.Source plan -var-file="terraform.tfvars"
}
finally {
    Pop-Location
}
