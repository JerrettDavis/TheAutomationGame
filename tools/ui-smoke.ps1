param(
    [switch]$KeepOpen,
    [switch]$AllowDesktopInput,
    [string]$Configuration = "Release",
    [string]$RetainScreenshotsPath
)

$ErrorActionPreference = "Stop"
if (-not $AllowDesktopInput) {
    throw "This test takes exclusive control of the shared OS cursor and opens/resizes windows. Close active work and rerun with -AllowDesktopInput only when the desktop is idle."
}
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $repositoryRoot "src\Automation.Client.Stride.Windows\bin\$Configuration\net10.0\Automation.Client.Stride.Windows.exe"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Client executable not found at $executable. Build the solution first."
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class NativeGameDriver
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr handle, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr handle, ref POINT point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr handle, int attribute, out RECT rect, int size);
    [DllImport("dwmapi.dll")] public static extern int DwmFlush();
}
'@
[NativeGameDriver]::SetProcessDPIAware() | Out-Null

function Wait-ForWindow([System.Diagnostics.Process]$Process, [int]$TimeoutSeconds = 15) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Client exited before creating its window." }
        if ($Process.MainWindowHandle -ne 0) { return }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out waiting for the client window."
}

function Wait-ForStage([System.Diagnostics.Process]$Process, [string]$Stage, [int]$TimeoutSeconds = 20) {
    $needle = "[stage=$Stage]"
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Client exited while waiting for $needle." }
        if ($Process.MainWindowTitle.Contains($needle, [StringComparison]::Ordinal)) { return }
        Start-Sleep -Milliseconds 50
    }
    throw "Timed out waiting for $needle. Last title: $($Process.MainWindowTitle)"
}

function Wait-ForTitle([System.Diagnostics.Process]$Process, [string]$Text, [int]$TimeoutSeconds = 10) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Client exited while waiting for title text '$Text'." }
        if ($Process.MainWindowTitle.Contains($Text, [StringComparison]::OrdinalIgnoreCase)) { return }
        Start-Sleep -Milliseconds 50
    }
    throw "Timed out waiting for '$Text'. Last title: $($Process.MainWindowTitle)"
}

function Wait-ForUiScale([System.Diagnostics.Process]$Process, [double]$Minimum, [int]$TimeoutSeconds = 10) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Client exited while waiting for UI scale $Minimum." }
        if ($Process.MainWindowTitle -match '\[ui=([0-9.]+)\]' -and [double]$Matches[1] -ge $Minimum) { return }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out waiting for UI scale >= $Minimum. Last title: $($Process.MainWindowTitle)"
}

function Send-GameControl([System.Diagnostics.Process]$Process, [string]$Control) {
    $Process.Refresh()
    if ($Process.HasExited) { throw "Client exited before control '$Control'." }
    $script:controlSequence++
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        try {
            [System.IO.File]::WriteAllText($script:controlFile, "$($script:controlSequence)|$Control")
            break
        }
        catch [System.IO.IOException] {
            if ($attempt -eq 20) { throw }
            Start-Sleep -Milliseconds 15
        }
    }
    Start-Sleep -Milliseconds 120
}

function Send-ControlUntilTitle([System.Diagnostics.Process]$Process, [string]$Control, [string]$Text) {
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Send-GameControl $Process $Control
        try {
            Wait-ForTitle $Process $Text 2
            return
        }
        catch {
            if ($attempt -eq 3) { throw }
        }
    }
}

function Send-ControlUntilStage([System.Diagnostics.Process]$Process, [string]$Control, [string]$Stage) {
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Send-GameControl $Process $Control
        try {
            Wait-ForStage $Process $Stage 2
            return
        }
        catch {
            if ($attempt -eq 3) { throw }
        }
    }
}

function Move-GamePointer([System.Diagnostics.Process]$Process, [double]$VirtualX, [double]$VirtualY, [switch]$Click) {
    $Process.Refresh()
    if ($Process.MainWindowTitle -notmatch '\[viewport=(\d+)x(\d+)\]') {
        throw "Could not read viewport transform from title: $($Process.MainWindowTitle)"
    }
    $viewportWidth = [double]$Matches[1]
    $viewportHeight = [double]$Matches[2]
    $scale = [Math]::Max(0.5, [Math]::Min($viewportWidth / 1024.0, $viewportHeight / 600.0))
    $origin = New-Object NativeGameDriver+POINT
    if (-not [NativeGameDriver]::ClientToScreen($Process.MainWindowHandle, [ref]$origin)) {
        throw "Could not resolve the game client origin."
    }
    $canvasX = ($viewportWidth - 1024 * $scale) * 0.5
    $canvasY = ($viewportHeight - 600 * $scale) * 0.5
    $screenX = [int][Math]::Round($origin.X + $canvasX + $VirtualX * $scale)
    $screenY = [int][Math]::Round($origin.Y + $canvasY + $VirtualY * $scale)
    [NativeGameDriver]::SetCursorPos($screenX, $screenY) | Out-Null
    Start-Sleep -Milliseconds 150
    if ($Click) {
        [NativeGameDriver]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
        # Hold through at least one client update so IsMouseButtonPressed observes
        # the transition even when the compositor and driver frames are offset.
        Start-Sleep -Milliseconds 60
        [NativeGameDriver]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 150
    }
}

function Click-UntilTitle(
    [System.Diagnostics.Process]$Process,
    [double]$VirtualX,
    [double]$VirtualY,
    [string]$ExpectedTitle,
    [int]$Attempts = 4
) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        Move-GamePointer $Process $VirtualX $VirtualY -Click
        try {
            Wait-ForTitle $Process $ExpectedTitle 2
            return
        }
        catch {
            if ($attempt -eq $Attempts) { throw }
        }
    }
}

function Save-WindowScreenshot([System.Diagnostics.Process]$Process, [System.Drawing.Rectangle]$Bounds, [string]$Name = "final") {
    # The semantic state is published from Update; allow the following Draw/Present
    # to settle before asking DWM for the rendered window.
    Start-Sleep -Milliseconds 200
    [NativeGameDriver]::DwmFlush() | Out-Null
    $frame = New-Object NativeGameDriver+RECT
    $frameSize = [Runtime.InteropServices.Marshal]::SizeOf($frame)
    $hasFrame = [NativeGameDriver]::DwmGetWindowAttribute($Process.MainWindowHandle, 9, [ref]$frame, $frameSize) -eq 0
    $captureX = if ($hasFrame) { $frame.Left } else { $Bounds.X }
    $captureY = if ($hasFrame) { $frame.Top } else { $Bounds.Y }
    $captureWidth = if ($hasFrame) { $frame.Right - $frame.Left } else { $Bounds.Width }
    $captureHeight = if ($hasFrame) { $frame.Bottom - $frame.Top } else { $Bounds.Height }
    $bestBitmap = $null
    $bestCoverage = -1
    $captureAttempts = if ($Name -like "*report*") { 10 } else { 3 }
    try {
        for ($attempt = 0; $attempt -lt $captureAttempts; $attempt++) {
            if ($attempt -gt 0) {
                Start-Sleep -Milliseconds 173
                [NativeGameDriver]::DwmFlush() | Out-Null
            }
            $candidate = New-Object System.Drawing.Bitmap $captureWidth, $captureHeight
            $graphics = [System.Drawing.Graphics]::FromImage($candidate)
            try {
                $graphics.CopyFromScreen($captureX, $captureY, 0, 0, $candidate.Size)
            }
            finally {
                $graphics.Dispose()
            }

            $coverage = 0
            for ($y = 0; $y -lt $captureHeight; $y += 8) {
                for ($x = 0; $x -lt $captureWidth; $x += 8) {
                    $sample = $candidate.GetPixel($x, $y)
                    if (($sample.R + $sample.G + $sample.B) -gt 75) { $coverage++ }
                }
            }
            if ($coverage -gt $bestCoverage) {
                if ($null -ne $bestBitmap) { $bestBitmap.Dispose() }
                $bestBitmap = $candidate
                $bestCoverage = $coverage
            }
            else {
                $candidate.Dispose()
            }
        }

        $path = Join-Path $env:TEMP "automation-game-ui-smoke-$Name.png"
        $bestBitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        return $path
    }
    finally {
        if ($null -ne $bestBitmap) { $bestBitmap.Dispose() }
    }
}

$leftmost = [System.Windows.Forms.Screen]::AllScreens | Sort-Object { $_.Bounds.X } | Select-Object -First 1
$windowBounds = New-Object System.Drawing.Rectangle ($leftmost.Bounds.X + 40), ($leftmost.Bounds.Y + 40), 1280, 800
$controlFile = Join-Path $env:TEMP "automation-game-ui-$([Guid]::NewGuid().ToString('N')).control"
$saveFile = Join-Path $env:TEMP "automation-game-career-$([Guid]::NewGuid().ToString('N')).json"
$playtestFile = Join-Path $env:TEMP "automation-game-playtest-$([Guid]::NewGuid().ToString('N')).json"
$controlSequence = 0
$previousControlFile = [Environment]::GetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE", "Process")
$previousWindowed = [Environment]::GetEnvironmentVariable("AUTOMATION_WINDOWED", "Process")
$previousSavePath = [Environment]::GetEnvironmentVariable("AUTOMATION_SAVE_PATH", "Process")
$previousPlaytestPath = [Environment]::GetEnvironmentVariable("AUTOMATION_PLAYTEST_EVIDENCE_PATH", "Process")
$previousPlaytestSession = [Environment]::GetEnvironmentVariable("AUTOMATION_PLAYTEST_SESSION_ID", "Process")
$previousDisableSave = [Environment]::GetEnvironmentVariable("AUTOMATION_DISABLE_CAREER_SAVE", "Process")
$previousDeveloperTools = [Environment]::GetEnvironmentVariable("AUTOMATION_DEVELOPER_TOOLS", "Process")
$previousDiagnosticTitle = [Environment]::GetEnvironmentVariable("AUTOMATION_DIAGNOSTIC_TITLE", "Process")

# Verify the ordinary player process does not expose consequence-bypassing tools
# before the final shift outcome. The semantic driver is deliberately absent.
[Environment]::SetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE", $null, "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", "1", "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_DISABLE_CAREER_SAVE", "1", "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_DEVELOPER_TOOLS", $null, "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_DIAGNOSTIC_TITLE", "1", "Process")
try {
    $playerProcess = Start-Process -FilePath $executable -PassThru
}
finally {
    [Environment]::SetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE", $previousControlFile, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", $previousWindowed, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_DISABLE_CAREER_SAVE", $previousDisableSave, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_DEVELOPER_TOOLS", $previousDeveloperTools, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_DIAGNOSTIC_TITLE", $previousDiagnosticTitle, "Process")
}
try {
    Wait-ForWindow $playerProcess
    [NativeGameDriver]::MoveWindow($playerProcess.MainWindowHandle, $windowBounds.X, $windowBounds.Y, $windowBounds.Width, $windowBounds.Height, $true) | Out-Null
    Wait-ForTitle $playerProcess "[tools=locked]" 3
    $lockedToolsScreenshot = Save-WindowScreenshot $playerProcess $windowBounds "player-tools-locked"
}
finally {
    if (-not $playerProcess.HasExited) { Stop-Process -Id $playerProcess.Id }
}

[Environment]::SetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE", $controlFile, "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", "1", "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_SAVE_PATH", $saveFile, "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_EVIDENCE_PATH", $playtestFile, "Process")
[Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_SESSION_ID", "ui-smoke", "Process")
try {
    $process = Start-Process -FilePath $executable -PassThru
}
finally {
    [Environment]::SetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE", $previousControlFile, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", $previousWindowed, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_SAVE_PATH", $previousSavePath, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_EVIDENCE_PATH", $previousPlaytestPath, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_PLAYTEST_SESSION_ID", $previousPlaytestSession, "Process")
}
$passed = $false

try {
    Wait-ForWindow $process
    [NativeGameDriver]::MoveWindow($process.MainWindowHandle, $windowBounds.X, $windowBounds.Y, $windowBounds.Width, $windowBounds.Height, $true) | Out-Null
    Wait-ForTitle $process "[intro=1/5:Guided]" 3
    Start-Sleep -Milliseconds 150
    $introWelcomeScreenshot = Save-WindowScreenshot $process $windowBounds "intro-welcome"
    Click-UntilTitle $process 748 480 "[intro=2/5:Guided]"
    Click-UntilTitle $process 748 480 "[intro=3/5:Guided]"
    Click-UntilTitle $process 748 480 "[intro=4/5:Guided]"
    Click-UntilTitle $process 512 363 "[intro=4/5:Contextual]"
    Start-Sleep -Milliseconds 150
    $introGuidanceScreenshot = Save-WindowScreenshot $process $windowBounds "intro-guidance"
    Click-UntilTitle $process 748 480 "[intro=5/5:Contextual]"
    Click-UntilTitle $process 327 368 "[comfort=motion=reduced,contrast=standard]"
    Click-UntilTitle $process 657 368 "[comfort=motion=reduced,contrast=high]"
    Start-Sleep -Milliseconds 150
    $introComfortScreenshot = Save-WindowScreenshot $process $windowBounds "intro-comfort"
    Click-UntilTitle $process 748 480 "[intro=done]"
    Wait-ForStage $process "RestockFirstDish"
    Click-UntilTitle $process 854 535 "[help=True]"
    Start-Sleep -Milliseconds 150
    $shiftHandbookScreenshot = Save-WindowScreenshot $process $windowBounds "shift-handbook"
    Click-UntilTitle $process 860 93 "[help=False]"
    Start-Sleep -Milliseconds 750
    foreach ($fixture in @(
        @{ Name = "Scrape"; X = 440; Y = 158 },
        @{ Name = "Rack"; X = 548; Y = 200 },
        @{ Name = "Washer"; X = 656; Y = 232 },
        @{ Name = "Unload"; X = 764; Y = 284 },
        @{ Name = "DryRestock"; X = 764; Y = 340 },
        @{ Name = "Service"; X = 620; Y = 365 }
    )) {
        Move-GamePointer $process $fixture.X $fixture.Y
        Wait-ForTitle $process "[pointer=$($fixture.Name):" 3
    }
    Move-GamePointer $process 620 365 -Click
    Wait-ForTitle $process "[click=Service:ROUTE:" 3
    Wait-ForTitle $process "[player=11,7]" 3
    Move-GamePointer $process 620 365 -Click
    Wait-ForTitle $process "[click=Service:INSPECT]" 3
    Move-GamePointer $process 548 200 -Click
    Wait-ForTitle $process "[station=RACK]" 3
    Wait-ForTitle $process "[click=Rack:ROUTE:" 3
    Wait-ForTitle $process "[player=4,2]" 3
    Move-GamePointer $process 260 216 -Click
    Wait-ForTitle $process "[click=FLOOR:ROUTE:" 3
    Wait-ForTitle $process "[player=0,5]" 3
    Send-GameControl $process "PreviousWorkstation"
    Wait-ForTitle $process "[station=SCRAPE]" 3
    Send-GameControl $process "ContextWork"
    Wait-ForTitle $process "[player=1,2]" 3
    Wait-ForTitle $process "[layout=Linear] [build=False] [route=17]" 3

    Send-GameControl $process "ToggleGodMode"
    Wait-ForTitle $process "[god=True]" 3
    Send-GameControl $process "TogglePlacementMode"
    Wait-ForTitle $process "[build=True]" 3
    Move-GamePointer $process 404 160 -Click
    Wait-ForTitle $process "[layout=Custom] [build=True] [route=18]" 3
    Start-Sleep -Milliseconds 150
    $placementScreenshot = Save-WindowScreenshot $process $windowBounds "placement"
    Send-GameControl $process "UndoPlacement"
    Wait-ForTitle $process "[route=17]" 3
    Send-GameControl $process "ResetSandboxLayout"
    Wait-ForTitle $process "[layout=Linear]" 3
    Send-GameControl $process "TogglePlacementMode"
    Wait-ForTitle $process "[build=False]" 3
    Send-GameControl $process "ToggleGodMode"
    Wait-ForTitle $process "[god=False]" 3

    $largeBounds = New-Object System.Drawing.Rectangle ($leftmost.WorkingArea.X + 20), ($leftmost.WorkingArea.Y + 20), ($leftmost.WorkingArea.Width - 40), ($leftmost.WorkingArea.Height - 60)
    [NativeGameDriver]::MoveWindow($process.MainWindowHandle, $largeBounds.X, $largeBounds.Y, $largeBounds.Width, $largeBounds.Height, $true) | Out-Null
    Wait-ForUiScale $process 2.0 10
    Start-Sleep -Milliseconds 200
    $scalingScreenshot = Save-WindowScreenshot $process $largeBounds "4k-scaling"
    [NativeGameDriver]::MoveWindow($process.MainWindowHandle, $windowBounds.X, $windowBounds.Y, $windowBounds.Width, $windowBounds.Height, $true) | Out-Null
    Start-Sleep -Milliseconds 500

    Send-ControlUntilTitle $process "Scrape" "Work has state"
    Send-ControlUntilTitle $process "Rack" "Ready for the machine"
    Send-ControlUntilTitle $process "StartWasher" "Washer started"
    Start-Sleep -Milliseconds 2400
    Send-ControlUntilTitle $process "Unload" "Drying area"
    Send-ControlUntilStage $process "DryAndRestock" "EnableDinnerRush"
    Wait-ForStage $process "EnableDinnerRush"
    Wait-ForTitle $process "[level=2] [xp=100]" 3
    Wait-ForTitle $process "[receipt=ClockIn:L2]" 3
    Start-Sleep -Milliseconds 150
    $progressionReceiptScreenshot = Save-WindowScreenshot $process $windowBounds "progression-receipt"
    Click-UntilTitle $process 760 535 "[journal=True]"
    Wait-ForTitle $process "[journalQuest=FindTheConstraint] [detail=False]" 3
    Click-UntilTitle $process 400 160 "[journalQuest=ClockIn] [detail=False]"
    Click-UntilTitle $process 400 203 "[journalQuest=FindTheConstraint] [detail=False]"
    Start-Sleep -Milliseconds 150
    $earlyJournalScreenshot = Save-WindowScreenshot $process $windowBounds "journal-early"
    Click-UntilTitle $process 678 518 "[journalQuest=FindTheConstraint] [detail=True]"
    Start-Sleep -Milliseconds 150
    $activeQuestDetailScreenshot = Save-WindowScreenshot $process $windowBounds "quest-active-detail"
    Click-UntilTitle $process 678 503 "[detail=False]"
    Click-UntilTitle $process 808 518 "[journal=False]"

    Send-GameControl $process "ToggleRush"
    Wait-ForStage $process "InspectShortage" 8
    Send-ControlUntilStage $process "ToggleProcessLens" "ChooseBottleneck"
    Send-ControlUntilStage $process "ConfirmBottleneck" "ImproveLayout"
    Wait-ForTitle $process "[receipt=FindTheConstraint:L3]" 3
    Send-ControlUntilStage $process "ConfigureFlowCell" "ValidateBottleneck"
    Wait-ForStage $process "ValidateBottleneck"

    Send-GameControl $process "NextDish"
    Send-ControlUntilTitle $process "Scrape" "Work has state"
    Send-ControlUntilTitle $process "Rack" "Ready for the machine"
    Send-ControlUntilTitle $process "StartWasher" "Washer started"
    Start-Sleep -Milliseconds 2400
    Send-ControlUntilTitle $process "Unload" "Drying area"
    Send-ControlUntilStage $process "DryAndRestock" "AwaitValidationDemand"
    Wait-ForStage $process "InviteNewHire" 8

    Send-ControlUntilStage $process "ToggleNewHire" "TrainNewHire"
    Send-ControlUntilStage $process "TrainHappyPath" "ObserveNewHire"
    Wait-ForStage $process "DocumentGlassPriority" 8
    Send-ControlUntilStage $process "TrainRushPriority" "ValidateDelegation"
    Wait-ForStage $process "DocumentRareTray" 25
    Wait-ForTitle $process "[receipt=TransferTheWork:L4]" 3
    Send-ControlUntilStage $process "TrainRareTray" "ValidateRareTray"
    Wait-ForStage $process "OfferAutomation" 15

    Send-GameControl $process "ToggleAutomationEditor"
    Send-GameControl $process "AutomationEditorToggleValue"
    Send-ControlUntilStage $process "AutomationEditorApply" "ObserveAutomation"
    Send-GameControl $process "ToggleAutomationEditor"
    Send-GameControl $process "AutomationEditorSaveBaseline"
    Send-GameControl $process "AutomationEditorClose"
    Wait-ForStage $process "InvestigateAutomation" 15
    Send-ControlUntilStage $process "InspectIncident" "ReplayAutomation"
    Wait-ForStage $process "ReplayAutomation"
    Send-ControlUntilStage $process "ReplayIncident" "RefineAutomation"
    Wait-ForStage $process "RefineAutomation"
    Send-GameControl $process "ToggleAutomationEditor"
    Send-GameControl $process "AutomationEditorNext"
    Send-GameControl $process "AutomationEditorNext"
    Send-GameControl $process "AutomationEditorNext"
    Send-GameControl $process "AutomationEditorToggleValue"
    Send-GameControl $process "AutomationEditorApply"
    Send-GameControl $process "ToggleAutomationEditor"
    Send-GameControl $process "AutomationEditorSaveVariant"
    Send-GameControl $process "AutomationEditorRunComparison"
    Send-GameControl $process "AutomationEditorClose"
    Wait-ForStage $process "ValidateRegression" 8
    Send-ControlUntilStage $process "ReplayIncident" "ShiftReview"
    Wait-ForTitle $process "[quest=OwnTheShift]" 3
    Wait-ForTitle $process "[level=6] [xp=2500]" 3
    Send-GameControl $process "ToggleQuestJournal"
    Wait-ForTitle $process "[journalQuest=OwnTheShift] [detail=False]" 3
    Send-GameControl $process "ToggleQuestDetail"
    Wait-ForTitle $process "[journalQuest=OwnTheShift] [detail=True]" 3
    $shiftReviewScreenshot = Save-WindowScreenshot $process $windowBounds "shift-review-detail"
    Send-GameControl $process "ToggleQuestJournal"
    Wait-ForTitle $process "[journal=False]" 3
    Send-GameControl $process "ToggleIncidentLens"
    Wait-ForTitle $process "[lens=Process]" 3

    # Give the deterministic smoke journey a staged service buffer; the headless
    # scenario separately proves preparation, failure, recovery, and retry.
    Send-GameControl $process "ToggleGodMode"
    Wait-ForTitle $process "[god=True]" 3
    Send-ControlUntilTitle $process "GodSetCleanSupply" "Set available glass supply to 10"
    Send-GameControl $process "ToggleGodMode"
    Wait-ForTitle $process "[god=False]" 3
    Send-ControlUntilStage $process "StartShiftTrial" "ValidateShift"
    Wait-ForTitle $process "[trial=Running:0/3]" 3
    $shiftRunningScreenshot = Save-WindowScreenshot $process $windowBounds "shift-window-running"
    Wait-ForStage $process "EpisodeComplete" 10
    Wait-ForTitle $process "[trial=Passed:3/3]" 3
    Wait-ForTitle $process "[receipt=OwnTheShift:L7]" 3

    Send-GameControl $process "CameraZoomIn"
    Wait-ForTitle $process "[zoom=1.10]" 3
    Send-GameControl $process "CameraPanRight"
    Wait-ForTitle $process "[cam=28,0]" 3
    Send-GameControl $process "CameraReset"
    Wait-ForTitle $process "[zoom=1.00] [cam=0,0]" 3

    $screenshots = @($lockedToolsScreenshot, $introWelcomeScreenshot, $introGuidanceScreenshot, $introComfortScreenshot, $shiftHandbookScreenshot, $progressionReceiptScreenshot, $earlyJournalScreenshot, $activeQuestDetailScreenshot, $placementScreenshot, $scalingScreenshot, $shiftReviewScreenshot, $shiftRunningScreenshot)
    Start-Sleep -Milliseconds 150
    $screenshots += Save-WindowScreenshot $process $windowBounds "runtime"
    foreach ($lens in @("State", "Knowledge", "Automation", "Runtime", "Responsibility", "Reality", "Process", "State")) {
        Send-GameControl $process "NextLens"
        Wait-ForTitle $process "[lens=$lens]" 3
        Start-Sleep -Milliseconds 100
        if ($lens -in @("Responsibility", "Reality", "Process", "State", "Knowledge", "Automation")) {
            $screenshots += Save-WindowScreenshot $process $windowBounds $lens.ToLowerInvariant()
        }
    }
    Wait-ForTitle $process "[quest=complete]" 3
    Wait-ForTitle $process "[level=7] [xp=3400]" 3
    Wait-ForTitle $process "[evidence=written]" 5
    if (-not (Test-Path -LiteralPath $playtestFile)) { throw "Playtest evidence was not written to $playtestFile." }
    $evidence = Get-Content -LiteralPath $playtestFile -Raw | ConvertFrom-Json
    if ($evidence.schemaVersion -ne 2) { throw "Unexpected playtest evidence schema $($evidence.schemaVersion)." }
    if ($evidence.sessionId -ne "ui-smoke") { throw "Unexpected playtest session id $($evidence.sessionId)." }
    if ($evidence.level -ne 7 -or $evidence.experience -ne 3400) { throw "Playtest progression evidence was incomplete." }
    if ($evidence.quests.Count -ne 8) { throw "Expected evidence for eight quests, got $($evidence.quests.Count)." }
    if ($evidence.shiftTrial.status -ne "Passed" -or $evidence.shiftTrial.successfulDemandChecks -ne 3) { throw "Shift-trial evidence did not record the passed reliability window." }
    if (-not $evidence.shiftReport.available) { throw "Frozen shift-report evidence was unavailable." }
    if ($evidence.handbookVisits.Count -lt 1 -or $evidence.handbookVisits[0].stage -ne "RestockFirstDish" -or $evidence.handbookVisits[0].openCount -ne 1) { throw "Handbook-use evidence did not preserve the observed stage visit." }
    if ($evidence.wallClockSeconds -le 0 -or $evidence.activeSimulationTicks -le 0) { throw "Playtest duration evidence was invalid." }
    Send-GameControl $process "ToggleQuestJournal"
    Wait-ForTitle $process "[journal=True]" 3
    Wait-ForTitle $process "[journalQuest=OwnTheShift] [detail=False]" 3
    Start-Sleep -Milliseconds 150
    $screenshots += Save-WindowScreenshot $process $windowBounds "journal-complete"
    Send-GameControl $process "ToggleQuestDetail"
    Wait-ForTitle $process "[journalQuest=OwnTheShift] [detail=True]" 3
    Start-Sleep -Milliseconds 150
    $screenshots += Save-WindowScreenshot $process $windowBounds "quest-complete-detail"
    Send-GameControl $process "JournalBack"
    Wait-ForTitle $process "[detail=False]" 3
    Send-GameControl $process "ToggleQuestJournal"
    Wait-ForTitle $process "[journal=False]" 3
    Click-UntilTitle $process 948 535 "[report=True]"
    Start-Sleep -Milliseconds 2000
    $screenshots += Save-WindowScreenshot $process $windowBounds "shift-report"
    Click-UntilTitle $process 858 540 "[report=False]"
    Send-GameControl $process "ToggleGodMode"
    Wait-ForTitle $process "[god=True]" 3
    Start-Sleep -Milliseconds 100
    $screenshots += Save-WindowScreenshot $process $windowBounds "tools"
    Send-GameControl $process "GodToggleBenchmark"
    Wait-ForTitle $process "[benchmark=on]" 3
    Send-GameControl $process "GodTogglePause"
    Wait-ForTitle $process "[paused=True]" 3
    $process.Refresh()
    if ($process.MainWindowTitle -notmatch '\[dirty=(\d+)\]') { throw "Window title did not expose dirty count for save/restore validation." }
    $savedDirty = [int]$Matches[1]
    Send-GameControl $process "GodQuickSave"
    Send-GameControl $process "GodAddDirty"
    Wait-ForTitle $process "[dirty=$($savedDirty + 5)]" 3
    Send-GameControl $process "GodQuickLoad"
    Wait-ForTitle $process "[dirty=$savedDirty]" 3
    Start-Sleep -Milliseconds 150
    $screenshots += Save-WindowScreenshot $process $windowBounds "benchmark"
    if (-not (Test-Path -LiteralPath $saveFile)) { throw "Career autosave was not written to $saveFile." }

    Stop-Process -Id $process.Id
    $process.WaitForExit(5000) | Out-Null
    [Environment]::SetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE", $controlFile, "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", "1", "Process")
    [Environment]::SetEnvironmentVariable("AUTOMATION_SAVE_PATH", $saveFile, "Process")
    try {
        $process = Start-Process -FilePath $executable -PassThru
    }
    finally {
        [Environment]::SetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE", $previousControlFile, "Process")
        [Environment]::SetEnvironmentVariable("AUTOMATION_WINDOWED", $previousWindowed, "Process")
        [Environment]::SetEnvironmentVariable("AUTOMATION_SAVE_PATH", $previousSavePath, "Process")
    }
    Wait-ForWindow $process
    [NativeGameDriver]::MoveWindow($process.MainWindowHandle, $windowBounds.X, $windowBounds.Y, $windowBounds.Width, $windowBounds.Height, $true) | Out-Null
    Wait-ForTitle $process "[menu=continue] [save=FOUND]" 5
    Start-Sleep -Milliseconds 150
    $screenshots += Save-WindowScreenshot $process $windowBounds "career-continue"
    Click-UntilTitle $process 650 280 "[menu=confirm-new]"
    Start-Sleep -Milliseconds 150
    $screenshots += Save-WindowScreenshot $process $windowBounds "career-new-confirm"
    Click-UntilTitle $process 670 412 "[menu=new]"
    Click-UntilTitle $process 350 280 "[menu=closed]"
    Wait-ForTitle $process "[save=LOADED]" 3
    Wait-ForTitle $process "[intro=done]" 3
    Wait-ForTitle $process "[quest=complete]" 3
    Wait-ForTitle $process "[comfort=motion=reduced,contrast=high]" 3
    Wait-ForTitle $process "[level=7] [xp=3400]" 3
    Wait-ForTitle $process "[trial=Passed:3/3]" 3
    Send-GameControl $process "ToggleShiftReport"
    Wait-ForTitle $process "[report=True]" 3
    Send-GameControl $process "ToggleShiftReport"
    Wait-ForTitle $process "[report=False]" 3
    Start-Sleep -Milliseconds 150
    $screenshots += Save-WindowScreenshot $process $windowBounds "career-resumed"
    $screenshot = $screenshots[-1]
    $retainedScreenshots = @()
    if (-not [string]::IsNullOrWhiteSpace($RetainScreenshotsPath)) {
        $retainedDirectory = [System.IO.Path]::GetFullPath($RetainScreenshotsPath, $repositoryRoot)
        New-Item -ItemType Directory -Force -Path $retainedDirectory | Out-Null
        foreach ($capturedPath in $screenshots | Select-Object -Unique) {
            $retainedName = [System.IO.Path]::GetFileName($capturedPath).Replace("automation-game-ui-smoke-", "")
            $retainedPath = Join-Path $retainedDirectory $retainedName
            Copy-Item -LiteralPath $capturedPath -Destination $retainedPath -Force
            $retainedScreenshots += $retainedPath
        }
    }
    $passed = $true
    [pscustomobject]@{
        Result = "PASS"
        Stage = "EpisodeComplete"
        ProcessId = $process.Id
        Screenshot = $screenshot
        Evidence = $playtestFile
        LensScreenshots = $screenshots -join "; "
        RetainedScreenshots = $retainedScreenshots -join "; "
        WindowTitle = $process.MainWindowTitle
    }
}
finally {
    if ((-not $KeepOpen -or -not $passed) -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
    }
    if (Test-Path -LiteralPath $controlFile) {
        [System.IO.File]::Delete($controlFile)
    }
    if (Test-Path -LiteralPath $saveFile) {
        [System.IO.File]::Delete($saveFile)
    }
    if (Test-Path -LiteralPath ($saveFile + ".tmp")) {
        [System.IO.File]::Delete($saveFile + ".tmp")
    }
    if (Test-Path -LiteralPath $playtestFile) {
        [System.IO.File]::Delete($playtestFile)
    }
    if (Test-Path -LiteralPath ($playtestFile + ".tmp")) {
        [System.IO.File]::Delete($playtestFile + ".tmp")
    }
}
