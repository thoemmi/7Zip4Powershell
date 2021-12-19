[CmdLetBinding()]
param(
	[Parameter(Mandatory=$false)]
	[string]$Configuration = "Release",

	[Parameter(Mandatory=$true)]
	[string]$NuGetApiKey
)

# The environment variable MSBUILDSINGLELOADCONTEXT must be set to get GitVersion task working with MSBuild 16.5
# See https://github.com/GitTools/GitVersion/issues/2063
$env:MSBUILDSINGLELOADCONTEXT = 1

# compile
& dotnet build --configuration $Configuration

# For publishing we need a folder with the same name as the module
$moduleTargetPath = Join-Path $PSScriptRoot "Module" "7Zip4Powershell"
if (Test-Path $moduleTargetPath) {
	Remove-Item $moduleTargetPath -Recurse
}
New-Item $moduleTargetPath -ItemType Directory | Out-Null

# copy all required files to that folder
Copy-Item -Path (Join-Path $PSScriptRoot "7Zip4Powershell" "bin" $configuration "netstandard2.0" "*.*") -Exclude "JetBrains.Annotations.dll" -Destination $moduleTargetPath

# determine the version
dotnet tool restore
$versionInfo = dotnet tool run dotnet-gitversion | ConvertFrom-Json
$version = "$($versionInfo.Major).$($versionInfo.Minor).$($versionInfo.Patch)"
$prerelease = $versionInfo.NuGetPreReleaseTagV2

# patch the version in the .PSD1 file
$psd1File = Join-Path $moduleTargetPath "7Zip4PowerShell.psd1"
Write-Host "Patching version in $psd1File file to $version"
(((Get-Content $psd1File -Raw) -replace '\$version\$',$version) -replace '\$prerelease\$',$prerelease) | Set-Content $psd1File

# finally publish the 
Publish-Module -Path $moduleTargetPath -NuGetApiKey $NuGetApiKey