@{
GUID = 'bd4390dc-a8ad-4bce-8d69-f53ccf8e4163'
Author = 'Thomas Freudenberg'
Description = 'Powershell module for creating and extracting 7-Zip archives'
CompanyName = 'N/A'
Copyright = '2013-2021 Thomas Freudenberg'
DotNetFrameworkVersion = '4.7.2'
ModuleVersion = '$version$'
PowerShellVersion = '5.0'
PrivateData = @{
    PSData = @{
        Tags = @('powershell', '7zip', '7-zip', 'zip', 'archive', 'extract', 'compress', 'PSEdition_Core', 'PSEdition_Desktop', 'Windows')
        LicenseUri = 'https://github.com/thoemmi/7Zip4Powershell/blob/master/LICENSE'
        ProjectUri = 'https://github.com/thoemmi/7Zip4Powershell'
        IconUri = 'https://raw.githubusercontent.com/thoemmi/7Zip4Powershell/master/Assets/7zip4powershell.png'
        RequireLicenseAcceptance = $false
        PreRelease = '$prerelease$'
        # ReleaseNotes = ''
    } # End of PSData hashtable
}

NestedModules = @("7Zip4PowerShell.dll")
CmdletsToExport = @(
    "Expand-7Zip",
    "Compress-7Zip",
    "Get-7Zip",
    "Get-7ZipInformation")
}