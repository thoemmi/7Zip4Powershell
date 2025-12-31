<#
.SYNOPSIS
    Builds the project and runs Pester tests.

.DESCRIPTION
    This script builds the 7Zip4Powershell project, prepares the module directory,
    installs Pester if needed, and runs the test suite. It can be used both locally
    and in CI/CD pipelines.

.PARAMETER SkipBuild
    Skip the build step (useful if you've already built the project).

.PARAMETER SkipModulePrep
    Skip the module preparation step (useful if module files are already in place).

.PARAMETER Configuration
    Build configuration to use (Release or Debug). Default: Release

.PARAMETER Verbose
    Show verbose output during execution.

.EXAMPLE
    .\Scripts\Test.ps1
    Builds the project and runs all tests.

.EXAMPLE
    .\Scripts\Test.ps1 -SkipBuild
    Runs tests without rebuilding (assumes project is already built).

.EXAMPLE
    .\Scripts\Test.ps1 -Configuration Debug
    Builds in Debug configuration and runs tests.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$SkipBuild,

    [Parameter()]
    [switch]$SkipModulePrep,

    [Parameter()]
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Get repository root directory
$RepoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $RepoRoot

try {
    Write-Host "===> 7Zip4Powershell Test Runner" -ForegroundColor Cyan
    Write-Host ""

    # Step 1: Build project
    if (-not $SkipBuild) {
        Write-Host "===> Building project ($Configuration configuration)..." -ForegroundColor Cyan

        dotnet restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE"
        }

        dotnet build --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE"
        }

        Write-Host "Build completed successfully." -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "===> Skipping build (using existing build output)" -ForegroundColor Yellow
        Write-Host ""
    }

    # Step 2: Prepare module directory
    if (-not $SkipModulePrep) {
        Write-Host "===> Preparing module directory..." -ForegroundColor Cyan

        $moduleTargetPath = Join-Path $RepoRoot "Module" "7Zip4Powershell"
        New-Item $moduleTargetPath -ItemType Directory -Force | Out-Null

        $sourcePath = Join-Path $RepoRoot "7Zip4Powershell" "bin" $Configuration "netstandard2.0"

        if (-not (Test-Path $sourcePath)) {
            throw "Build output not found at: $sourcePath. Run without -SkipBuild first."
        }

        Copy-Item -Path (Join-Path $sourcePath "*.*") `
                  -Exclude "JetBrains.Annotations.dll" `
                  -Destination $moduleTargetPath `
                  -Force

        Write-Host "Module prepared at: $moduleTargetPath" -ForegroundColor Green

        if ($VerbosePreference -eq 'Continue') {
            Get-ChildItem $moduleTargetPath | Format-Table Name, Length -AutoSize
        }

        Write-Host ""
    } else {
        Write-Host "===> Skipping module preparation" -ForegroundColor Yellow
        Write-Host ""
    }

    # Step 3: Install Pester v5 if needed
    Write-Host "===> Checking Pester installation..." -ForegroundColor Cyan

    $pesterModule = Get-Module -Name Pester -ListAvailable |
                    Where-Object { $_.Version -ge [Version]'5.0.0' } |
                    Sort-Object Version -Descending |
                    Select-Object -First 1

    if (-not $pesterModule) {
        Write-Host "Pester v5 not found. Installing..." -ForegroundColor Yellow
        Install-Module -Name Pester -Force -MinimumVersion 5.0.0 -Scope CurrentUser

        $pesterModule = Get-Module -Name Pester -ListAvailable |
                        Where-Object { $_.Version -ge [Version]'5.0.0' } |
                        Sort-Object Version -Descending |
                        Select-Object -First 1
    }

    Write-Host "Using Pester version: $($pesterModule.Version)" -ForegroundColor Green
    Write-Host ""

    # Step 4: Run tests
    Write-Host "===> Running tests..." -ForegroundColor Cyan
    Write-Host ""

    Import-Module Pester -MinimumVersion 5.0.0 -Force

    $testPath = Join-Path $RepoRoot "Tests" "7Zip4Powershell.Tests.ps1"

    if (-not (Test-Path $testPath)) {
        throw "Test file not found at: $testPath"
    }

    $testResults = Invoke-Pester -Path $testPath -Output Detailed -PassThru

    Write-Host ""
    Write-Host "===> Test Results" -ForegroundColor Cyan
    Write-Host "Total:  $($testResults.TotalCount)" -ForegroundColor White
    Write-Host "Passed: $($testResults.PassedCount)" -ForegroundColor Green
    Write-Host "Failed: $($testResults.FailedCount)" -ForegroundColor $(if ($testResults.FailedCount -eq 0) { 'Green' } else { 'Red' })
    Write-Host "Skipped: $($testResults.SkippedCount)" -ForegroundColor Yellow
    Write-Host ""

    if ($testResults.FailedCount -gt 0) {
        Write-Host "===> TESTS FAILED" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "===> ALL TESTS PASSED" -ForegroundColor Green
        exit 0
    }

} catch {
    Write-Host ""
    Write-Host "===> ERROR: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}
