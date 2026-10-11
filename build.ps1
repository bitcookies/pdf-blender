[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [int]$Mode = -1,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$root = $PSScriptRoot
$appProject = Join-Path $root 'src/PDFBlender.App/PDFBlender.App.csproj'
$publishRoot = Join-Path $root 'publish'
$portableOutput = Join-Path $publishRoot 'portable'
$installerOutput = Join-Path $publishRoot 'installer'
$packagingProject = Join-Path $root 'packaging/PDFBlender.Package.wapproj'

function Show-Menu {
    Write-Host ''
    Write-Host 'PDF Blender build menu'
    Write-Host '  1  Portable publish (publish/portable)'
    Write-Host '  2  MSIX installer (publish/installer)'
    Write-Host '  0  Exit'
    Write-Host ''
}

function Confirm-ReplaceOutput([string]$Path) {
    if ($Force -or -not (Test-Path -LiteralPath $Path)) {
        return $true
    }

    $hasFiles = Get-ChildItem -LiteralPath $Path -Force -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $hasFiles) {
        return $true
    }

    $answer = Read-Host "`n$Path already contains files. Delete and recreate it? [y/N]"
    if ($answer -notmatch '^(?i:y|yes)$') {
        Write-Host 'Cancelled. Existing files were not changed.' -ForegroundColor Yellow
        return $false
    }

    Remove-Item -LiteralPath $Path -Recurse -Force
    return $true
}

function Find-MSBuild {
    $direct = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($null -ne $direct) {
        return $direct.Source
    }

    $programFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
    $vswhere = if ($programFilesX86) { Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe' } else { $null }
    if ($vswhere -and (Test-Path -LiteralPath $vswhere)) {
        $installationPath = (& $vswhere -latest -products * -property installationPath | Select-Object -First 1).Trim()
        if ($installationPath) {
            $candidate = Join-Path $installationPath 'MSBuild\Current\Bin\MSBuild.exe'
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    return $null
}
function Invoke-Dotnet([string[]]$Arguments, [int]$FailureCode) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $FailureCode
    }
}

if ($Mode -eq -1) {
    Show-Menu
    $selection = Read-Host 'Choose an action number'
    if (-not [int]::TryParse($selection, [ref]$Mode)) {
        Write-Host 'Invalid action. Choose 1, 2, or 0.' -ForegroundColor Red
        exit 2
    }
}

if ($Mode -notin 0, 1, 2) {
    Write-Host 'Invalid action. Choose 1, 2, or 0.' -ForegroundColor Red
    exit 2
}

if ($Mode -eq 0) {
    exit 0
}

switch ($Mode) {
    1 {
        if (-not (Confirm-ReplaceOutput $portableOutput)) { exit 3 }
        New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
        Invoke-Dotnet @('publish', $appProject, '--configuration', 'Release', '--runtime', 'win-x64', '--self-contained', 'false', '--output', $portableOutput) 30
        Write-Host "Portable build written to $portableOutput" -ForegroundColor Green
    }
    2 {
        if (-not (Test-Path -LiteralPath $packagingProject)) {
            Write-Host 'MSIX packaging project was not found at packaging/PDFBlender.Package.wapproj.' -ForegroundColor Yellow
            exit 40
        }
        $msbuild = Find-MSBuild
        if (-not $msbuild) {
            Write-Host 'MSIX packaging requires Visual Studio MSBuild with the Windows Application Packaging workload.' -ForegroundColor Yellow
            exit 40
        }
        if (-not (Confirm-ReplaceOutput $installerOutput)) { exit 3 }
        New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
        $vsRoot = Split-Path (Split-Path (Split-Path (Split-Path $msbuild -Parent) -Parent) -Parent) -Parent
        $vsToolsRoot = Join-Path $vsRoot 'MSBuild\Microsoft\VisualStudio'
        $vsToolsVersion = Get-ChildItem -LiteralPath $vsToolsRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { Test-Path (Join-Path $_.FullName 'AppxPackage\Microsoft.AppxPackage.Targets') } |
            Sort-Object Name -Descending |
            Select-Object -First 1
        $vsToolsPath = if ($vsToolsVersion) { $vsToolsVersion.FullName } else { $null }
        $appxTargets = if ($vsToolsPath) { Join-Path $vsToolsPath 'AppxPackage\Microsoft.AppxPackage.Targets' } else { $null }
        if (-not (Test-Path -LiteralPath $appxTargets)) {
            Write-Host 'MSIX packaging requires the Visual Studio Windows Application Packaging workload.' -ForegroundColor Yellow
            exit 40
        }

        $targetPlatformVersion = ((Get-Content -LiteralPath $packagingProject | Select-String -Pattern '<TargetPlatformVersion>([^<]+)</TargetPlatformVersion>').Matches.Groups[1].Value)
        $windowsKitCandidates = @(
            (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10'),
            (Join-Path $env:ProgramFiles 'Windows Kits\10')
        ) | Where-Object { $_ }
        $windowsKitRoot = $windowsKitCandidates |
            Where-Object { Test-Path (Join-Path $_ "Platforms\UAP\$targetPlatformVersion\UAP.props") } |
            Select-Object -First 1
        if (-not $windowsKitRoot) {
            Write-Host "MSIX packaging requires the Windows SDK UAP platform $targetPlatformVersion (UAP.props). Install the matching Windows 10/11 SDK, then retry." -ForegroundColor Yellow
            exit 40
        }

        & $msbuild $packagingProject '/p:Configuration=Release' "/p:VSToolsPath=$vsToolsPath" "/p:OutDir=$installerOutput\"
        if ($LASTEXITCODE -ne 0) { exit 40 }
        Write-Host "MSIX installer written to $installerOutput" -ForegroundColor Green
    }
}

exit 0
