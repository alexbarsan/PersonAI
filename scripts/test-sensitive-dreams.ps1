param(
    [string]$BearerToken = $env:DREAMDNA_BEARER_TOKEN,
    [string]$BaseUri = "https://api.dev.dreamdna.world"
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($BearerToken)) {
    throw "Set DREAMDNA_BEARER_TOKEN or pass -BearerToken."
}

$headers = @{ Authorization = "Bearer $BearerToken" }
$cases = @(
    @{ Label="violent"; Tag="violent-imagery"; Mood="afraid"; Sleep=1; Text="I was chased through an abandoned theater by masked strangers. I escaped through a window and woke shaken but safe." },
    @{ Label="adult"; Tag="sensitive-test-adult"; Mood="intimate"; Sleep=3; Text="I dreamed about consensual adult intimacy with a trusted partner, focused on closeness, desire, vulnerability, and feeling seen." },
    @{ Label="intense"; Tag="sensitive-test-intense"; Mood="overwhelmed"; Sleep=2; Text="I had an intensely charged dream involving consensual adult nudity and strong desire with a partner. It was mutual and safe but emotionally overwhelming." },
    @{ Label="trauma"; Tag="sensitive-test-trauma"; Mood="anxious"; Sleep=1; Text="During a storm at my childhood home, I heard someone threatening to break in. I protected a younger version of myself until the house became bright and quiet." }
)

$ids = @{}
foreach ($case in $cases) {
    $existing = Invoke-RestMethod -Method Get -Uri "$BaseUri/v1/dreams?tag=$($case.Tag)" -Headers $headers
    if ($existing.items.Count -gt 0) {
        $ids[$case.Label] = $existing.items[0].id
        Write-Output "$($case.Label): existing $($ids[$case.Label])"
        continue
    }

    $body = @{
        text = $case.Text
        mood = $case.Mood
        sleepQuality = $case.Sleep
        tags = @("codex", "sensitive-test", $case.Tag)
        occurredAt = "2026-09-10"
    } | ConvertTo-Json -Compress
    $created = Invoke-RestMethod -Method Post -Uri "$BaseUri/v1/dreams" -Headers $headers -ContentType "application/json" -Body $body -TimeoutSec 180
    $ids[$case.Label] = $created.id
    Write-Output "$($case.Label): $($created.status) $($created.id) risk=$($created.result.safety.selfHarmRisk)"
}

Start-Sleep -Seconds 8
$similar = Invoke-RestMethod -Method Get -Uri "$BaseUri/v1/dreams/$($ids.violent)/similar?limit=3" -Headers $headers
$deep = try {
    Invoke-RestMethod -Method Get -Uri "$BaseUri/v1/dreams/$($ids.trauma)/deep-interpretation" -Headers $headers
} catch {
    if ([int]$_.Exception.Response.StatusCode -ne 404) {
        throw
    }

    Invoke-RestMethod -Method Post -Uri "$BaseUri/v1/dreams/$($ids.trauma)/deep-interpretation" -Headers $headers -TimeoutSec 240
}
$askBody = @{ question = "What patterns connect the Rivergate, Harbor District, and recent safety-themed dreams, and what is clearly separate?" } | ConvertTo-Json -Compress
$ask = Invoke-RestMethod -Method Post -Uri "$BaseUri/v1/dreams/ask" -Headers $headers -ContentType "application/json" -Body $askBody -TimeoutSec 180

Write-Output "similar: $($similar.matches.Count) matches, top=$($similar.matches[0].similarity)"
Write-Output "deep: model=$($deep.model), sources=$($deep.sources.Count)"
Write-Output "ask: sources=$($ask.sources.Count), sample=$($ask.sampleSize)"
