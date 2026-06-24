param(
    [string]$Version = "1.1.0",
    [string]$Runtime = "win-x64",
    [string]$CertificatePath,
    [switch]$Sign,
    [switch]$SignAllBinaries,
    [switch]$SkipInstaller,
    [string]$InnoSetupCompiler
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ProjectPath = Join-Path $RepoRoot "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
$PublishRoot = Join-Path $RepoRoot "publish"
$PublishDir = Join-Path $PublishRoot "BedwarsOverlay-$Runtime"
$ZipPath = Join-Path $PublishRoot "BedwarsOverlay-$Version-$Runtime.zip"
$InstallerScript = Join-Path $RepoRoot "installer\BedwarsOverlay.iss"
$InstallerPath = Join-Path $PublishRoot "BedwarsOverlaySetup-$Version-$Runtime.exe"

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

if (Test-Path -LiteralPath $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

dotnet test (Join-Path $RepoRoot "BedwarsOverlay.slnx")
if ($LASTEXITCODE -ne 0) { throw "Tests failed." }

dotnet publish $ProjectPath -c Release -r $Runtime --self-contained true -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }

$signPassword = $null
if ($Sign) {
    if (-not $CertificatePath) { throw "Use -CertificatePath when -Sign is set." }
    if (-not (Test-Path -LiteralPath $CertificatePath)) { throw "Certificate not found: $CertificatePath" }

    $securePassword = Read-Host "Certificate password" -AsSecureString
    $signPassword = ConvertTo-PlainText $securePassword

    try {
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
    finally {
        if ($signPassword) { $signPassword = $null }
    }
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
        $securePassword = Read-Host "Certificate password for installer" -AsSecureString
        $installerPassword = ConvertTo-PlainText $securePassword
        try {
            Sign-Files -FilePaths @($InstallerPath) -CertPath $CertificatePath -Password $installerPassword
        }
        finally {
            if ($installerPassword) { $installerPassword = $null }
        }
    }
}

"Release artifacts:"
"- $ZipPath"
if (-not $SkipInstaller) { "- $InstallerPath" }
