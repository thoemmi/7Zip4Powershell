# 7Zip4Powershell Tests

This directory contains Pester tests for the 7Zip4Powershell module.

## Running Tests Locally

### Prerequisites
- PowerShell 5.1 or PowerShell 7+
- .NET SDK (for building the project)

### Quick Start

The easiest way to run tests is using the provided test script:

```powershell
# Run all tests (builds project, prepares module, installs Pester, runs tests)
.\Scripts\Test.ps1
```

### Additional Options

```powershell
# Run tests without rebuilding (if you already built)
.\Scripts\Test.ps1 -SkipBuild

# Run tests with Debug configuration
.\Scripts\Test.ps1 -Configuration Debug

# Run with verbose output
.\Scripts\Test.ps1 -Verbose
```

### Manual Test Execution

If you prefer to run tests manually:

```powershell
# Build the project
dotnet build --configuration Release

# Copy module files (required for tests)
New-Item -ItemType Directory -Path "Module\7Zip4Powershell" -Force
Copy-Item -Path "7Zip4Powershell\bin\Release\netstandard2.0\*.*" `
          -Exclude "JetBrains.Annotations.dll" `
          -Destination "Module\7Zip4Powershell" -Force

# Install Pester v5 if not already installed
Install-Module -Name Pester -Force -MinimumVersion 5.0.0

# Run tests
Invoke-Pester -Path .\Tests\7Zip4Powershell.Tests.ps1 -Output Detailed
```

## Test Structure

- **7Zip4Powershell.Tests.ps1**: Main smoke tests covering compress/expand operations
- **TestData/**: Sample files used for testing (auto-created if missing)

## Test Coverage

The smoke tests verify:
- Compression to .7z, .zip, and .tar.gz formats
- Extraction from all formats
- Password-protected archive creation and extraction
- SecureString password support
- Content integrity after compress/extract cycle

## CI Integration

Tests run automatically in the PR Build workflow on GitHub Actions:
- Executes after successful build
- Fails the build if any test fails
- Expected execution time: < 30 seconds
