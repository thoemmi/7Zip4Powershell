# Scripts Directory

## Update-7ZipDlls.ps1

Automates downloading and updating native 7-Zip DLL files for all supported architectures.

### Usage

**Check for updates** (read-only):
```powershell
.\Update-7ZipDlls.ps1 -CheckOnly -Verbose
```

**Perform update** to latest version:
```powershell
.\Update-7ZipDlls.ps1 -Update
```

**Update to specific version**:
```powershell
.\Update-7ZipDlls.ps1 -Update -TargetVersion "25.01"
```

**Dry run** (preview changes without modifying files):
```powershell
.\Update-7ZipDlls.ps1 -Update -WhatIf
```

### Parameters

- **`-CheckOnly`**: Only check if an update is available, don't download or update files. Sets GitHub Actions output variables.
- **`-Update`**: Download and install updated DLLs for all architectures.
- **`-TargetVersion`**: Specify exact version to download (e.g., "25.01"). Default: latest from 7-zip.org.
- **`-WhatIf`**: Show what would happen without making any changes. Dry run mode.
- **`-Verbose`**: Show detailed progress information.

### What It Does

1. **Fetches latest version** from 7-zip.org/download.html
2. **Compares** with current version in `Libs/7zip-version.json`
3. **If newer version found:**
   - Downloads EXE installers for x86, x64, and ARM64 architectures
   - Extracts native DLLs using self-extraction or 7-Zip
   - Validates DLL architecture by inspecting PE headers
   - Updates `Libs/` folder with new DLLs
   - Copies `License.txt` from the installer
   - Updates version tracking in `Libs/7zip-version.json`

### Output for GitHub Actions

When run with `-CheckOnly`, the script sets output variables for GitHub Actions:
- `update_available`: "true" or "false"
- `new_version`: Version number if update is available

These outputs are used by the automated workflow to determine if a PR should be created.

### Error Handling

The script includes comprehensive error handling:
- **Network failures**: Retries downloads with exponential backoff (3 attempts)
- **Download corruption**: Validates file sizes and checksums
- **Extraction failures**: Checks that DLLs exist after extraction
- **Architecture mismatch**: Validates PE headers to ensure correct architecture

### Files Modified

When an update is performed, the following files are modified:
- `Libs/7z.dll` - x86 32-bit DLL
- `Libs/7z64.dll` - x64 64-bit DLL
- `Libs/7zARM64.dll` - ARM64 DLL
- `Libs/License.txt` - License from 7-Zip installer
- `Libs/7zip-version.json` - Version metadata and checksums

### Testing

**Test with dry run** to preview changes:
```powershell
.\Update-7ZipDlls.ps1 -Update -TargetVersion "25.01" -WhatIf -Verbose
```

**Test with current version** (should be no-op):
```powershell
.\Update-7ZipDlls.ps1 -Update -TargetVersion "24.09" -Verbose
```

**Verify DLLs after update**:
```powershell
# Check file hashes
Get-FileHash .\Libs\*.dll -Algorithm SHA256

# View version metadata
Get-Content .\Libs\7zip-version.json | ConvertFrom-Json

# Build and test the project
dotnet build --configuration Release
```

### Troubleshooting

**"Failed to fetch latest version"**
- Check internet connection
- Verify 7-zip.org is accessible
- Check if the HTML format of the download page has changed

**"Download failed after 3 attempts"**
- Verify the version exists on 7-zip.org
- Check firewall settings
- Try with `-TargetVersion` to specify a known good version

**"DLL not found in extracted EXE"**
- The installer structure may have changed
- Check the extraction path manually
- Report as an issue if the script needs updating

**"Architecture mismatch"**
- The downloaded DLL doesn't match the expected architecture
- This indicates a problem with the download source
- Do not use these DLLs - report the issue

**"EXE extraction failed"**
- Ensure you have 7z.exe available or the installer can self-extract
- Check available disk space
- Verify the downloaded EXE is not corrupted

### Manual Workflow Trigger

To manually trigger the automated GitHub Actions workflow:
1. Go to **Actions** tab in GitHub
2. Select **Update 7-Zip DLLs** workflow
3. Click **Run workflow**
4. Choose branch (usually `master`)
5. Click **Run workflow** button

The workflow will check for updates and create a PR if a new version is available.

### Development Notes

- Script requires **PowerShell Core (pwsh)** for full compatibility
- Uses `Get-FileHash` cmdlet (PowerShell 4.0+)
- Temporary files stored in `Scripts/temp/` directory
- Automatic cleanup after successful update
- Supports WhatIf for safe testing

### Version URL Format

7-Zip uses a compressed version format in URLs:
- Version `25.01` → URL contains `2501`
- Version `24.09` → URL contains `2409`

Download URLs (all EXE format):
- x86: `https://www.7-zip.org/a/7z{version}.exe`
- x64: `https://www.7-zip.org/a/7z{version}-x64.exe`
- ARM64: `https://www.7-zip.org/a/7z{version}-arm64.exe`
