param(
    [Parameter(Mandatory = $true)]
    [string]$BearerToken,
    [string]$BaseUri = "https://api.dev.dreamdna.world"
)

$ErrorActionPreference = "Stop"
$headers = @{ Authorization = "Bearer $BearerToken" }
$marker = "codex-corpus-2026"

function New-Dream([string]$Text, [string]$Mood, [int]$SleepQuality, [string[]]$Tags, [datetime]$OccurredAt) {
    [pscustomobject]@{
        text = $Text
        mood = $Mood
        sleepQuality = $SleepQuality
        tags = @("codex", $marker) + $Tags
        occurredAt = $OccurredAt.ToString("yyyy-MM-dd")
    }
}

function Invoke-DreamRequest([object]$Dream) {
    $body = $Dream | ConvertTo-Json -Depth 4 -Compress
    Invoke-RestMethod -Method Post -Uri "$BaseUri/v1/dreams" -Headers $headers -ContentType "application/json" -Body $body -TimeoutSec 120
}

$existing = Invoke-RestMethod -Method Get -Uri "$BaseUri/v1/dreams?tag=$marker" -Headers $headers
if ($existing.items.Count -gt 0) {
    throw "The codex corpus marker already exists ($($existing.items.Count) dreams). Refusing to duplicate it."
}

$dreams = [System.Collections.Generic.List[object]]::new()

$rivergateScenes = @(
    "Mira held a brass lantern while the silver river rose through the tracks, and I missed the last train.",
    "Mira waited on the old bridge as lanterns floated upstream beneath the platform.",
    "Mira gave me a ticket with no destination while the river reflected a different conversation.",
    "the station clock stopped whenever I carried a lantern toward Mira on the bridge.",
    "we found a locked waiting room and heard a train bell under the silver water.",
    "Mira was signaling from the bridge while I ran to catch the last train.",
    "a conductor gave Mira a silver key and the river became a road under the station.",
    "Mira said the river remembered every missed train and we set a lantern on the platform.",
    "Mira laughed when the wooden bridge replaced the platform and a silent train passed below.",
    "we searched for a red scarf as lantern light led us to a tunnel beside the river.",
    "I found Mira asleep beside a lantern and the frozen river had become train tracks.",
    "I stopped to help Mira light a lantern and the river flooded the platform without getting us wet.",
    "Mira showed me a timetable where every departure was a memory carried under the bridge.",
    "Mira called from across the river and I arrived just as the last train vanished.",
    "Mira opened a hidden door behind the timetable onto a river path marked by lanterns."
)
for ($i = 0; $i -lt $rivergateScenes.Count; $i++) {
    $mood = @("anxious", "curious", "calm")[$i % 3]
    $sleep = if ($i % 4 -eq 0) { 2 } else { 3 }
    $dreams.Add((New-Dream "I was back at Rivergate Station at night. $($rivergateScenes[$i])" $mood $sleep @("rivergate", "mira", "recurring-place", "water") ((Get-Date "2026-03-02").AddDays($i * 9))))
}

$harborScenes = @(
    "Jonas and Maya waved from a ferry while I ran along the wet pier, late for a meeting.",
    "rain filled our shoes as Jonas waited with me for Maya at the ferry terminal.",
    "Maya's map led every street back to the ferry while Jonas checked his watch.",
    "I chased a ferry through heavy rain and Jonas called my name from the deck.",
    "Maya and Jonas planned a trip in a harbor cafe while I could not find my bag.",
    "I boarded the ferry with Jonas, but Maya was left on the foggy pier.",
    "Jonas led me through flooded streets to Maya, who held three tickets and a broken clock.",
    "Maya repaired a compass during a thunderstorm while Jonas tied the ferry to a streetlamp.",
    "I arrived early for once and felt relieved seeing Jonas and Maya on calm water.",
    "I lost Maya and Jonas in the market and ran toward a ferry bell carrying a suitcase of clocks."
)
for ($i = 0; $i -lt $harborScenes.Count; $i++) {
    $mood = if ($i -eq 8) { "relieved" } else { "anxious" }
    $sleep = if ($i % 3 -eq 0) { 2 } else { 3 }
    $dreams.Add((New-Dream "I was in the Harbor District. $($harborScenes[$i])" $mood $sleep @("harbor-district", "jonas", "maya", "lateness", "rain") ((Get-Date "2026-03-03").AddDays($i * 11))))
}

$glasshouseScenes = @(
    "Grandmother Elena gave me a small key and showed me orange trees growing inside while snow fell outside.",
    "Elena asked me to water the plants before sunrise and I found a key hidden in basil.",
    "Elena pruned an orange tree and said the missing key opened a room of remembered gardeners.",
    "I carried a cracked key to Elena while the windows glowed gold and she told me not to worry about being late.",
    "Elena and I ate oranges while rain tapped the roof, and I felt peaceful without finding the key.",
    "the Glasshouse became my childhood home when Elena opened the door with a brass key.",
    "Elena wrote names on seed packets, gave me an orange, and asked me to plant the key beneath a fern.",
    "Elena waited with a lantern and a basket of oranges, and I felt happy and protected from winter."
)
for ($i = 0; $i -lt $glasshouseScenes.Count; $i++) {
    $dreams.Add((New-Dream "I visited the old Glasshouse. $($glasshouseScenes[$i])" @("comforted", "happy")[$i % 2] 4 @("glasshouse", "elena", "grandmother", "garden", "key") ((Get-date "2026-03-07").AddDays($i * 14))))
}

$schoolScenes = @(
    "Mrs Vale said the exam had started, but every clock pointed to a different time.",
    "I arrived late for an exam and Mrs Vale gave me a blank paper while the corridor grew longer.",
    "Mrs Vale asked why I forgot my timetable, and every classroom door opened into the same corridor.",
    "my classmates became shadows and the school clock ticked louder than my footsteps.",
    "Mrs Vale calmly showed me the way back when I could not remember my name.",
    "I climbed endless stairs carrying an exam paper and worried I had missed the whole year.",
    "I reached Mrs Vale's classroom early, and the exam became a bright map."
)
for ($i = 0; $i -lt $schoolScenes.Count; $i++) {
    $mood = if ($i -eq 6) { "relieved" } else { "anxious" }
    $sleep = if ($i % 2 -eq 0) { 2 } else { 1 }
    $dreams.Add((New-Dream "I was back in the long school corridor looking for room 204. $($schoolScenes[$i])" $mood $sleep @("school", "mrs-vale", "exam", "lateness", "recurring-scenario") ((Get-Date "2026-03-04").AddDays($i * 13))))
}

$cabinScenes = @(
    "Snow covered the pines and a quiet eagle landed near the window, making me feel clear and rested.",
    "I lit a fire and watched an eagle circle above the snowy valley in peaceful silence.",
    "An eagle showed me a path from the cabin to a frozen lake, and I woke calm.",
    "Wind moved through the pines while the cabin fire would not go out, but I was not afraid.",
    "At sunrise an eagle crossed the orange sky and I decided I could take my time."
)
for ($i = 0; $i -lt $cabinScenes.Count; $i++) {
    $dreams.Add((New-Dream "I stayed alone at a cabin on North Ridge. $($cabinScenes[$i])" "calm" 5 @("north-ridge", "mountain-cabin", "solitude", "eagle") ((Get-Date "2026-04-05").AddDays($i * 17))))
}

$controls = @(
    @{ Text = "I repaired a satellite alone inside a silent space station. Earth looked green through the window, and a radio signal came from Jupiter."; Mood = "curious"; Tag = "space-station" },
    @{ Text = "I swam through an underwater city made of blue glass. An octopus librarian handed me a book about tides."; Mood = "delighted"; Tag = "underwater-city" },
    @{ Text = "I rode a red train across an empty desert where the sand sang. No familiar places or people appeared, only stars."; Mood = "wonder"; Tag = "desert-train" },
    @{ Text = "I baked a giant loaf of bread in a floating kitchen above the clouds. The oven timer spoke in riddles and I laughed."; Mood = "happy"; Tag = "floating-kitchen" },
    @{ Text = "I entered a museum of clocks where every room was a different century. A faceless guide showed me a clock made of ice."; Mood = "curious"; Tag = "clock-museum" }
)
for ($i = 0; $i -lt $controls.Count; $i++) {
    $dreams.Add((New-Dream $controls[$i].Text $controls[$i].Mood 4 @("control-group", $controls[$i].Tag) ((Get-Date "2026-05-10").AddDays($i * 19))))
}

if ($dreams.Count -ne 50) { throw "Expected 50 dreams but constructed $($dreams.Count)." }

$successes = [System.Collections.Generic.List[string]]::new()
$failures = [System.Collections.Generic.List[string]]::new()
for ($i = 0; $i -lt $dreams.Count; $i++) {
    try {
        $result = Invoke-DreamRequest $dreams[$i]
        $successes.Add($result.Id)
        Write-Output "Created $($i + 1)/50: $($result.Id)"
    }
    catch {
        $failures.Add("$($i + 1): $($_.Exception.Message)")
        Write-Output "Failed $($i + 1)/50"
    }
    Start-Sleep -Milliseconds 400
}

Write-Output "Finished: $($successes.Count) created, $($failures.Count) failed."
if ($failures.Count -gt 0) { $failures | Write-Output }
