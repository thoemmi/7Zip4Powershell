# Pester tests for https://github.com/thoemmi/7Zip4Powershell.git
# This script must be in the Tests folder.
# To use, install the Pester module, run Invoke-Pester while in Tests folder.
# Version: 1.0, Date: 20.Jun.2016

#region SetupTestFiles
# Confirm we are in the Tests folder, delete any previous temp files:
if ($PWD.Path -notmatch '.+\\Tests$'){"ERROR: Must be in Tests folder!" ; exit }
del -Path .\TestFiles -Force -Recurse -ErrorAction SilentlyContinue


# Create test files to compress in .\TestFiles:
mkdir TestFiles -ErrorAction Stop | Out-Null
"A" * 129 | Set-Content .\TestFiles\AAA.txt -Encoding UTF8 -ErrorAction Stop
"B" * 254 | Set-Content .\TestFiles\BBB.txt -Encoding Unicode -ErrorAction Stop
"C" * 511 | Set-Content .\TestFiles\CCC.txt -Encoding UTF32 -ErrorAction Stop
"D" * 513 | Set-Content .\TestFiles\DDD.txt -Encoding BigEndianUTF32 -ErrorAction Stop
[Byte[]] ([Int32]0..255) | Set-Content .\TestFiles\111.bin -Encoding Byte -ErrorAction Stop
(Get-Content -Path .\TestFiles\111.bin -Encoding Byte -TotalCount 67) | Add-Content .\TestFiles\111.bin -Encoding Byte -ErrorAction Stop
#endregion SetupTestFiles


Describe "How The 7Zip4PowerShell Module Loads" {
    It "Loads module of type: Manifest" {
        Get-Module -Name 7Zip4PowerShell | Remove-Module | Out-Null
        Import-Module -Name 7Zip4Powershell -ErrorAction Stop
        Get-Module -Name 7Zip4Powershell | Select-Object -ExpandProperty ModuleType | Should Be "Manifest"
    }
}


Describe "How Compress-7Zip Compresses Files" {
    It "Compresses AAA.txt using SevenZip to AAA.7z" {
        Compress-7Zip -Path .\TestFiles\AAA.txt -ArchiveFileName .\TestFiles\AAA.7z -Format SevenZip -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\AAA.7z | Should Exist
    }

    It "Compresses AAA.txt using SevenZip+LZMA2 to AAA-LZMA2.7z" {
        Compress-7Zip -Path .\TestFiles\AAA.txt -ArchiveFileName .\TestFiles\AAA-LZMA2.7z -Format SevenZip -CompressionMethod Lzma2 -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\AAA-LZMA2.7z | Should Exist
    } 

    It "Compresses BBB.txt using Zip+Deflate to BBB.zip" {
        Compress-7Zip -Path .\TestFiles\BBB.txt -ArchiveFileName .\TestFiles\BBB.zip -Format Zip -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\BBB.zip | Should Exist
    } 

    It "Compresses BBB.txt using Zip+Deflate64 to BBB-64.zip" {
        Compress-7Zip -Path .\TestFiles\BBB.txt -ArchiveFileName .\TestFiles\BBB-64.zip -Format Zip -CompressionMethod Deflate64 -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\BBB-64.zip | Should Exist
    } 

    It "Compresses CCC.txt using BZip2 to CCC-BZip2.bz2" {
        Compress-7Zip -Path .\TestFiles\CCC.txt -ArchiveFileName .\TestFiles\CCC-BZip2.bz2 -Format BZip2 -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\CCC-BZip2.bz2 | Should Exist
    } 

    # FAILS: Compress-7Zip -Path .\TestFiles\CCC.txt -ArchiveFileName .\TestFiles\CCC-BZip2.bz2 -Format BZip2 -CompressionMethod BZip2
    # Error Msg: Value does not fall within the expected range.

    It "Compresses CCC.txt using Tar to CCC.tar" {
        Compress-7Zip -Path .\TestFiles\CCC.txt -ArchiveFileName .\TestFiles\CCC.tar -Format Tar -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\CCC.tar | Should Exist
    } 

    It "Compresses CCC.txt using XZ to CCC.xz" {
        Compress-7Zip -Path .\TestFiles\CCC.txt -ArchiveFileName .\TestFiles\CCC.xz -Format XZ -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\CCC.xz | Should Exist
    } 

    It "Compresses 111.bin using Zip+Deflate+Ultra to 111-Ultra.zip" {
        Compress-7Zip -Path .\TestFiles\111.bin -ArchiveFileName .\TestFiles\111-Ultra.zip -Format Zip -CompressionMethod Deflate -CompressionLevel Ultra -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\111-Ultra.zip | Should Exist
    } 

    It "Compresses 111.bin using Zip+Deflate+Fast to 111-Fast.zip" {
        Compress-7Zip -Path .\TestFiles\111.bin -ArchiveFileName .\TestFiles\111-Fast.zip -Format Zip -CompressionMethod Deflate -CompressionLevel Fast -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\111-Fast.zip | Should Exist
    } 

    It "Compresses 111.bin using SevenZip+LZMA2+Ultra to 111-Ultra.7z" {
        Compress-7Zip -Path .\TestFiles\111.bin -ArchiveFileName .\TestFiles\111-Ultra.7z -Format SevenZip -CompressionMethod Lzma2 -CompressionLevel Ultra -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\111-Ultra.7z | Should Exist
    } 

    It "Compresses 111.bin using SevenZip+LZMA2+Fast to 111-Fast.7z" {
        Compress-7Zip -Path .\TestFiles\111.bin -ArchiveFileName .\TestFiles\111-Fast.7z -Format SevenZip -CompressionMethod Lzma2 -CompressionLevel Fast -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\111-Fast.7z | Should Exist
    } 

    It "Compresses *.txt using Zip+Deflate to ALL.zip" {
        dir .\TestFiles\*.txt -File | Compress-7Zip -ArchiveFileName .\TestFiles\ALL.zip -Format Zip -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\ALL.zip | Should Exist
    } 

    It "Compresses *.txt using Zip+Deflate64 to ALL-64.zip" {
        dir .\TestFiles\*.txt -File | Compress-7Zip -ArchiveFileName .\TestFiles\ALL-64.zip -Format Zip -CompressionMethod Deflate64 -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\ALL-64.zip | Should Exist
    } 

    It "Encrypts DDD.txt using SevenZip+LZMA2+AES to PlainFileNames.7z" {
        Compress-7Zip -Path .\TestFiles\DDD.txt -ArchiveFileName .\TestFiles\PlainFileNames.7z -Format SevenZip -Password "DasP@sswurd" -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\PlainFileNames.7z | Should Exist
    } 

    It "Encrypts DDD.txt using SevenZip+LZMA2+AES to EncryptedFileNames.7z" {
        Compress-7Zip -Path .\TestFiles\DDD.txt -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -Format SevenZip -Password "DasP@sswurd" -EncryptFilenames -ErrorAction Stop | Out-Null
        Get-Item .\TestFiles\EncryptedFileNames.7z | Should Exist
    } 

}


Describe "How Get-7Zip Shows Archive Details" {
    It "Reads AAA.7z" {
        Get-7Zip -ArchiveFileName .\TestFiles\AAA.7z -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "AAA.txt"
    }

    It "Reads AAA-LZMA2.7z" {
        Get-7Zip -ArchiveFileName .\TestFiles\AAA-LZMA2.7z -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "AAA.txt"
    }

    It "Reads BBB.zip" {
        Get-7Zip -ArchiveFileName .\TestFiles\BBB.zip -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "BBB.txt"
    }

    It "Reads BBB-64.zip" {
        Get-7Zip -ArchiveFileName .\TestFiles\BBB-64.zip -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "BBB.txt"
    }

    #### Not sure if the following is supposed to work/fail (the XZ format fails same way too...):
    #It "Reads CCC-Bzip2.bz2 Format" {
    #    Get-7Zip -ArchiveFileName .\TestFiles\CCC-BZip2.bz2 -ErrorAction Stop |
    #    Select-Object -ExpandProperty FileName | Should Be "CCC.txt"
    #}

    It "Reads CCC.tar Format" {
        Get-7Zip -ArchiveFileName .\TestFiles\CCC.tar -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "CCC.txt"
    }

    It "Reads ALL.zip" {
        $x = Get-7Zip -ArchiveFileName .\TestFiles\ALL.zip -ErrorAction Stop
        $x.GetType().FullName | Should Be "System.Object[]"
        $x.Count -gt 2 | Should Be $True
        $x = $null
    }

    It "Reads ALL-64.zip" {
        $x = Get-7Zip -ArchiveFileName .\TestFiles\ALL-64.zip -ErrorAction Stop
        $x.GetType().FullName | Should Be "System.Object[]"
        $x.Count -gt 2 | Should Be $True
        $x = $null
    }

    It "Reads PlainFileNames.7z with Correct Password" {
        Get-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -Password "DasP@sswurd" -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "DDD.txt"
    }

    It "Reads PlainFileNames.7z with Incorrect Password" {
        Get-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -Password "WrongPassword" -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "DDD.txt"
    }

    It "Reads PlainFileNames.7z with No Password" {
        Get-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "DDD.txt"
    }

    It "Reads EncryptedFileNames.7z with Correct Password" {
        Get-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -Password "DasP@sswurd" -ErrorAction Stop |
        Select-Object -ExpandProperty FileName | Should Be "DDD.txt"
    }

    It "Fails to read EncryptedFileNames.7z with Incorrect Password" {
        { Get-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -Password "WrongPassword" } | Should Throw
    }

    It "Fails to read EncryptedFileNames.7z with No Password" {
        { Get-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z } | Should Throw
    }

}


Describe "How Get-7ZipInformation Shows Archive Details" {
    It "Reads AAA.7z Method" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\AAA.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should Be "LZMA"
    }

    It "Reads AAA-LZMA2.7z Method" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\AAA-LZMA2.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should Be "LZMA2"
    }

    It "Reads AAA.7z Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\AAA.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "SevenZip"
    }

    It "Reads AAA-LZMA2.7z Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\AAA-LZMA2.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "SevenZip"
    }

    <# ######## Should the next two be failing? ###################
    It "Reads BBB.zip Method" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\BBB.zip -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should Be "Deflate"
    }

    It "Reads BBB-64.zip Method" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\BBB-64.zip -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should Be "Deflate64"
    }
    ############################################################# #>

    It "Reads BBB.zip Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\BBB.zip -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "Zip"
    }

    It "Reads BBB-64.zip Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\BBB-64.zip -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "Zip"
    }

    It "Reads CCC-Bzip2.bz2 Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\CCC-BZip2.bz2 -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "BZip2"
    }

    It "Reads CCC.tar Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\CCC.tar -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "Tar"
    }

    It "Reads 111-Ultra.zip Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\111-Ultra.zip -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "Zip"
    }

    It "Reads 111-Fast.zip Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\111-Fast.zip -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "Zip"
    }

    It "Reads 111-Ultra.7z Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\111-Ultra.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "SevenZip"
    }

    It "Reads 111-Fast.7z Format" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\111-Fast.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Format | Should Be "SevenZip"
    }

    It "Reads 111-Ultra.7z Method" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\111-Ultra.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should Be "LZMA2"
    }

    It "Reads 111-Fast.7z Method" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\111-Fast.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should Be "LZMA2"
    }

    It "Reads ALL.zip FilesCount" {
        $x = Get-7ZipInformation -ArchiveFileName .\TestFiles\ALL.zip -ErrorAction Stop
        $x.FilesCount -gt 2 | Should Be $True
        $x = $null
    }

    It "Reads ALL-64.zip FilesCount" {
        $x = Get-7ZipInformation -ArchiveFileName .\TestFiles\ALL-64.zip -ErrorAction Stop
        $x.FilesCount -gt 2 | Should Be $True
        $x = $null
    }

    It "Reads PlainFileNames.7z with Correct Password" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\PlainFileNames.7z -Password "DasP@sswurd" -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should BeLike "*AES"
    }

    It "Reads PlainFileNames.7z with Incorrect Password" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\PlainFileNames.7z -Password "WrongPassword" -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should BeLike "*AES"
    }

    It "Reads PlainFileNames.7z with No Password" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\PlainFileNames.7z -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should BeLike "*AES"
    }

    It "Reads EncryptedFileNames.7z with Correct Password" {
        Get-7ZipInformation -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -Password "DasP@sswurd" -ErrorAction Stop |
        Select-Object -ExpandProperty Method | Should BeLike "*AES"
    }

    It "Fails to read EncryptedFileNames.7z with Incorrect Password" {
        { Get-7ZipInformation -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -Password "WrongPassword" } | Should Throw
    }

    It "Fails to read EncryptedFileNames.7z with No Password" {
        { Get-7ZipInformation -ArchiveFileName .\TestFiles\EncryptedFileNames.7z } | Should Throw
    }

}


Describe "How Expand-7Zip Decompresses Archives" {
    It "Expands AAA.7z" {
        del .\TestFiles\AAA.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\AAA.7z -TargetPath .\TestFiles
        Get-Item -Path .\TestFiles\AAA.txt -ErrorAction Stop | Should Exist
    }

    It "Expands AAA-LZMA2.7z" {
        del .\TestFiles\AAA.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\AAA-LZMA2.7z -TargetPath .\TestFiles
        Get-Item -Path .\TestFiles\AAA.txt -ErrorAction Stop | Should Exist
    }


    It "Expands BBB.zip" {
        del .\TestFiles\BBB.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\BBB.zip -TargetPath .\TestFiles
        Get-Item -Path .\TestFiles\BBB.txt -ErrorAction Stop | Should Exist
    }

    It "Expands BBB-64.zip" {
        del .\TestFiles\BBB.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\BBB-64.zip -TargetPath .\TestFiles
        Get-Item -Path .\TestFiles\BBB.txt -ErrorAction Stop | Should Exist
    }

    It "Expands ALL.zip" {
        del .\TestFiles\*.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\ALL.zip -TargetPath .\TestFiles
        (dir .\TestFiles\*.txt).count -gt 2 | Should Be $true
    }

    It "Expands ALL-64.zip" {
        del .\TestFiles\*.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\ALL-64.zip -TargetPath .\TestFiles
        (dir .\TestFiles\*.txt).count -gt 2 | Should Be $true
    }

    It "Expands PlainFileNames.7z with Correct Password" {
        del .\TestFiles\DDD.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -TargetPath .\TestFiles -Password "DasP@sswurd" -ErrorAction Stop
        Get-Item -Path .\TestFiles\DDD.txt -ErrorAction Stop | Should Exist 
        Get-Content -Path .\TestFiles\DDD.txt | Should BeLike "*DDDDDDDDDDDDDDDDDDDDDDDDDDDDDD*"
    }

    It "Expands EncryptedFileNames.7z with Correct Password" {
        del .\TestFiles\DDD.txt -ErrorAction SilentlyContinue
        Expand-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -TargetPath .\TestFiles -Password "DasP@sswurd" -ErrorAction Stop
        Get-Item -Path .\TestFiles\DDD.txt -ErrorAction Stop | Should Exist 
        Get-Content -Path .\TestFiles\DDD.txt | Should BeLike "*DDDDDDDDDDDDDDDDDDDDDDDDDDDDDD*"
    }

    It "Fails to expand PlainFileNames.7z with Incorrect Password" {
        del .\TestFiles\DDD.txt -ErrorAction SilentlyContinue
        ## Pester doesn't catch the following exception, not sure if this is module's or Pester's fault:
        #{ Expand-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -TargetPath .\TestFiles -Password "WrongPassword" -ErrorAction SilentlyContinue } | Should Throw 
        Expand-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -TargetPath .\TestFiles -Password "WrongPassword" -ErrorAction SilentlyContinue
        $pwd.path + "\TestFiles\DDD.txt" | Should Not Exist  
    }

    It "Fails to expand PlainFileNames.7z with No Password" {
        del .\TestFiles\DDD.txt -ErrorAction SilentlyContinue
        ## Pester doesn't catch the following exception, not sure if this is module's or Pester's fault:
        #{ Expand-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -TargetPath .\TestFiles -ErrorAction SilentlyContinue } | Should Throw 
        Expand-7Zip -ArchiveFileName .\TestFiles\PlainFileNames.7z -TargetPath .\TestFiles -ErrorAction SilentlyContinue
        $pwd.path + "\TestFiles\DDD.txt" | Should Not Exist  
    }

    It "Fails to expand EncryptedFileNames.7z with Incorrect Password" {
        del .\TestFiles\DDD.txt -ErrorAction SilentlyContinue
        ## Pester doesn't catch the following exception, not sure if this is module's or Pester's fault:
        #{ Expand-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -TargetPath .\TestFiles -Password "WrongPassword" -ErrorAction SilentlyContinue } | Should Throw 
        Expand-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -TargetPath .\TestFiles -Password "WrongPassword" -ErrorAction SilentlyContinue
        $pwd.path + "\TestFiles\DDD.txt" | Should Not Exist 
    }

    It "Fails to expand EncryptedFileNames.7z with No Password" {
        del .\TestFiles\DDD.txt -ErrorAction SilentlyContinue
        ## Pester doesn't catch the following exception, not sure if this is module's or Pester's fault:
        #{ Expand-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -TargetPath .\TestFiles } | Should Throw 
        Expand-7Zip -ArchiveFileName .\TestFiles\EncryptedFileNames.7z -TargetPath .\TestFiles -ErrorAction SilentlyContinue
        $pwd.path + "\TestFiles\DDD.txt" | Should Not Exist 
    }

}


Describe "How The Built-In Expand-Archive Interoperates" {
    # Zip format with Deflate (but not Deflate64) should be accessible to built-in Expand-Archive on PoSh 5.0+
    It "Allows Expand-Archive to read Zip+Deflate BBB.zip" {
        If ($PSVersionTable.PSVersion.Major -ge 5)
        {   
            del .\TestFiles\BBB.txt -ErrorAction SilentlyContinue
            Expand-Archive -Path .\TestFiles\BBB.zip -DestinationPath .\TestFiles -Force -ErrorAction Stop
            Get-Item .\TestFiles\BBB.txt | Should Exist
        } 
        Else 
        { $True | Should Be $True } 
    }

    It "Allows Expand-Archive to read Zip+Deflate+Ultra 111-Ultra.zip" {
        If ($PSVersionTable.PSVersion.Major -ge 5)
        {   
            del .\TestFiles\111.bin -ErrorAction SilentlyContinue
            Expand-Archive -Path .\TestFiles\111-Ultra.zip -DestinationPath .\TestFiles -Force -ErrorAction Stop
            Get-Item .\TestFiles\111.bin | Should Exist
        } 
        Else 
        { $True | Should Be $True } 
    }


}


#region CleanUp

del -Path .\TestFiles -Force -Recurse -ErrorAction SilentlyContinue

#endregion CleanUp



