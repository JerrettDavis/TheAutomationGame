param(
    [string]$PlaytestRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\playtests")
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $PlaytestRoot)) {
    throw "Playtest root not found: $PlaytestRoot"
}

$sessions = foreach ($directory in Get-ChildItem -LiteralPath $PlaytestRoot -Directory | Sort-Object Name) {
    $evidencePath = Join-Path $directory.FullName "first-hours-evidence.json"
    $observationPath = Join-Path $directory.FullName "facilitator-observation.json"
    $evidence = if (Test-Path -LiteralPath $evidencePath) { Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json } else { $null }
    $observation = if (Test-Path -LiteralPath $observationPath) { Get-Content -LiteralPath $observationPath -Raw | ConvertFrom-Json } else { $null }
    [pscustomobject]@{
        Session = $directory.Name
        Complete = $null -ne $evidence
        Novice = if ($null -ne $observation) { [bool]$observation.vocabularyNovice } else { $null }
        NoActionHelp = if ($null -ne $observation) { -not [bool]$observation.actionDirectedFacilitatorHelp } else { $null }
        CausalAnswers = if ($null -ne $observation) { [int]$observation.causalAnswers } else { $null }
        Blocker = if ($null -ne $observation) { [string]$observation.primaryBlocker } else { "UNRECORDED" }
        WallMinutes = if ($null -ne $evidence) { [math]::Round([double]$evidence.wallClockSeconds / 60, 1) } else { $null }
        ActiveMinutes = if ($null -ne $evidence) { [math]::Round([double]$evidence.activeSimulationTicks / 600, 1) } else { $null }
        TrialAttempts = if ($null -ne $evidence) { [int]$evidence.shiftTrial.attempts } else { $null }
        HandbookOpens = if ($null -ne $evidence) { [int](($evidence.handbookVisits | Measure-Object -Property openCount -Sum).Sum) } else { $null }
    }
}

$sessions | Format-Table -AutoSize
$complete = @($sessions | Where-Object Complete)
$observed = @($sessions | Where-Object { $null -ne $_.Novice })
$novices = @($observed | Where-Object Novice).Count
$withoutHelp = @($observed | Where-Object NoActionHelp).Count
$causal = @($observed | Where-Object { $_.CausalAnswers -ge 2 }).Count
$commonBlocker = $observed |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.Blocker) -and $_.Blocker -ne "UNRECORDED" } |
    Group-Object Blocker | Sort-Object Count -Descending | Select-Object -First 1

Write-Host ""
Write-Host "First formative gate"
Write-Host ("Completed sessions: {0}/5 {1}" -f $complete.Count, $(if ($complete.Count -ge 5) { "PASS" } else { "PENDING" }))
Write-Host ("Vocabulary novices: {0}/2 {1}" -f $novices, $(if ($novices -ge 2) { "PASS" } else { "PENDING" }))
Write-Host ("Without action-directed help: {0}/5 {1}" -f $withoutHelp, $(if ($withoutHelp -ge 4) { "PASS" } else { "PENDING" }))
Write-Host ("At least 2/3 causal answers: {0}/5 {1}" -f $causal, $(if ($causal -ge 4) { "PASS" } else { "PENDING" }))
if ($observed.Count -lt 5) {
    Write-Host "Most common blocker: PENDING (five structured observations required)"
} elseif ($null -eq $commonBlocker) {
    Write-Host "Most common blocker: none recorded PASS"
} else {
    Write-Host ("Most common blocker: {0} ({1} sessions) {2}" -f $commonBlocker.Name, $commonBlocker.Count, $(if ($commonBlocker.Count -le 1) { "PASS" } else { "FAIL" }))
}
Write-Host "Duration envelope: REVIEW wall-clock distribution with the intended study target."
