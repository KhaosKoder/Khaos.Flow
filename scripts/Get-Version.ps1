<#
.SYNOPSIS
    Gets the current version based on Git tags.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$srcDir = Join-Path $repoRoot 'src'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Version: $(Split-Path $repoRoot -Leaf)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Push-Location $repoRoot
try {
    $project = Get-ChildItem -Path $srcDir -Filter '*.csproj' -Recurse | Select-Object -First 1
    
    if ($project) {
        Write-Host "[Version] Querying version via MinVer..." -ForegroundColor Yellow
        $version = dotnet msbuild $project.FullName -getProperty:MinVerVersion 2>$null
        
        if ($version) {
            Write-Host "Current version: $version" -ForegroundColor Green
        }
        else {
            Write-Host "Could not determine version." -ForegroundColor Yellow
        }
    }
}
finally { Pop-Location }
