param(
    [string]$Root = (Resolve-Path "$PSScriptRoot\..").Path
)

$ErrorActionPreference = "Stop"

$requiredDirectories = @(
    "api",
    "app",
    "infra",
    "personas",
    "docs",
    "docs/plan",
    "scripts",
    ".github",
    ".github/workflows"
)

$requiredFiles = @(
    "README.md",
    ".gitattributes",
    ".gitignore",
    "SLICE-STATUS.md",
    "scripts/check-s0-structure.ps1",
    ".github/workflows/ci.yml",
    "docs/plan/readme.md",
    "docs/plan/decision-record.md",
    "docs/plan/00-overview.md",
    "docs/plan/01-backend-architecture.md",
    "docs/plan/02-frontend-architecture.md",
    "docs/plan/03-aws-infrastructure.md",
    "docs/plan/04-security-privacy.md",
    "docs/plan/05-testing-strategy.md",
    "docs/plan/06-dev-orchestrator.md",
    "docs/plan/07-slices-S0-S5.md",
    "docs/plan/08-slices-S6-S11.md",
    "docs/plan/09-slices-S12-S16.md",
    "docs/plan/10-slices-S17-S21.md",
    "docs/plan/11-runtime-prompts.md",
    "docs/plan/12-reuse-playbook.md"
)

$rootPlanFiles = @(
    "00-overview.md",
    "01-backend-architecture.md",
    "02-frontend-architecture.md",
    "03-aws-infrastructure.md",
    "04-security-privacy.md",
    "05-testing-strategy.md",
    "06-dev-orchestrator.md",
    "07-slices-S0-S5.md",
    "08-slices-S6-S11.md",
    "09-slices-S12-S16.md",
    "10-slices-S17-S21.md",
    "11-runtime-prompts.md",
    "12-reuse-playbook.md",
    "decision-record.md"
)

$failures = New-Object System.Collections.Generic.List[string]

foreach ($directory in $requiredDirectories) {
    $path = Join-Path $Root $directory
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        $failures.Add("Missing directory: $directory")
    }
}

foreach ($file in $requiredFiles) {
    $path = Join-Path $Root $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $failures.Add("Missing file: $file")
    }
}

foreach ($file in $rootPlanFiles) {
    $path = Join-Path $Root $file
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $failures.Add("Plan file should live under docs/plan, not repository root: $file")
    }
}

$readmePath = Join-Path $Root "README.md"
if (Test-Path -LiteralPath $readmePath -PathType Leaf) {
    $readme = Get-Content -Raw -Encoding UTF8 $readmePath
    if ($readme -notmatch "docs/plan/readme\.md") {
        $failures.Add("Root README.md must point to docs/plan/readme.md")
    }
}

$statusPath = Join-Path $Root "SLICE-STATUS.md"
if (Test-Path -LiteralPath $statusPath -PathType Leaf) {
    $status = Get-Content -Raw -Encoding UTF8 $statusPath
    if ($status -notmatch "\| S0 \| Done \|") {
        $failures.Add("SLICE-STATUS.md must mark S0 as Done")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "S0 structure check passed."
