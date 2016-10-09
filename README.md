# 7Zip4Powershell

Powershell module for creating and extracting 7-Zip archives supporting Powershell's `WriteProgress` API.

![Screenshot](https://raw.githubusercontent.com/thoemmi/7Zip4Powershell/master/Assets/compression.gif)

## Usage

The syntax is simple as this:

```powershell
Expand-7Zip
    [-ArchiveFileName] <string> 
    [-TargetPath] <string>  
    [-Password <string>]
    [-CustomInitialization <ScriptBlock>]
    [<CommonParameters>]

Compress-7Zip
    [-ArchiveFileName] <string> 
    [-Path] <string> 
    [[-Filter] <string>] 
    [-Format <OutputFormat> {Auto | SevenZip | Zip | GZip | BZip2 | Tar | XZ}] 
    [-CompressionLevel <CompressionLevel> {None | Fast | Low | Normal | High | Ultra}] 
    [-CompressionMethod <CompressionMethod> {Copy | Deflate | Deflate64 | BZip2 | Lzma | Lzma2 | Ppmd | Default}]
    [-Password <string>]
    [-CustomInitialization <ScriptBlock>]
    [-EncryptFilenames]
    [-VolumeSize <int>]
    [<CommonParameters>]

Get-7Zip
    [-ArchiveFileName] <string[]>
    [-Password <string>]
    [<CommonParameters>]

Get-7ZipInformation
    [-ArchiveFileName] <string[]> 
    [-Password <string>]
    [<CommonParameters>]
```

It works with both x86 and x64 and uses [SevenZipSharp](https://sevenzipsharp.codeplex.com/) as a wrapper around 7zip’s API.

[Jason Fossen](https://github.com/JasonFossen) wrote the article [PowerShell 7-Zip Module Versus Compress-Archive with Encryption](https://cyber-defense.sans.org/blog/2016/06/06/powershell-7-zip-compress-archive-encryption)
where he describes some usage scenarios with 7Zip4PowerShell.

## Where to get it

7Zip4Powershell is published [as a NuGet package](https://nuget.org/packages/7Zip4Powershell/) and [at PowerShell Gallery](https://www.powershellgallery.com/packages/7Zip4Powershell).

[![NuGet](https://img.shields.io/nuget/v/7Zip4Powershell.svg?maxAge=2592000)](https://nuget.org/packages/7Zip4Powershell/)
[![https://www.powershellgallery.com/packages/7Zip4Powershell](https://img.shields.io/badge/PowerShell%20Gallery-download-blue.svg)](https://www.powershellgallery.com/packages/7Zip4Powershell)

## Customization

Both `Compress-7Zip` and `Expand-7Zip` accept script blocks for customization. The script blocks get passed the current
`SevenZipCompressor` and `SevenZipExtractor` instance respectively. E.g. you can set the multithread mode this way:

```powershell
$initScript = {
    param ($compressor)
    $compressor.CustomParameters.Add("mt", "off")
}

Compress-7Zip -Path . -ArchiveFileName demo.7z -CustomInitialization $initScript
```

A list of all custom parameters can be found [here](https://sevenzip.osdn.jp/chm/cmdline/switches/method.htm).

## Motivation

I've written and maintaining the module just for fun and to serve my own needs. If
it's useful for you too, that's great. I don't demand anything in return. 

However, if you like this module and feel the urge to give something back, a coffee 
or a beer is always appreciated. Thank you very much in advance.

[![PayPal.me](https://img.shields.io/badge/PayPal-me-blue.svg?maxAge=2592000)](https://www.paypal.me/ThomasFreudenberg)