param(
    [string]$Version = "1.4.2",
    [string]$Runtime = "win-x64",
    [string]$CertificatePath,
    [switch]$Sign,
    [switch]$SignAllBinaries,
    [switch]$SkipInstaller,
    [string]$InnoSetupCompiler,
    [switch]$Help,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArguments
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ProjectPath = Join-Path $RepoRoot "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
$PublishRoot = Join-Path $RepoRoot "publish"
$PublishDir = Join-Path $PublishRoot "BedwarsOverlay-$Runtime"
$ZipPath = Join-Path $PublishRoot "BedwarsOverlay-$Version-$Runtime.zip"
$InstallerScript = Join-Path $RepoRoot "installer\BedwarsOverlay.iss"
$InstallerPath = Join-Path $PublishRoot "BedwarsOverlaySetup-$Version-$Runtime.exe"

function Show-Help {
    @"
Build BedwarsOverlay release artifacts.

Usage:
  .\scripts\publish-release.ps1 [options]
  .\scripts\publish-release.ps1 -Help
  .\scripts\publish-release.ps1 --help

Options:
  -Version <value>              Release version. Default: 1.4.2
  -Runtime <value>              .NET runtime identifier. Default: win-x64
  -CertificatePath <path>       PFX certificate path. Required with -Sign.
  -Sign                         Sign published app files and installer.
  -SignAllBinaries              Sign every .exe and .dll in publish output.
  -SkipInstaller                Skip Inno Setup installer build.
  -InnoSetupCompiler <path>     Explicit ISCC.exe path.
  -Help, --help, -h             Show this help.

Examples:
  .\scripts\publish-release.ps1 -Version "1.4.2"
  .\scripts\publish-release.ps1 -Version "1.4.2" -SkipInstaller
  .\scripts\publish-release.ps1 -Version "1.4.2" -Sign -CertificatePath ".\Bedwars-Overlay.pfx"
  .\scripts\publish-release.ps1 -Version "1.4.2" -Runtime "win-x64" -Sign -SignAllBinaries -CertificatePath ".\Bedwars-Overlay.pfx"

Notes:
  - With -Sign, certificate password is requested at script start.
  - Wrong certificate password is rejected immediately and asked again.
  - Release artifacts are written to .\publish.
"@
}

if ($Help -or ($RemainingArguments | Where-Object { $_ -in @("--help", "-h", "/?") }) -or ($MyInvocation.Line -match '(^|\s)--help(\s|$)')) {
    Show-Help
    exit 0
}

if ($RemainingArguments) {
    throw "Unknown argument(s): $($RemainingArguments -join ', '). Use -Help or --help."
}

function Find-SignTool {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path -LiteralPath $kitsRoot) {
        $matches = Get-ChildItem -LiteralPath $kitsRoot -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match "\\x64\\signtool\.exe$" } |
            Sort-Object FullName -Descending
        if ($matches) { return $matches[0].FullName }
    }

    throw "signtool.exe not found. Install Windows SDK or add signtool.exe to PATH."
}

function Copy-VisualCppRuntime {
    param([string]$DestinationDirectory)

    $visualStudioRoots = @()
    foreach ($programFilesRoot in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
        if (-not $programFilesRoot) { continue }
        $visualStudioRoot = Join-Path $programFilesRoot "Microsoft Visual Studio"
        if (Test-Path -LiteralPath $visualStudioRoot) {
            $installDirectories = Get-ChildItem -LiteralPath $visualStudioRoot -Directory |
                ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Directory -ErrorAction SilentlyContinue }
            $visualStudioRoots += $installDirectories |
                ForEach-Object { Join-Path $_.FullName "VC\Redist\MSVC" }
        }
    }

    $redistRoot = $visualStudioRoots |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { Get-ChildItem -LiteralPath $_ -Directory } |
        Where-Object { $_.Name -match '^\d+\.\d+' } |
        Sort-Object { [version]$_.Name } -Descending |
        Select-Object -First 1

    if (-not $redistRoot) {
        throw "Visual Studio x64 C++ redistributable files were not found. Install Visual Studio Build Tools with the C++ runtime redistributable component."
    }

    $crtDirectory = Join-Path $redistRoot.FullName "x64\Microsoft.VC143.CRT"
    $openMpDirectory = Join-Path $redistRoot.FullName "x64\Microsoft.VC143.OpenMP"
    if (-not (Test-Path -LiteralPath $crtDirectory)) {
        throw "Visual C++ CRT files were not found at: $crtDirectory"
    }

    Copy-Item -Path (Join-Path $crtDirectory "*.dll") -Destination $DestinationDirectory -Force
    $openMpRuntime = Join-Path $openMpDirectory "vcomp140.dll"
    if (Test-Path -LiteralPath $openMpRuntime) {
        Copy-Item -LiteralPath $openMpRuntime -Destination $DestinationDirectory -Force
    }
}

function Find-InnoSetupCompiler {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (Test-Path -LiteralPath $ExplicitPath) { return (Resolve-Path $ExplicitPath).Path }
        throw "Inno Setup compiler not found at: $ExplicitPath"
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $knownPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )

    foreach ($path in $knownPaths) {
        if (Test-Path -LiteralPath $path) { return $path }
    }

    throw "ISCC.exe not found. Install Inno Setup 6 or pass -InnoSetupCompiler."
}

function ConvertTo-PlainText {
    param([Security.SecureString]$SecureString)

    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
}

function Test-CertificatePassword {
    param(
        [string]$CertPath,
        [string]$Password
    )

    try {
        $certificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2(
            $CertPath,
            $Password,
            [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet
        )
        $certificate.Reset()
        return $true
    }
    catch {
        Write-Host "Certificate password rejected: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

function Read-CertificatePassword {
    param([string]$CertPath)

    if ($env:SIGNING_CERTIFICATE_PASSWORD) {
        $environmentPassword = $env:SIGNING_CERTIFICATE_PASSWORD
        Remove-Item Env:SIGNING_CERTIFICATE_PASSWORD
        if (Test-CertificatePassword -CertPath $CertPath -Password $environmentPassword) {
            return $environmentPassword
        }

        throw "The SIGNING_CERTIFICATE_PASSWORD environment variable is incorrect."
    }

    while ($true) {
        $securePassword = Read-Host "Certificate password" -AsSecureString
        $password = ConvertTo-PlainText $securePassword

        if (Test-CertificatePassword -CertPath $CertPath -Password $password) {
            return $password
        }

        if ($password) { $password = $null }
        "Certificate password is incorrect. Try again."
    }
}

function Sign-Files {
    param(
        [string[]]$FilePaths,
        [string]$CertPath,
        [string]$Password
    )

    if (-not $FilePaths -or $FilePaths.Count -eq 0) { return }

    $signTool = Find-SignTool
    & $signTool sign /f $CertPath /p $Password /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $FilePaths
    if ($LASTEXITCODE -ne 0) { throw "Signing failed." }
}

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "Project not found: $ProjectPath"
}

New-Item -ItemType Directory -Force -Path $PublishRoot | Out-Null

$signPassword = $null
if ($Sign) {
    if (-not $CertificatePath) { throw "Use -CertificatePath when -Sign is set." }
    if (-not (Test-Path -LiteralPath $CertificatePath)) { throw "Certificate not found: $CertificatePath" }

    $CertificatePath = (Resolve-Path -LiteralPath $CertificatePath).Path
    $signPassword = Read-CertificatePassword -CertPath $CertificatePath
}

if (Test-Path -LiteralPath $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

dotnet test (Join-Path $RepoRoot "BedwarsOverlay.slnx")
if ($LASTEXITCODE -ne 0) { throw "Tests failed." }

dotnet publish $ProjectPath -c Release -r $Runtime --self-contained true -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }

try {
    Copy-VisualCppRuntime -DestinationDirectory $PublishDir

    if ($Sign) {
        if ($SignAllBinaries) {
            $filesToSign = Get-ChildItem -LiteralPath $PublishDir -Recurse -File |
                Where-Object { $_.Extension -in @(".exe", ".dll") } |
                ForEach-Object { $_.FullName }
        }
        else {
            $filesToSign = @(
                Join-Path $PublishDir "BedwarsOverlay.App.exe"
                Join-Path $PublishDir "BedwarsOverlay.App.dll"
                Join-Path $PublishDir "BedwarsOverlay.Core.dll"
            ) | Where-Object { Test-Path -LiteralPath $_ }
        }

        Sign-Files -FilePaths $filesToSign -CertPath $CertificatePath -Password $signPassword
    }

    if (Test-Path -LiteralPath $ZipPath) {
        Remove-Item -LiteralPath $ZipPath -Force
    }
    Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipPath -Force

    if (-not $SkipInstaller) {
        $iscc = Find-InnoSetupCompiler -ExplicitPath $InnoSetupCompiler
        $env:BEDWARS_OVERLAY_VERSION = $Version
        & $iscc $InstallerScript
        if ($LASTEXITCODE -ne 0) { throw "Installer build failed." }

        if ($Sign) {
            Sign-Files -FilePaths @($InstallerPath) -CertPath $CertificatePath -Password $signPassword
        }
    }
}
finally {
    if ($signPassword) { $signPassword = $null }
}

"Release artifacts:"
"- $ZipPath"
if (-not $SkipInstaller) { "- $InstallerPath" }
