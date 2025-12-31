<#
.SYNOPSIS
    Automates downloading and updating native 7-Zip DLL files.

.DESCRIPTION
    This script checks for the latest 7-Zip version, downloads installers for x86, x64, and ARM64 architectures,
    extracts the native DLLs, and updates the Libs folder along with version tracking metadata.

.PARAMETER CheckOnly
    Only check if an update is available without downloading or updating files.
    Sets GitHub Actions output variables: update_available, new_version

.PARAMETER Update
    Perform the full update process: download, extract, and update DLLs.

.PARAMETER TargetVersion
    Force a specific version to download (e.g., "25.01"). Useful for testing.

.PARAMETER WhatIf
    Show what would happen without making any changes. Dry run mode.

.EXAMPLE
    .\Update-7ZipDlls.ps1 -CheckOnly -Verbose
    Check if an update is available

.EXAMPLE
    .\Update-7ZipDlls.ps1 -Update
    Download and install the latest version

.EXAMPLE
    .\Update-7ZipDlls.ps1 -Update -TargetVersion "25.01"
    Download and install a specific version

.EXAMPLE
    .\Update-7ZipDlls.ps1 -Update -WhatIf
    Preview what would happen without making changes
#>

[CmdletBinding()]
param(
    [switch]$CheckOnly,
    [switch]$Update,
    [string]$TargetVersion,
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Script configuration
$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$script:LibsPath = Join-Path $RepoRoot "Libs"
$script:VersionJsonPath = Join-Path $script:LibsPath "7zip-version.json"
$script:TempPath = Join-Path $PSScriptRoot "temp"
$script:DownloadRetries = 3
$script:DownloadRetryDelay = 2

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARN" { "Yellow" }
        "SUCCESS" { "Green" }
        default { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Get-Latest7ZipVersion {
    <#
    .SYNOPSIS
        Scrapes 7-zip.org/download.html to find the latest version.
    #>
    [CmdletBinding()]
    param()

    Write-Log "Fetching latest 7-Zip version from 7-zip.org..."

    try {
        $response = Invoke-WebRequest -Uri "https://www.7-zip.org/download.html" -UseBasicParsing

        # Look for version pattern like "7-Zip 25.01" or "Download 7-Zip 25.01"
        if ($response.Content -match '7-Zip\s+(\d+\.\d+)') {
            $version = $Matches[1]
            Write-Log "Latest version found: $version" -Level SUCCESS
            return $version
        } else {
            throw "Could not parse version from 7-zip.org download page"
        }
    } catch {
        Write-Log "Failed to fetch latest version: $_" -Level ERROR
        throw
    }
}

function Get-Current7ZipVersion {
    <#
    .SYNOPSIS
        Reads the current version from Libs/7zip-version.json.
    #>
    [CmdletBinding()]
    param()

    if (-not (Test-Path $script:VersionJsonPath)) {
        Write-Log "Version tracking file not found: $script:VersionJsonPath" -Level WARN
        return $null
    }

    try {
        $versionData = Get-Content $script:VersionJsonPath -Raw | ConvertFrom-Json
        Write-Log "Current version: $($versionData.version)"
        return $versionData.version
    } catch {
        Write-Log "Failed to read version file: $_" -Level ERROR
        throw
    }
}

function ConvertTo-VersionNumber {
    <#
    .SYNOPSIS
        Converts version string (e.g., "25.01") to comparable version object.
    #>
    param([string]$VersionString)

    try {
        return [Version]$VersionString
    } catch {
        Write-Log "Invalid version format: $VersionString" -Level ERROR
        throw
    }
}

function Get-VersionUrlFormat {
    <#
    .SYNOPSIS
        Converts version string (e.g., "25.01") to URL format (e.g., "2501").
    #>
    param([string]$Version)

    # "25.01" -> "2501"
    return $Version -replace '\.'
}

function Download-7ZipInstaller {
    <#
    .SYNOPSIS
        Downloads 7-Zip installer for specified architecture.
    #>
    [CmdletBinding()]
    param(
        [string]$Version,
        [ValidateSet("x86", "x64", "ARM64")]
        [string]$Architecture
    )

    $urlVersion = Get-VersionUrlFormat -Version $Version

    # Build download URL based on architecture (all use .exe)
    $url = switch ($Architecture) {
        "x86"   { "https://www.7-zip.org/a/7z$urlVersion.exe" }
        "x64"   { "https://www.7-zip.org/a/7z$urlVersion-x64.exe" }
        "ARM64" { "https://www.7-zip.org/a/7z$urlVersion-arm64.exe" }
    }

    $outputPath = Join-Path $script:TempPath "7z-$Architecture-$Version.exe"

    Write-Log "Downloading $Architecture installer from: $url"

    $attempt = 0
    $success = $false

    while ($attempt -lt $script:DownloadRetries -and -not $success) {
        $attempt++
        try {
            if ($WhatIf) {
                Write-Log "[WhatIf] Would download: $url -> $outputPath" -Level WARN
                return $outputPath
            }

            Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing

            if (Test-Path $outputPath) {
                $fileSize = (Get-Item $outputPath).Length
                Write-Log "Download successful: $outputPath ($fileSize bytes)" -Level SUCCESS
                $success = $true
                return $outputPath
            }
        } catch {
            Write-Log "Download attempt $attempt failed: $_" -Level WARN
            if ($attempt -lt $script:DownloadRetries) {
                $delay = $script:DownloadRetryDelay * $attempt
                Write-Log "Retrying in $delay seconds..."
                Start-Sleep -Seconds $delay
            } else {
                Write-Log "Download failed after $script:DownloadRetries attempts" -Level ERROR
                throw
            }
        }
    }
}

function Test-DllArchitecture {
    <#
    .SYNOPSIS
        Validates DLL architecture by inspecting PE header.
    #>
    param(
        [string]$DllPath,
        [ValidateSet("x86", "x64", "ARM64")]
        [string]$ExpectedArchitecture
    )

    try {
        $bytes = [System.IO.File]::ReadAllBytes($DllPath)

        # Check PE signature
        $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
        $machineType = [BitConverter]::ToUInt16($bytes, $peOffset + 4)

        $actualArch = switch ($machineType) {
            0x014C { "x86" }    # IMAGE_FILE_MACHINE_I386
            0x8664 { "x64" }    # IMAGE_FILE_MACHINE_AMD64
            0xAA64 { "ARM64" }  # IMAGE_FILE_MACHINE_ARM64
            default { "Unknown" }
        }

        if ($actualArch -eq $ExpectedArchitecture) {
            Write-Log "DLL architecture validated: $actualArch" -Level SUCCESS
            return $true
        } else {
            Write-Log "Architecture mismatch: Expected $ExpectedArchitecture, got $actualArch" -Level ERROR
            return $false
        }
    } catch {
        Write-Log "Failed to validate DLL architecture: $_" -Level ERROR
        return $false
    }
}

function Get-7zrExtractor {
    <#
    .SYNOPSIS
        Downloads 7zr.exe (standalone 7-Zip console) if not already available.
    #>
    [CmdletBinding()]
    param()

    $sevenZrPath = Join-Path $script:TempPath "7zr.exe"

    if (Test-Path $sevenZrPath) {
        return $sevenZrPath
    }

    Write-Log "Downloading 7zr.exe (standalone 7-Zip console)..."

    try {
        # Download 7zr.exe (standalone console version)
        $url = "https://www.7-zip.org/a/7zr.exe"
        Invoke-WebRequest -Uri $url -OutFile $sevenZrPath -UseBasicParsing

        if (Test-Path $sevenZrPath) {
            Write-Log "7zr.exe downloaded successfully" -Level SUCCESS
            return $sevenZrPath
        } else {
            throw "Failed to download 7zr.exe"
        }
    } catch {
        Write-Log "Failed to download 7zr.exe: $_" -Level ERROR
        throw
    }
}

function Extract-DllFromInstaller {
    <#
    .SYNOPSIS
        Extracts the native DLL from EXE installer.
    #>
    [CmdletBinding()]
    param(
        [string]$InstallerPath,
        [ValidateSet("x86", "x64", "ARM64")]
        [string]$Architecture,
        [switch]$CopyLicense
    )

    $extractPath = Join-Path $script:TempPath "extract-$Architecture"
    $dllName = switch ($Architecture) {
        "x86"   { "7z.dll" }
        "x64"   { "7z64.dll" }
        "ARM64" { "7zARM64.dll" }
    }

    Write-Log "Extracting $dllName from installer..."

    try {
        if ($WhatIf) {
            Write-Log "[WhatIf] Would extract DLL from: $InstallerPath" -Level WARN
            if ($CopyLicense) {
                Write-Log "[WhatIf] Would copy License.txt from installer" -Level WARN
            }
            return (Join-Path $script:LibsPath $dllName)
        }

        # Create extraction directory
        New-Item -ItemType Directory -Path $extractPath -Force | Out-Null

        # Try to use 7z.exe if available in PATH
        $sevenZipExe = Get-Command "7z.exe" -ErrorAction SilentlyContinue

        if (-not $sevenZipExe) {
            # Download and use 7zr.exe (standalone console version)
            Write-Log "7z.exe not found in PATH, using 7zr.exe..."
            $sevenZipExe = Get-7zrExtractor
        } else {
            $sevenZipExe = $sevenZipExe.Source
        }

        # Extract using 7-Zip
        Write-Log "Extracting with: $sevenZipExe"
        & $sevenZipExe x "$InstallerPath" "-o$extractPath" -y | Out-Null

        # All installers contain "7z.dll" regardless of architecture
        # We need to find it and rename it to the target name
        $extractedDll = Get-ChildItem -Path $extractPath -Filter "7z.dll" -Recurse -ErrorAction SilentlyContinue |
                        Select-Object -First 1 -ExpandProperty FullName

        if (-not $extractedDll) {
            throw "7z.dll not found in extracted EXE"
        }

        Write-Log "Found extracted DLL: $extractedDll"
        $dllPath = $extractedDll

        # Validate architecture
        if (-not (Test-DllArchitecture -DllPath $dllPath -ExpectedArchitecture $Architecture)) {
            throw "DLL architecture validation failed"
        }

        # Copy to Libs folder
        $targetPath = Join-Path $script:LibsPath $dllName
        Copy-Item -Path $dllPath -Destination $targetPath -Force
        Write-Log "DLL copied to: $targetPath" -Level SUCCESS

        # Copy License.txt if requested (only once, from first extraction)
        if ($CopyLicense) {
            $extractedLicense = Get-ChildItem -Path $extractPath -Filter "License.txt" -Recurse -ErrorAction SilentlyContinue |
                                Select-Object -First 1 -ExpandProperty FullName

            if ($extractedLicense) {
                $licensePath = Join-Path $script:LibsPath "License.txt"
                Copy-Item -Path $extractedLicense -Destination $licensePath -Force
                Write-Log "License.txt copied from installer" -Level SUCCESS
            } else {
                Write-Log "License.txt not found in installer" -Level WARN
            }
        }

        return $targetPath
    } catch {
        Write-Log "Failed to extract DLL: $_" -Level ERROR
        throw
    }
}

function New-DllMetadata {
    <#
    .SYNOPSIS
        Creates metadata object for a DLL (hash, size, etc.).
    #>
    param(
        [string]$DllPath,
        [string]$Version,
        [string]$Architecture
    )

    $file = Get-Item $DllPath
    $hash = Get-FileHash -Path $DllPath -Algorithm SHA256

    $urlVersion = Get-VersionUrlFormat -Version $Version
    $source = switch ($Architecture) {
        "x86"   { "https://www.7-zip.org/a/7z$urlVersion.exe" }
        "x64"   { "https://www.7-zip.org/a/7z$urlVersion-x64.exe" }
        "ARM64" { "https://www.7-zip.org/a/7z$urlVersion-arm64.exe" }
    }

    return @{
        architecture = $Architecture
        version = $Version
        source = $source
        sha256 = $hash.Hash
        size = $file.Length
    }
}

function Update-VersionTracking {
    <#
    .SYNOPSIS
        Updates Libs/7zip-version.json with new version information.
    #>
    param(
        [string]$Version,
        [hashtable]$DllMetadata
    )

    try {
        if ($WhatIf) {
            Write-Log "[WhatIf] Would update version tracking JSON" -Level WARN
            return
        }

        $versionData = @{
            version = $Version
            updated = (Get-Date).ToUniversalTime().ToString("o")
            dlls = $DllMetadata
        }

        $json = $versionData | ConvertTo-Json -Depth 10
        Set-Content -Path $script:VersionJsonPath -Value $json
        Write-Log "Version tracking updated: $script:VersionJsonPath" -Level SUCCESS
    } catch {
        Write-Log "Failed to update version tracking: $_" -Level ERROR
        throw
    }
}

function Set-GitHubOutput {
    <#
    .SYNOPSIS
        Sets output variables for GitHub Actions.
    #>
    param(
        [string]$Name,
        [string]$Value
    )

    if ($env:GITHUB_OUTPUT) {
        Add-Content -Path $env:GITHUB_OUTPUT -Value "$Name=$Value"
        Write-Log "GitHub Action output set: $Name=$Value"
    }
}

# Main execution
try {
    Write-Log "7-Zip DLL Update Script"
    Write-Log "Repository: $script:RepoRoot"

    # Validate parameters
    if (-not $CheckOnly -and -not $Update) {
        Write-Log "Error: Must specify either -CheckOnly or -Update" -Level ERROR
        exit 1
    }

    # Get version information
    $latestVersion = if ($TargetVersion) {
        Write-Log "Using target version: $TargetVersion"
        $TargetVersion
    } else {
        Get-Latest7ZipVersion
    }

    $currentVersion = Get-Current7ZipVersion

    # Compare versions
    $updateAvailable = $false
    if ($currentVersion) {
        $currentVer = ConvertTo-VersionNumber -VersionString $currentVersion
        $latestVer = ConvertTo-VersionNumber -VersionString $latestVersion

        if ($latestVer -gt $currentVer) {
            $updateAvailable = $true
            Write-Log "Update available: $currentVersion -> $latestVersion" -Level SUCCESS
        } else {
            Write-Log "Already up to date: $currentVersion"
        }
    } else {
        $updateAvailable = $true
        Write-Log "No current version found, will install: $latestVersion" -Level WARN
    }

    # Set GitHub Actions outputs
    if ($CheckOnly) {
        Set-GitHubOutput -Name "update_available" -Value ($updateAvailable.ToString().ToLower())
        Set-GitHubOutput -Name "new_version" -Value $latestVersion

        if ($updateAvailable) {
            Write-Log "Check complete: Update available to version $latestVersion" -Level SUCCESS
            exit 0
        } else {
            Write-Log "Check complete: No update needed" -Level SUCCESS
            exit 0
        }
    }

    # Perform update
    if ($Update) {
        if (-not $updateAvailable -and -not $TargetVersion) {
            Write-Log "No update needed, current version is latest: $currentVersion"
            exit 0
        }

        Write-Log "Starting update to version $latestVersion..."

        # Create temp directory
        if (-not $WhatIf) {
            New-Item -ItemType Directory -Path $script:TempPath -Force | Out-Null
        }

        # Download and extract each architecture
        $architectures = @("x86", "x64", "ARM64")
        $dllMetadata = @{}

        foreach ($arch in $architectures) {
            Write-Log "Processing $arch architecture..."

            # Download installer
            $installerPath = Download-7ZipInstaller -Version $latestVersion -Architecture $arch

            # Extract DLL (copy License.txt only from first extraction)
            $extractParams = @{
                InstallerPath = $installerPath
                Architecture = $arch
            }
            if ($arch -eq "x86") {
                $extractParams.CopyLicense = $true
            }
            $dllPath = Extract-DllFromInstaller @extractParams

            # Create metadata
            if (-not $WhatIf) {
                $dllName = Split-Path -Leaf $dllPath
                $dllMetadata[$dllName] = New-DllMetadata -DllPath $dllPath -Version $latestVersion -Architecture $arch
            }
        }

        # Update version tracking
        if (-not $WhatIf) {
            Update-VersionTracking -Version $latestVersion -DllMetadata $dllMetadata
        }

        # Cleanup temp directory
        if (-not $WhatIf -and (Test-Path $script:TempPath)) {
            Remove-Item -Path $script:TempPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Log "Cleaned up temporary files"
        }

        if ($WhatIf) {
            Write-Log "[WhatIf] Update simulation complete" -Level SUCCESS
        } else {
            Write-Log "Update complete! All DLLs updated to version $latestVersion" -Level SUCCESS
        }
    }

} catch {
    Write-Log "Script failed: $_" -Level ERROR
    Write-Log $_.ScriptStackTrace -Level ERROR
    exit 1
}
