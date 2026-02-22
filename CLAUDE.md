# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

7Zip4Powershell is a PowerShell module that provides cmdlets for creating and extracting 7-Zip archives. It's a .NET Standard 2.0 library that wraps the SevenZipSharp library and integrates with PowerShell's WriteProgress API.

**Key Components:**
- `Compress-7Zip` - Creates compressed archives
- `Expand-7Zip` - Extracts archives  
- `Get-7Zip` - Lists files in archives
- `Get-7ZipInformation` - Gets archive metadata

## Architecture

The project uses a simple architecture:

- **Main Project**: `7Zip4Powershell/` contains the C# cmdlet implementations
- **Core Classes**: Each cmdlet is implemented as a separate class inheriting from `ThreadedCmdlet`
- **Dependencies**: Uses SevenZipSharp (v1.6.2.24) wrapper around native 7-Zip DLLs
- **Module Distribution**: Built module goes to `Module/7Zip4Powershell/` directory

**Key Files:**
- `Compress7Zip.cs` - Compression cmdlet implementation
- `Expand7Zip.cs` - Extraction cmdlet implementation  
- `Get7Zip.cs` - Archive listing cmdlet
- `Get7ZipInformation.cs` - Archive info cmdlet
- `ThreadedCmdlet.cs` - Base class providing progress reporting
- `Utils.cs` - Shared utility functions

## Development Commands

### Build
```bash
# Restore dependencies
dotnet restore

# Build the project (Release configuration)
dotnet build --configuration Release --no-restore

# Build Debug configuration
dotnet build --configuration Debug --no-restore
```

### Version Management
The project uses GitVersion for automatic versioning:
- Version is determined from Git tags and branch structure
- Configuration in `GitVersion.yml` uses TrunkBased workflow
- Build outputs include version info automatically

### Module Preparation
The GitHub Actions workflow shows how to prepare the module for distribution:
```powershell
# Copy built files to module directory (excluding JetBrains.Annotations.dll)
Copy-Item -Path "7Zip4Powershell\bin\Release\netstandard2.0\*.*" -Exclude "JetBrains.Annotations.dll" -Destination "Module\7Zip4Powershell"

# Replace version template variables in PSD1 (done automatically in CI)
# The PSD1 contains $version$ and $prerelease$ placeholders replaced at release time
```

**Note:** `7Zip4PowerShell.psd1` uses `$version$` and `$prerelease$` template variables. The release workflow replaces these from GitVersion outputs. For local use, the module loads fine without replacement.

## Testing

```powershell
# Run all tests (builds, prepares module, installs Pester, runs tests)
.\Scripts\Test.ps1

# Skip rebuild if already built
.\Scripts\Test.ps1 -SkipBuild

# Debug configuration
.\Scripts\Test.ps1 -Configuration Debug
```

Uses Pester v5. Tests cover: compress/expand, .7z/.zip/.tar.gz formats, password-protected archives (including SecureString), and content integrity. See `Tests/README.md` for full details. Expected CI runtime: < 30 seconds.

## Project Structure Notes

- **Solution**: Single project solution file (`7Zip4Powershell.sln`)
- **Target Framework**: .NET Standard 2.0 for broad PowerShell compatibility
- **Native Dependencies**: Includes both 32-bit (`7z.dll`) and 64-bit (`7z64.dll`) 7-Zip libraries, plus ARM64 (`7zARM64.dll`)
- **Module Manifest**: `7Zip4Powershell.psd1` defines the PowerShell module structure

## 7-Zip DLL Updates

The native 7-Zip DLLs are automatically kept up to date through a scheduled automation system.

### Automated Updates

The project uses a GitHub Actions workflow that:
- **Schedule**: Runs every Monday at 2 AM UTC
- **Process**: Checks 7-zip.org for new releases and creates PRs with updated DLLs
- **Architectures**: Updates all three DLLs (x86, x64, ARM64) simultaneously
- **Files Updated**:
  - `Libs/7z.dll` (x86)
  - `Libs/7z64.dll` (x64)
  - `Libs/7zARM64.dll` (ARM64)
  - `Libs/License.txt` (copied from installer)
  - `Libs/7zip-version.json` (version tracking metadata)

### Manual Updates

To manually check for or install updates:

```powershell
# Check if update is available
.\Scripts\Update-7ZipDlls.ps1 -CheckOnly -Verbose

# Perform update to latest version
.\Scripts\Update-7ZipDlls.ps1 -Update -Verbose

# Update to specific version (for testing)
.\Scripts\Update-7ZipDlls.ps1 -Update -TargetVersion "25.01" -Verbose

# Dry run (preview changes without modifying files)
.\Scripts\Update-7ZipDlls.ps1 -Update -WhatIf
```

### Version Tracking

Current DLL versions and metadata are tracked in `Libs/7zip-version.json`:
- Version numbers for each architecture
- SHA256 checksums for integrity verification
- Source download URLs from 7-zip.org
- File sizes and update timestamps

### Troubleshooting

**Script fails to download**: Check internet connection and verify 7-zip.org is accessible

**Extraction fails**: Ensure sufficient disk space and write permissions to Libs/ folder

**Version detection fails**: The 7-zip.org HTML format may have changed; check download page structure

**Manual workflow trigger**: Go to Actions → Update 7-Zip DLLs → Run workflow

For more details, see `Scripts/README.md`.

## CI/CD

The project uses GitHub Actions:
- **PR Build** (`pr-build.yml`): Builds, runs tests on every PR targeting master
- **Release and Publish** (`release-publish.yml`): Runs on every push to master and on manual dispatch; the `release` job (PowerShell Gallery publish + GitHub Release) only runs on `v*` tags
- **Update 7-Zip DLLs** (`update-7zip-dlls.yml`): Scheduled Monday 2 AM UTC; creates PRs with updated native DLLs (see "7-Zip DLL Updates" section above)

## Important Notes

- This is a legacy project - the original maintainer notes it's not actively maintained
- The codebase is stable and focused on specific use cases
- Uses PowerShellStandard.Library (v5.1.1) for cmdlet base classes
- Native 7-Zip DLLs are copied to output directory during build