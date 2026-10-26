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

These outputs are used by the automated workflow to create or update a PR on the fixed `update-7zip-libraries` branch. If a newer version is detected while that PR is still open, the same PR is updated with the latest DLLs, title, and description.

### Automatic PR Checks and GitHub App Setup

The updater uses a dedicated GitHub App installation token to create or update its PR, so the existing `PR Build` workflow starts automatically. PRs created with `GITHUB_TOKEN` require manual workflow approval; see [GitHub's token documentation](https://docs.github.com/en/actions/concepts/security/github_token#when-github_token-triggers-workflow-runs).

Set up the App before enabling this change on `master`:

1. [Register a GitHub App](https://docs.github.com/en/apps/creating-github-apps/registering-a-github-app/registering-a-github-app) under the repository owner's account, for example `7Zip4Powershell Updater`. Use the repository URL as its homepage, disable webhooks, and allow installation only on this account.
2. Grant only the repository permissions **Contents: Read and write** and **Pull requests: Read and write**. Metadata read access is implicit. No Actions, Workflows, or administration permissions are needed.
3. Install the App on **only `thoemmi/7Zip4Powershell`**.
4. In the repository's **Settings → Secrets and variables → Actions**, add the App's **Client ID** as the repository variable `SEVENZIP_UPDATER_APP_CLIENT_ID`.
5. Generate a private key in the App settings and store the complete PEM file as the repository secret `SEVENZIP_UPDATER_APP_PRIVATE_KEY`. Never commit it or paste it into a PR.

The automation is limited to the existing scheduled/manual update detector on `master` in this repository. Manual runs on other branches are skipped. An App token is requested only after a newer version has been detected and that exact version has been downloaded successfully. The token is limited to this repository and the two permissions above, and is revoked when the job finishes. Without a new version, the workflow does not request a token or create a PR; if App configuration is missing when an update is found, the run fails rather than falling back to `GITHUB_TOKEN`.

The PR action stages only the three DLLs, `License.txt`, and `7zip-version.json` in `Libs/`. Runs are serialized to avoid concurrent changes to the shared update branch. The token's Contents permission is repository-wide; the file restriction is enforced by the workflow's `add-paths` list, not by GitHub's permission model.

`PR Build` retains its normal `pull_request` trigger, uses a read-only token, and does not receive the App key. No workflow automatically approves arbitrary bot or fork PRs. App-created update PRs use the same Release build and Pester tests as other PRs; successful checks do not merge or publish a release.

To verify the setup, run **Update 7-Zip DLLs** on `master` when a newer release is available. Confirm that the PR is created by the dedicated App and that **PR Build → build** starts without an approval prompt. A no-update run verifies detection only, not the App configuration or automatic PR checks.

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

The workflow will check for updates and create or update the PR if a new version is available.

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
