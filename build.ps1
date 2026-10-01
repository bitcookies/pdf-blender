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
            Write-Host 'MSIX packaging is not configured yet. Add packaging/PDFBlender.Package.wapproj with the Store identity from SPEC.md.' -ForegroundColor Yellow
            exit 40
        }
        if (-not (Confirm-ReplaceOutput $installerOutput)) { exit 3 }
        New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
        Invoke-Dotnet @('msbuild', $packagingProject, '/p:Configuration=Release', "/p:OutDir=$installerOutput\") 40
        Write-Host "MSIX installer written to $installerOutput" -ForegroundColor Green
    }
}

exit 0
