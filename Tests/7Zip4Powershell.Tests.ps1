# Pester 5 tests for 7Zip4Powershell module

BeforeAll {
    # Import the module from the build output
    $ModulePath = Join-Path $PSScriptRoot "..\Module\7Zip4Powershell\7Zip4PowerShell.dll"

    # Verify module exists before importing
    if (-not (Test-Path $ModulePath)) {
        throw "Module not found at: $ModulePath. Run build first."
    }

    # Import the module
    Import-Module $ModulePath -Force -ErrorAction Stop

    # Create temp directory for test outputs
    $script:TestOutputDir = Join-Path $env:TEMP "7Zip4PS_Tests_$(Get-Random)"
    New-Item -ItemType Directory -Path $script:TestOutputDir -Force | Out-Null

    # Create test data directory with sample files
    $script:TestDataDir = Join-Path $PSScriptRoot "TestData"

    # Helper function to create test files if they don't exist
    function Initialize-TestData {
        if (-not (Test-Path $script:TestDataDir)) {
            New-Item -ItemType Directory -Path $script:TestDataDir -Force | Out-Null
        }

        # Create sample.txt
        $sampleFile = Join-Path $script:TestDataDir "sample.txt"
        if (-not (Test-Path $sampleFile)) {
            "This is a test file for 7Zip4Powershell smoke tests.`nLine 2`nLine 3" |
                Set-Content $sampleFile -NoNewline
        }

        # Create nested directory and file
        $nestedDir = Join-Path $script:TestDataDir "nested"
        if (-not (Test-Path $nestedDir)) {
            New-Item -ItemType Directory -Path $nestedDir -Force | Out-Null
        }

        $nestedFile = Join-Path $nestedDir "nested-file.txt"
        if (-not (Test-Path $nestedFile)) {
            "Nested file content" | Set-Content $nestedFile -NoNewline
        }
    }

    Initialize-TestData
}

AfterAll {
    # Cleanup temp test outputs
    if (Test-Path $script:TestOutputDir) {
        Remove-Item -Path $script:TestOutputDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Describe "Compress-7Zip and Expand-7Zip" -Tags "Smoke" {

    Context "7z format" {

        It "Should compress a single file to .7z format" {
            $sourceFile = Join-Path $script:TestDataDir "sample.txt"
            $archivePath = Join-Path $script:TestOutputDir "test.7z"

            { Compress-7Zip -Path $sourceFile -ArchiveFileName $archivePath } |
                Should -Not -Throw

            Test-Path $archivePath | Should -Be $true
        }

        It "Should extract a .7z archive" {
            $archivePath = Join-Path $script:TestOutputDir "test.7z"
            $extractPath = Join-Path $script:TestOutputDir "extracted_7z"

            { Expand-7Zip -ArchiveFileName $archivePath -TargetPath $extractPath } |
                Should -Not -Throw

            $extractedFile = Join-Path $extractPath "sample.txt"
            Test-Path $extractedFile | Should -Be $true

            # Verify content matches
            $originalContent = Get-Content (Join-Path $script:TestDataDir "sample.txt") -Raw
            $extractedContent = Get-Content $extractedFile -Raw
            $extractedContent | Should -Be $originalContent
        }
    }

    Context "zip format" {

        It "Should compress a directory to .zip format" {
            $sourceDir = $script:TestDataDir
            $archivePath = Join-Path $script:TestOutputDir "test.zip"

            { Compress-7Zip -Path $sourceDir -ArchiveFileName $archivePath } |
                Should -Not -Throw

            Test-Path $archivePath | Should -Be $true
        }

        It "Should extract a .zip archive" {
            $archivePath = Join-Path $script:TestOutputDir "test.zip"
            $extractPath = Join-Path $script:TestOutputDir "extracted_zip"

            { Expand-7Zip -ArchiveFileName $archivePath -TargetPath $extractPath } |
                Should -Not -Throw

            # Verify both files exist (files are extracted directly without parent directory)
            Test-Path (Join-Path $extractPath "sample.txt") | Should -Be $true
            Test-Path (Join-Path $extractPath "nested\nested-file.txt") | Should -Be $true
        }
    }

    Context "tar.gz format (combined)" {

        It "Should compress to .tar format first" {
            $sourceFile = Join-Path $script:TestDataDir "sample.txt"
            $tarPath = Join-Path $script:TestOutputDir "test.tar"

            { Compress-7Zip -Path $sourceFile -ArchiveFileName $tarPath } |
                Should -Not -Throw

            Test-Path $tarPath | Should -Be $true
        }

        It "Should compress .tar to .tar.gz (gzip)" {
            $tarPath = Join-Path $script:TestOutputDir "test.tar"
            $gzPath = Join-Path $script:TestOutputDir "test.tar.gz"

            { Compress-7Zip -Path $tarPath -ArchiveFileName $gzPath } |
                Should -Not -Throw

            Test-Path $gzPath | Should -Be $true
        }

        It "Should extract .tar.gz archive (two-step)" {
            $gzPath = Join-Path $script:TestOutputDir "test.tar.gz"
            $extractPath1 = Join-Path $script:TestOutputDir "extracted_gz"

            # Step 1: Extract .gz to get .tar
            { Expand-7Zip -ArchiveFileName $gzPath -TargetPath $extractPath1 } |
                Should -Not -Throw

            $tarPath = Join-Path $extractPath1 "test.tar"
            Test-Path $tarPath | Should -Be $true

            # Step 2: Extract .tar to get original file
            $extractPath2 = Join-Path $script:TestOutputDir "extracted_tar"
            { Expand-7Zip -ArchiveFileName $tarPath -TargetPath $extractPath2 } |
                Should -Not -Throw

            $extractedFile = Join-Path $extractPath2 "sample.txt"
            Test-Path $extractedFile | Should -Be $true
        }
    }

    Context "Password-protected archives" {

        It "Should compress with password (7z)" {
            $sourceFile = Join-Path $script:TestDataDir "sample.txt"
            $archivePath = Join-Path $script:TestOutputDir "test-password.7z"
            $password = "TestPass123"

            { Compress-7Zip -Path $sourceFile -ArchiveFileName $archivePath -Password $password } |
                Should -Not -Throw

            Test-Path $archivePath | Should -Be $true
        }

        It "Should extract password-protected archive with correct password" {
            $archivePath = Join-Path $script:TestOutputDir "test-password.7z"
            $extractPath = Join-Path $script:TestOutputDir "extracted_password"
            $password = "TestPass123"

            { Expand-7Zip -ArchiveFileName $archivePath -TargetPath $extractPath -Password $password } |
                Should -Not -Throw

            $extractedFile = Join-Path $extractPath "sample.txt"
            Test-Path $extractedFile | Should -Be $true
        }

        It "Should fail to extract password-protected archive without password" {
            $archivePath = Join-Path $script:TestOutputDir "test-password.7z"
            $extractPath = Join-Path $script:TestOutputDir "extracted_no_password"

            # This should throw an error
            { Expand-7Zip -ArchiveFileName $archivePath -TargetPath $extractPath -ErrorAction Stop } |
                Should -Throw
        }

        It "Should compress with SecureString password (zip)" {
            $sourceFile = Join-Path $script:TestDataDir "sample.txt"
            $archivePath = Join-Path $script:TestOutputDir "test-secure-password.zip"
            $securePassword = ConvertTo-SecureString "SecurePass456" -AsPlainText -Force

            { Compress-7Zip -Path $sourceFile -ArchiveFileName $archivePath -SecurePassword $securePassword } |
                Should -Not -Throw

            Test-Path $archivePath | Should -Be $true
        }

        It "Should extract with SecureString password" {
            $archivePath = Join-Path $script:TestOutputDir "test-secure-password.zip"
            $extractPath = Join-Path $script:TestOutputDir "extracted_secure_password"
            $securePassword = ConvertTo-SecureString "SecurePass456" -AsPlainText -Force

            { Expand-7Zip -ArchiveFileName $archivePath -TargetPath $extractPath -SecurePassword $securePassword } |
                Should -Not -Throw

            $extractedFile = Join-Path $extractPath "sample.txt"
            Test-Path $extractedFile | Should -Be $true
        }
    }
}
