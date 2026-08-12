param(
    [Parameter(Mandatory = $true)]
    [string]$PlayerId,
    [string]$Configuration = "Release",
    [switch]$Windowed,
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$safePlayerId = ($PlayerId -replace '[^A-Za-z0-9_-]', '-').Trim('-')
if ([string]::IsNullOrWhiteSpace($safePlayerId)) { throw "PlayerId must contain at least one letter or number." }

$sessionId = "{0}-{1}" -f (Get-Date -Format "yyyyMMdd-HHmmss"), $safePlayerId
$sessionDirectory = Join-Path $repositoryRoot "artifacts\playtests\$sessionId"
$savePath = Join-Path $sessionDirectory "career.json"
$evidencePath = Join-Path $sessionDirectory "first-hours-evidence.json"
$debriefPath = Join-Path $sessionDirectory "facilitator-debrief.md"
$observationPath = Join-Path $sessionDirectory "facilitator-observation.json"
$executable = Join-Path $repositoryRoot "src\Automation.Client.Stride.Windows\bin\$Configuration\net10.0\Automation.Client.Stride.Windows.exe"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Client executable not found at $executable. Run dotnet build TheAutomationGame.sln -c $Configuration first."
}

New-Item -ItemType Directory -Path $sessionDirectory | Out-Null
$debrief = @"
# First-hours playtest: $sessionId

- Player background / vocabulary familiarity:
- Guidance and accessibility choices:
- Movement discovery (first attempt, coaching if any):
- Contextual interaction discovery (first attempt, coaching if any):
- Facilitator interventions (time, blocker, exact help):
- Attempts or retries:
- Observed blockers:
- Ignored UI / false assumptions:
- Predictions before consequential actions:

## Debrief

1. What constrained glass service, and which observed evidence supported that conclusion?
2. What did the panel say was true during the incident, and what was physically true?
3. Why were the captured replay and live reliability window stronger evidence than another happy-path run?
4. Would you trust the revised system during another rush? Why?
5. Before the Codex name reveal: what stayed the same, and what changed between the two stations?

- Meaningful bottleneck identified causally:
- Reported-versus-physical readiness understood:
- Replay/proof value articulated:
- Strategy shape expressed in ordinary language before naming:
- Critical UI/accessibility issues (code, summary, owner, fixed/backlog):
- Additional observations:
"@
[System.IO.File]::WriteAllText($debriefPath, $debrief)

$priorSave = [Environment]::GetEnvironmentVariable("AUTOMATION_SAVE_PATH", "Process")
$priorEvidence = [Environment]::GetEnvironmentVariable("AUTOMATION_PLAYTEST_EVIDENCE_PATH", "Process")
$priorSession = [Environment]::GetEnvironmentVariable("AUTOMATION_PLAYTEST_SESSION_ID", "Process")
$priorWindowed = [Environment]::GetEnvironmentVariable("AUTOMATION_WINDOWED", "Process")
try {
    [Environment]::SetEnvironmentVariable("AUTOMATION_SAVE_PATH", $savePath, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_EVIDENCE_PATH", $evidencePath, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_SESSION_ID", $sessionId, "Process")
    if ($Windowed) { [Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", "1", "Process") }
    else { [Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", $null, "Process") }
    $process = Start-Process -FilePath $executable -PassThru
}
finally {
    [Environment]::SetEnvironmentVariable("AUTOMATION_SAVE_PATH", $priorSave, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_EVIDENCE_PATH", $priorEvidence, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_SESSION_ID", $priorSession, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", $priorWindowed, "Process")
}

Write-Host "Playtest session $sessionId started. Close the game when the session is finished."
$process.WaitForExit()
if (Test-Path -LiteralPath $evidencePath) {
    Write-Host "Complete evidence: $evidencePath"
}
else {
    Write-Warning "The final quest was not completed; no evidence file was emitted. The career and debrief remain in $sessionDirectory"
}
Write-Host "Facilitator debrief: $debriefPath"

if (-not $NonInteractive) {
    function Read-YesNo([string]$Prompt) {
        while ($true) {
            $answer = (Read-Host "$Prompt [y/n]").Trim().ToLowerInvariant()
            if ($answer -in @("y", "yes")) { return $true }
            if ($answer -in @("n", "no")) { return $false }
            Write-Warning "Enter y or n."
        }
    }

    $vocabularyNovice = Read-YesNo "Was the player unfamiliar with the intended systems/programming vocabulary?"
    $movementDiscovered = Read-YesNo "Did the player discover movement without coaching?"
    $interactionDiscovered = Read-YesNo "Did the player discover contextual interaction without coaching?"
    $bottleneckIdentified = Read-YesNo "Did the player causally identify a meaningful bottleneck?"
    $readinessUnderstood = Read-YesNo "Did the player explain reported versus physical readiness?"
    $replayValue = Read-YesNo "Did the player articulate why replay/proof matters?"
    $strategyExpressed = Read-YesNo "Before naming, did the player express the stable decision slot and swappable routing choices?"
    $actionDirectedHelp = Read-YesNo "Did the facilitator tell the player which consequential action to take?"
    $primaryBlockerText = (Read-Host "Primary progression blocker code, or leave blank for none").Trim()
    $primaryBlocker = if ([string]::IsNullOrWhiteSpace($primaryBlockerText)) { $null } else { $primaryBlockerText }
    while ($true) {
        $issueCountText = Read-Host "How many critical UI/accessibility issues were observed? [0+]"
        $issueCount = 0
        if ([int]::TryParse($issueCountText, [ref]$issueCount) -and $issueCount -ge 0) { break }
        Write-Warning "Enter zero or a positive whole number."
    }
    $criticalIssues = @()
    for ($issueIndex = 1; $issueIndex -le $issueCount; $issueIndex++) {
        do { $code = (Read-Host "Issue $issueIndex stable code").Trim() } while ([string]::IsNullOrWhiteSpace($code))
        do { $summary = (Read-Host "Issue $issueIndex concise summary").Trim() } while ([string]::IsNullOrWhiteSpace($summary))
        do { $owner = (Read-Host "Issue $issueIndex owner (team/backlog session)").Trim() } while ([string]::IsNullOrWhiteSpace($owner))
        while ($true) {
            $dispositionText = (Read-Host "Issue $issueIndex disposition [fixed/backlog]").Trim().ToLowerInvariant()
            if ($dispositionText -in @("fixed", "backlog")) { break }
            Write-Warning "Enter fixed or backlog."
        }
        $criticalIssues += [ordered]@{
            code = $code
            summary = $summary
            owner = $owner
            disposition = if ($dispositionText -eq "fixed") { "Fixed" } else { "Backlog" }
        }
    }
    $observation = [ordered]@{
        schemaVersion = 2
        sessionId = $sessionId
        recordedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        participantKind = "Human"
        vocabularyNovice = $vocabularyNovice
        movementDiscoveredWithoutCoaching = $movementDiscovered
        interactionDiscoveredWithoutCoaching = $interactionDiscovered
        meaningfulBottleneckIdentifiedCausally = $bottleneckIdentified
        reportedVsPhysicalReadinessUnderstood = $readinessUnderstood
        replayProofValueArticulated = $replayValue
        strategyExpressedBeforeNaming = $strategyExpressed
        actionDirectedFacilitatorHelp = $actionDirectedHelp
        primaryProgressionBlocker = $primaryBlocker
        criticalIssues = $criticalIssues
    }
    [System.IO.File]::WriteAllText($observationPath, ($observation | ConvertTo-Json -Depth 5))
    Write-Host "Structured observation: $observationPath"
}
else {
    Write-Warning "NonInteractive mode does not create a facilitator observation. Complete it before cohort aggregation."
}
