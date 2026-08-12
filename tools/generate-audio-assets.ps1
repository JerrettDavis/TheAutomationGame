param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\src\Automation.Client.Stride\Resources\Audio'))

$ErrorActionPreference = 'Stop'
$sampleRate = 22050
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Write-Wave {
    param([string]$Name, [double]$Seconds, [scriptblock]$Sample)
    $count = [int]($sampleRate * $Seconds)
    $samples = New-Object short[] $count
    for ($index = 0; $index -lt $count; $index++) {
        $time = $index / $sampleRate
        $value = [Math]::Max(-1, [Math]::Min(1, (& $Sample $time $index $count)))
        $samples[$index] = [int16]($value * 32767)
    }
    $path = Join-Path $OutputDirectory "$Name.wav"
    $stream = [IO.File]::Create($path)
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $dataBytes = $count * 2
        $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write(36 + $dataBytes)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
        $writer.Write(16)
        $writer.Write([int16]1)
        $writer.Write([int16]1)
        $writer.Write($sampleRate)
        $writer.Write($sampleRate * 2)
        $writer.Write([int16]2)
        $writer.Write([int16]16)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write($dataBytes)
        foreach ($value in $samples) { $writer.Write($value) }
    }
    finally { $writer.Dispose(); $stream.Dispose() }
}

function Envelope([double]$time, [double]$seconds) {
    $attack = [Math]::Min(1, $time / 0.015)
    $release = [Math]::Min(1, ($seconds - $time) / 0.06)
    return [Math]::Max(0, [Math]::Min($attack, $release))
}

Write-Wave 'DishRoomAmbience' 2.0 {
    param($t, $i, $count)
    $hum = [Math]::Sin(2 * [Math]::PI * 55 * $t) * 0.10 + [Math]::Sin(2 * [Math]::PI * 110 * $t) * 0.035
    $water = [Math]::Sin(2 * [Math]::PI * (360 + 30 * [Math]::Sin(2 * [Math]::PI * 0.5 * $t)) * $t) * 0.018
    return ($hum + $water) * [Math]::Sin([Math]::PI * $t / 2.0) * 0.55
}
Write-Wave 'Work' 0.22 { param($t, $i, $count) (Envelope $t 0.22) * ([Math]::Sin(2*[Math]::PI*520*$t)*0.25 + [Math]::Sin(2*[Math]::PI*780*$t)*0.10) }
Write-Wave 'WasherStart' 0.52 { param($t, $i, $count) (Envelope $t 0.52) * [Math]::Sin(2*[Math]::PI*(120 + 420*$t)*$t)*0.28 }
Write-Wave 'WasherLoop' 1.50 { param($t, $i, $count) ([Math]::Sin([Math]::PI*$t/1.50) * [Math]::Sin([Math]::PI*$t/1.50)) * ([Math]::Sin(2*[Math]::PI*60*$t)*0.13 + [Math]::Sin(2*[Math]::PI*180*$t)*0.035) }
Write-Wave 'WasherComplete' 0.55 { param($t, $i, $count) (Envelope $t 0.55) * ([Math]::Sin(2*[Math]::PI*660*$t)*0.20 + [Math]::Sin(2*[Math]::PI*880*$t)*0.12) }
Write-Wave 'Blocked' 0.20 { param($t, $i, $count) (Envelope $t 0.20) * [Math]::Sin(2*[Math]::PI*(210 - 450*$t)*$t)*0.30 }
Write-Wave 'Failure' 0.62 { param($t, $i, $count) (Envelope $t 0.62) * ([Math]::Sin(2*[Math]::PI*185*$t)*0.24 + [Math]::Sin(2*[Math]::PI*233*$t)*0.16) }
Write-Wave 'QuestSuccess' 0.72 { param($t, $i, $count) $note = if($t -lt .24){523.25}elseif($t -lt .48){659.25}else{783.99}; (Envelope $t 0.72) * [Math]::Sin(2*[Math]::PI*$note*$t)*0.25 }
Write-Wave 'UiConfirm' 0.18 { param($t, $i, $count) (Envelope $t 0.18) * ([Math]::Sin(2*[Math]::PI*740*$t)*0.16 + [Math]::Sin(2*[Math]::PI*1110*$t)*0.08) }

Get-ChildItem -Path $OutputDirectory -Filter '*.wav' | Sort-Object Name | Select-Object Name, Length
