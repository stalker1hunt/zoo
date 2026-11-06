param(
    [string]$UnityPath = '',
    [switch]$BuildPlayer,
    [int]$TimeoutMinutes = 20
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$versionLine = Get-Content -LiteralPath (Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
$version = ($versionLine -split ': ', 2)[1]
if (-not $UnityPath) { $UnityPath = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" }
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity $version was not found. Supply -UnityPath." }
$resultsPath = Join-Path $projectPath 'TestResults'
New-Item -ItemType Directory -Force -Path $resultsPath | Out-Null

function Invoke-ZooUnity([string[]]$ExtraArguments, [string]$LogPath) {
    $arguments = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $projectPath), '-logFile', ('"{0}"' -f $LogPath)) + $ExtraArguments
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit($TimeoutMinutes * 60000)) {
        Stop-Process -Id $process.Id
        throw "Unity timed out. See $LogPath"
    }
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See $LogPath" }
}

# Close this project's interactive Editor before running. Each suite owns its Editor process.
foreach ($suite in @('EditMode', 'PlayMode')) {
    $resultFile = Join-Path $resultsPath "$suite.xml"
    $logFile = Join-Path $resultsPath "$suite.log"
    if (Test-Path -LiteralPath $resultFile) { Remove-Item -LiteralPath $resultFile }
    Write-Host "Running $suite tests..."
    Invoke-ZooUnity -ExtraArguments @('-runTests', '-testPlatform', $suite, '-testResults', ('"{0}"' -f $resultFile)) -LogPath $logFile
    if (-not (Test-Path -LiteralPath $resultFile)) { throw "Unity produced no $suite test results. See $logFile" }
    [xml]$results = Get-Content -LiteralPath $resultFile -Raw
    $run = $results.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.total -eq 0 -or [int]$run.skipped -gt 0) {
        throw "$suite did not fully pass: $($run.result), $($run.failed) failed, $($run.skipped) skipped. See $resultFile"
    }
    Write-Host "$suite passed: $($run.passed) tests."
}

if ($BuildPlayer) {
    Invoke-ZooUnity -ExtraArguments @('-executeMethod', 'ZooWorld.Editor.ZooBuildValidation.BuildWindows', '-quit') -LogPath (Join-Path $resultsPath 'WindowsBuild.log')
    Write-Host 'Windows player built in Builds/Validation/Zoo.exe.'
}
