7Zip4Powershell
===============

Powershell module for creating and extracting 7-Zip archives

The syntax is simple as this:

    Expand-7Zip 
        [-ArchiveFileName] <string> 
        [-TargetPath] <string>  
        [<CommonParameters>]
 
    Compress-7Zip 
        [-ArchiveFileName] <string> 
        [-Path] <string> 
        [[-Filter] <string>] 
        [-Format <OutArchiveFormat> {SevenZip | Zip | GZip | BZip2 | Tar | XZ}] 
        [-CompressionLevel <CompressionLevel> {None | Fast | Low | Normal | High | Ultra}] 
        [-CompressionMethod <CompressionMethod> {Copy | Deflate | Deflate64 | BZip2 | Lzma | Lzma2 | Ppmd | Default}] 
        [<CommonParameters>]

It works with both x86 and x64 and uses [SevenZipSharp](https://sevenzipsharp.codeplex.com/) as a wrapper around 7zip’s API.

7Zip4Powershell is also [distributed as a NuGet package](https://nuget.org/packages/7Zip4Powershell/).
