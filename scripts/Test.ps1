<#
.SYNOPSIS
    Runs tests with code coverage.
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solutionFile = Get-ChildItem -Path $repoRoot -Filter '*.sln' -File | Select-Object -First 1

if (-not $solutionFile) { throw "No solution file found in $repoRoot" }

$solutionPath = $solutionFile.FullName
$solutionName = $solutionFile.BaseName
$testResultsDir = Join-Path $repoRoot 'TestResults'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Testing: $solutionName" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Push-Location $repoRoot
try {
    $buildArg = if ($NoBuild) { '--no-build' } else { '' }
    
    Write-Host "[Test] Running tests with coverage..." -ForegroundColor Yellow
    dotnet test $solutionPath `
        -c $Configuration `
        $buildArg `
        --results-directory $testResultsDir `
        --collect:"XPlat Code Coverage" `
        --logger "trx;LogFileName=test-results.trx"
    
    Write-Host "[Test] Tests completed successfully." -ForegroundColor Green
}
finally { Pop-Location }
