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

$personaRoot = Join-Path $root 'personas\astrologer'
Assert-Path (Join-Path $personaRoot 'persona.json')
Assert-Path (Join-Path $personaRoot 'prompt.scriban')
Assert-Path (Join-Path $personaRoot 'output.schema.json')
Assert-Path (Join-Path $personaRoot 'section-map.json')
Assert-Contains (Join-Path $personaRoot 'persona.json') '"id"\s*:\s*"astrologer"'
Assert-Contains (Join-Path $personaRoot 'persona.json') '"displayName"\s*:\s*"Astra"'
Assert-Contains (Join-Path $personaRoot 'section-map.json') '"Focus Areas"'

$brand = Join-Path $root 'app\src\theme\brand.ts'
Assert-Contains $brand 'astraBrand'
Assert-Contains $brand 'DREAMLENS_APP_VARIANT'
Assert-Contains $brand 'astrologer'

Write-Host 'S20 Astra reuse check passed.'
