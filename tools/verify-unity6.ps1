param(
    [ValidateSet('Validate', 'Windows', 'Android')]
    [string]$Target = 'Validate',
    [string]$Unity = 'C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$logPath = Join-Path $projectPath ('Logs/verify-' + $Target.ToLowerInvariant() + '.log')
New-Item -ItemType Directory -Force (Split-Path $logPath) | Out-Null
$method = switch ($Target) {
    'Validate' { 'CatcheepCompatibility.Validate' }
    'Windows' { 'CatcheepCompatibility.BuildWindowsSmokeTest' }
    'Android' { 'CatcheepCompatibility.BuildAndroid' }
}
$arguments = '-batchmode -nographics -quit -projectPath "' + $projectPath + '" -executeMethod ' + $method + ' -logFile "' + $logPath + '"'
if ($Target -eq 'Android') { $arguments += ' -buildTarget Android' }
if ($Target -eq 'Windows') { $arguments += ' -buildTarget Win64' }
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) {
    Get-Content -LiteralPath $logPath -Tail 65
    throw "Unity exited with code $($process.ExitCode). See $logPath"
}
Select-String -LiteralPath $logPath -Pattern 'CATCHEEP_'
if ($Target -eq 'Windows') {
    $player = Join-Path $projectPath 'Builds/WindowsSmoke/Catcheep.exe'
    $playerLog = Join-Path $projectPath 'Logs/windows-smoke.log'
    $test = Start-Process -FilePath $player -WorkingDirectory $projectPath -ArgumentList ('-catcheep-smoke -catcheep-capture -screen-width 480 -screen-height 854 -screen-fullscreen 0 -logFile "' + $playerLog + '"') -WindowStyle Hidden -PassThru
    if (-not $test.WaitForExit(150000)) { $test.Kill(); throw 'Smoke test exceeded 150 seconds.' }
    Select-String -LiteralPath $playerLog -Pattern 'CATCHEEP_'
    if ($test.ExitCode -ne 0) { throw "Gameplay smoke test failed. See $playerLog" }
}
