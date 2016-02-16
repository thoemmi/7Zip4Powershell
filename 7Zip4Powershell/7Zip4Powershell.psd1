@{

GUID = 'bd4390dc-a8ad-4bce-8d69-f53ccf8e4163'
Author = 'Thomas Freudenberg'
Description = 'Powershell module for creating and extracting 7-Zip archives'
CompanyName = 'N/A'
Copyright = '© 2016 Thomas Freudenberg'
ModuleVersion = '$version$'
PowerShellVersion = '2.0'
HelpInfoUri = "https://github.com/thoemmi/7Zip4Powershell"

NestedModules = @("7Zip4PowerShell.dll")
CmdletsToExport = @(
	"Expand-7Zip",
	"Compress-7Zip"
)
}