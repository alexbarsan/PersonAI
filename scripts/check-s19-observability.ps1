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

$k6Script = Join-Path $root 'tests\load\dream-smoke.js'
$adotConfig = Join-Path $root 'infra\adot\collector.yaml'
$observabilityModule = Join-Path $root 'infra\modules\observability\main.tf'
$apiProgram = Join-Path $root 'api\src\DreamLens.Api\Program.cs'
$observabilityDoc = Join-Path $root 'docs\observability.md'

Assert-Path $k6Script
Assert-Contains $k6Script '/health/live'
Assert-Contains $k6Script '/health/ready'
Assert-Contains $k6Script '/v1/dreams'
Assert-Contains $k6Script 'DREAMLENS_TEST_TOKEN'

Assert-Path $adotConfig
Assert-Contains $adotConfig 'awsxray'
Assert-Contains $adotConfig 'awsemf'
Assert-Contains $adotConfig 'otlp'

Assert-Contains $observabilityModule 'ai-cost'
Assert-Contains $observabilityModule 'quota-rejections'
Assert-Contains $observabilityModule 'provider-failures'
Assert-Contains $observabilityModule 'aws_cloudwatch_metric_alarm'

Assert-Contains $apiProgram 'UseDreamLensSecurityHeaders'
Assert-Contains $apiProgram 'AddDreamLensObservability'

Assert-Path $observabilityDoc
Assert-Contains $observabilityDoc 'personakit.ai.estimated_cost_usd'
Assert-Contains $observabilityDoc 'k6 run tests/load/dream-smoke.js'

Write-Host 'S19 observability and hardening check passed.'
