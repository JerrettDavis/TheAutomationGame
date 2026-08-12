param(
    [string]$PlaytestRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\playtests"),
    [string]$OutputPath,
    [string]$Configuration = "Release",
    [switch]$RequirePass
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $PlaytestRoot)) {
    throw "Playtest root not found: $PlaytestRoot"
}
$resolvedRoot = [System.IO.Path]::GetFullPath($PlaytestRoot, $repositoryRoot)
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = Join-Path $resolvedRoot "readiness-report.md" }
else { $OutputPath = [System.IO.Path]::GetFullPath($OutputPath, $repositoryRoot) }
$executable = Join-Path $repositoryRoot "src\Automation.Headless\bin\$Configuration\net10.0\Automation.Headless.exe"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Headless executable not found at $executable. Run dotnet build TheAutomationGame.sln -c $Configuration first."
}

$arguments = @("--summarize-first-hours", $resolvedRoot, "--output", $OutputPath)
if ($RequirePass) { $arguments += "--require-pass" }
& $executable @arguments
if ($LASTEXITCODE -ne 0) { throw "Readiness summarizer exited with code $LASTEXITCODE." }
Write-Host "Durable readiness report: $OutputPath"
