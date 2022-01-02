# 7Zip4Powershell

Powershell module for creating and extracting 7-Zip archives supporting Powershell's `WriteProgress` API.

![Screenshot](https://raw.githubusercontent.com/thoemmi/7Zip4Powershell/master/Assets/compression.gif)


> # Note
> Please note that this repository is not maintained anymore. I've created it a couple
> of years ago to fit my own needs (just compressing a single folder). I love that lots
> of other users find my package helpful.
>
> I really appreciated if you report issues or suggest new feature. However,
> I don't use this package myself anymore, and I don't have the time to
> maintain it appropriately. So please don't expect me to fix any bugs. Any Pull
> Request is welcome though.

## Usage

The syntax is simple as this:

```powershell
Expand-7Zip
    [-ArchiveFileName] <string>
    [-TargetPath] <string>
    [-Password <string>] | [-SecurePassword <securestring>]
    [<CommonParameters>]

Compress-7Zip
    [-ArchiveFileName] <string>
    [-Path] <string>
    [[-Filter] <string>]
    [-OutputPath] <string>
    [-Format <OutputFormat> {Auto | SevenZip | Zip | GZip | BZip2 | Tar | XZ}]
    [-CompressionLevel <CompressionLevel> {None | Fast | Low | Normal | High | Ultra}]
    [-CompressionMethod <CompressionMethod> {Copy | Deflate | Deflate64 | BZip2 | Lzma | Lzma2 | Ppmd | Default}]
    [-Password <string>] | [-SecurePassword <securestring>]
    [-CustomInitialization <ScriptBlock>]
    [-EncryptFilenames]
    [-VolumeSize <int>]
    [-FlattenDirectoryStructure]
    [-SkipEmptyDirectories]
    [-PreserveDirectoryRoot]
    [-DisableRecursion]
    [-Append]
    [<CommonParameters>]

Get-7Zip
    [-ArchiveFileName] <string[]>
    [-Password <string>] | [-SecurePassword <securestring>]
    [<CommonParameters>]

Get-7ZipInformation
    [-ArchiveFileName] <string[]>
    [-Password <string>] | [-SecurePassword <securestring>]
    [<CommonParameters>]
```

It works with both x86 and x64 and uses [SevenZipSharp](https://sevenzipsharp.codeplex.com/) as a wrapper around 7zip’s API.

[Jason Fossen](https://github.com/JasonFossen) wrote the article [PowerShell 7-Zip Module Versus Compress-Archive with Encryption](https://cyber-defense.sans.org/blog/2016/06/06/powershell-7-zip-compress-archive-encryption)
where he describes some usage scenarios with 7Zip4PowerShell.

## Where to get it

7Zip4Powershell is published at [PowerShell Gallery](https://www.powershellgallery.com/packages/7Zip4Powershell).


[![https://www.powershellgallery.com/packages/7Zip4Powershell](https://img.shields.io/powershellgallery/v/7Zip4Powershell)](https://www.powershellgallery.com/packages/7Zip4Powershell)

## Customization

`Compress-7Zip` accepts a script block for customization. The script block gets passed the current
`SevenZipCompressor` instance. E.g. you can set the multithread mode this way:

```powershell
$initScript = {
    param ($compressor)
    $compressor.CustomParameters.Add("mt", "off")
}

Compress-7Zip -Path . -ArchiveFileName demo.7z -CustomInitialization $initScript
```

A list of all custom parameters can be found [here](https://sevenzip.osdn.jp/chm/cmdline/switches/method.htm).

## Changelog

### [v2.1](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v2.1)

* Updates 7-Zip libraries to 21.07 (contributed by [@kborowinski](https://github.com/kborowinski) in [#75](https://github.com/thoemmi/7Zip4Powershell/pull/75))

### [v2.0](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v2.0)

* Now based on .NET Standard 2.0 (thanks to [@kborowinski](https://github.com/kborowinski) for testing)

### [v1.13](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.13)

* Improved handling of paths in `ArchiveFileName`
  ([#63](https://github.com/thoemmi/7Zip4Powershell/pull/63) and [#65](https://github.com/thoemmi/7Zip4Powershell/pull/65), contributed by [@iRebbok](https://github.com/iRebbok))
* Updated readme
  ([#64](https://github.com/thoemmi/7Zip4Powershell/pull/64), contributed by [@kborowinski](https://github.com/kborowinski))

### [v1.12](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.12)

* Uses PowerShell 5 reference assembly, which reduces the package size dramatically
  ([#61](https://github.com/thoemmi/7Zip4Powershell/pull/61), contributed by [@kborowinski](https://github.com/kborowinski))

### [v1.11](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.11)

* Replaces *SevenZipSharp.Net45* with *Squid-Box.SevenZipSharp* library and adds new parameter `PreserveDirectoryRoot` for `Compress-7zip`.
  ([#57](https://github.com/thoemmi/7Zip4Powershell/pull/57), contributed by [@kborowinski](https://github.com/kborowinski))
* Adds new parameter `OutputPath` for `Compress-7Zip`
  ([#60](https://github.com/thoemmi/7Zip4Powershell/pull/60), contributed by [@iRebbok](https://github.com/iRebbok))

### [v1.10](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.10)

* Updated 7-Zip dlls to 19.00 ([#56](https://github.com/thoemmi/7Zip4Powershell/pull/56), contributed by [@kborowinski](https://github.com/kborowinski))

### [v1.9](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.9)

* Updated 7-Zip dlls to 16.04
* Disabled the `CustomInitialization` parameter for `Expand-7Zip`, will be removed in future versions.

### [v1.8](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.8)

January 25, 2017

* Added optional `SecurePassword` parameter of type `SecureString` to all cmdlets. (#34, #36)

### [v1.7.1](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.7.1)

October 27, 2016

* Compression with password encryption could cause an exception (#33)

### [v1.7](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.7)

October 16, 2016

* If `Format` is not specified, it is inferred from the file extension of `ArchiveFileName` (#24, proposed by @onyxhat)
* Added new parameter `VolumeSize` to specify the colume size for `Compress-7Zip` (#25, proposed by @rgel)
* Added new switches `FlattenDirectoryStructure`, `SkipEmptyDirectories`, and `DisableRecursion` to `Compress-7Zip` (#27, contributed by @itmagination)
* Added new switch `Append` to `Compress-7Zip` to append files to an existing archive (#30, inspired by @itmagination)

### [v1.6](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.6)

June 15, 2016

* added `Get-7ZipInformation` cmdlet
* use default compression method in `Compress-7Zip` (previously it was PPMd, for whatever reason) (#11)
* allow piped input for `Get-7Zip` (#15)
* use `WriteDebug` instead of `Write` of logging (#13)

### [v1.5](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.5)

June 5, 2016

* Added parameter `-EncryptFilenames` to `Compress-7Zip` (#10, requested by @JasonFossen)

### [v1.4](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.4)

May 29, 2016

* Added `Get-7Zip` to get a list of files in an archive (#9, contributed by @gigi81)

### [v1.3](https://github.com/thoemmi/7Zip4Powershell/releases/tag/v1.3)

30 March, 2016

* Added `Password` parameter to both `Compress-7Zip` and `Expand-7Zip` (#8)

## Motivation

I've written and maintaining the module just for fun and to serve my own needs. If
it's useful for you too, that's great. I don't demand anything in return.

However, if you like this module and feel the urge to give something back, a coffee
or a beer is always appreciated. Thank you very much in advance.

[![PayPal.me](https://img.shields.io/badge/PayPal-me-blue.svg?maxAge=2592000)](https://www.paypal.me/ThomasFreudenberg)
