param(
    [string] $ProfileName = "dreamlens-dev",
    [string] $Region = "us-east-1"
)

$ErrorActionPreference = "Continue"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

function Resolve-CommandPath {
    param(
        [string] $Name,
        [string[]] $FallbackPaths = @()
    )

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    foreach ($path in $FallbackPaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

function Invoke-Version {
    param(
        [string] $Name,
        [string] $Path,
        [string[]] $Arguments
    )

    if (-not $Path) {
        Write-Host "[missing] $Name"
        return
    }

    Write-Host "[found] $Name -> $Path"
    try {
        & $Path @Arguments
    }
    catch {
        Write-Host "[error] $Name version check failed: $($_.Exception.Message)"
    }
}

$aws = Resolve-CommandPath "aws" @(
    "$env:LOCALAPPDATA\Programs\Amazon\AWSCLIV2\aws.exe"
)
$uv = Resolve-CommandPath "uv" @(
    "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\astral-sh.uv_Microsoft.Winget.Source_8wekyb3d8bbwe\uv.exe"
)
$terraform = Resolve-CommandPath "terraform" @(
    "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Hashicorp.Terraform_Microsoft.Winget.Source_8wekyb3d8bbwe\terraform.exe"
)
$k6 = Resolve-CommandPath "k6" @(
    "$env:ProgramFiles\k6\k6.exe"
)
$java = Resolve-CommandPath "java" @(
    "$env:ProgramFiles\Microsoft\jdk-17.0.20.101-hotspot\bin\java.exe"
)
$maestro = Resolve-CommandPath "maestro" @(
    "$env:LOCALAPPDATA\Programs\Maestro\bin\maestro.cmd"
)

Invoke-Version "AWS CLI" $aws @("--version")
Invoke-Version "uv" $uv @("--version")
Invoke-Version "Terraform" $terraform @("version")
Invoke-Version "k6" $k6 @("version")
Invoke-Version "Java" $java @("-version")
Invoke-Version "Maestro" $maestro @("--version")

if (-not $aws) {
    Write-Host "[skip] AWS Agent Toolkit validation requires AWS CLI."
    exit 1
}

Write-Host "[aws] Checking caller identity for profile '$ProfileName'."
& $aws sts get-caller-identity --profile $ProfileName
if ($LASTEXITCODE -ne 0) {
    Write-Host "[aws] Profile is not logged in. Run: aws login --region $Region --profile $ProfileName"
    exit $LASTEXITCODE
}

Write-Host "[aws] Checking Agent Toolkit skills."
$skillsJson = & $aws agent-toolkit list-available-skills --region $Region --profile $ProfileName --output json 2>&1
if ($LASTEXITCODE -ne 0) {
    $skillsJson
    exit $LASTEXITCODE
}

$skills = $skillsJson | ConvertFrom-Json
$skillNames = @($skills.skills | Select-Object -ExpandProperty name)
Write-Host "[aws] Available AWS skills: $($skillNames.Count)"
Write-Host "[aws] Sample: $(@($skillNames | Select-Object -First 10) -join ', ')"
