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
- Facilitator interventions (time, blocker, exact help):
- Attempts or retries:
- Observed blockers:

## Debrief

1. What constrained glass service, and which observed evidence supported that conclusion?
2. Which unwritten assumptions failed under delegation or automation?
3. Why were the captured replay and live reliability window stronger evidence than another happy-path run?

- Causally supported answers (0-3):
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
    $actionDirectedHelp = Read-YesNo "Did the facilitator tell the player which consequential action to take?"
    while ($true) {
        $causalText = Read-Host "How many debrief answers were causally supported? [0-3]"
        $causalAnswers = 0
        if ([int]::TryParse($causalText, [ref]$causalAnswers) -and $causalAnswers -ge 0 -and $causalAnswers -le 3) { break }
        Write-Warning "Enter a number from 0 through 3."
    }
    $primaryBlocker = (Read-Host "Primary blocker code, or leave blank for none").Trim()
    $observation = [ordered]@{
        schemaVersion = 1
        sessionId = $sessionId
        recordedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        finalOutcomeComplete = (Test-Path -LiteralPath $evidencePath)
        vocabularyNovice = $vocabularyNovice
        actionDirectedFacilitatorHelp = $actionDirectedHelp
        causalAnswers = $causalAnswers
        primaryBlocker = $primaryBlocker
    }
    [System.IO.File]::WriteAllText($observationPath, ($observation | ConvertTo-Json))
    Write-Host "Structured observation: $observationPath"
}
