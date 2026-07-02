$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

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

Assert-Path (Join-Path $root 'api\src\DreamLens.Api\Infrastructure\Monetization\IEntitlementService.cs')
Assert-Path (Join-Path $root 'api\src\DreamLens.Api\Features\Entitlements\EntitlementEndpoints.cs')
Assert-Contains (Join-Path $root 'api\src\DreamLens.Api\Infrastructure\Quotas\EfDreamQuotaService.cs') 'IEntitlementService'
Assert-Contains (Join-Path $root 'api\src\DreamLens.Api\Program.cs') 'MapEntitlementEndpoints'
Assert-Contains (Join-Path $root 'app\src\features\paywall\PaywallScreen.tsx') 'Purchases not connected yet'
Assert-Contains (Join-Path $root 'app\src\api\client.ts') 'getEntitlements'
Assert-Contains (Join-Path $root 'docs\developer-manual.md') 'S21 can start without subscribing'

Write-Host 'S21 monetization foundation check passed.'
