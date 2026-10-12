[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [int]$Mode = -1,

    [switch]$Force,

    [ValidateSet('x86', 'x64', 'ARM64')]
    [string]$Architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant(),

    [string]$CertificateThumbprint
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$Architecture = if ($Architecture -ieq 'ARM64') { 'ARM64' } else { $Architecture.ToLowerInvariant() }

$root = $PSScriptRoot
$appProject = Join-Path $root 'src/PDFBlender.App/PDFBlender.App.csproj'
$publishRoot = Join-Path $root 'publish'
$portableOutput = Join-Path $publishRoot 'portable'
$installerOutput = Join-Path $publishRoot 'installer'
$localTestOutput = Join-Path $publishRoot 'local-test'
$packagingProject = Join-Path $root 'packaging/PDFBlender.Package.wapproj'

function Show-Menu {
    Write-Host ''
    Write-Host 'PDF Blender build menu'
    Write-Host '  1  Portable publish (publish/portable)'
    Write-Host '  2  MSIX installer (publish/installer)'
    Write-Host '  3  Signed local-test MSIX (publish/local-test)'
    Write-Host '  0  Exit'
    Write-Host ''
}

function Confirm-ReplaceOutput([string]$Path) {
    # Restrict recursive cleanup to a child of this repository's publish directory.
    $resolvedOutput = [IO.Path]::GetFullPath($Path)
    $publishPrefix = [IO.Path]::GetFullPath($publishRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedOutput.StartsWith($publishPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove output outside $publishRoot."
    }
    if (-not (Test-Path -LiteralPath $Path)) {
        return $true
    }

    if ($Force) {
        Remove-Item -LiteralPath $Path -Recurse -Force
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
        Write-Host 'Invalid action. Choose 1, 2, 3, or 0.' -ForegroundColor Red
        exit 2
    }
}

if ($Mode -notin 0, 1, 2, 3) {
    Write-Host 'Invalid action. Choose 1, 2, 3, or 0.' -ForegroundColor Red
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
    { $_ -in 2, 3 } {
        if (-not (Test-Path -LiteralPath $packagingProject)) {
            Write-Host 'MSIX packaging project was not found at packaging/PDFBlender.Package.wapproj.' -ForegroundColor Yellow
            exit 40
        }
        $msbuild = Find-MSBuild
        if (-not $msbuild) {
            Write-Host 'MSIX packaging requires Visual Studio MSBuild with the Windows Application Packaging workload.' -ForegroundColor Yellow
            exit 40
        }

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
        $sdkRegistryPaths = @(
            'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots',
            'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows Kits\Installed Roots'
        )
        $windowsKitCandidates = @(
            $env:WindowsSdkDir
            foreach ($registryPath in $sdkRegistryPaths) {
                (Get-ItemProperty -LiteralPath $registryPath -ErrorAction SilentlyContinue).KitsRoot10
            }
            (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10')
            (Join-Path $env:ProgramFiles 'Windows Kits\10')
        ) | Where-Object { $_ } | Select-Object -Unique
        $windowsKitRoot = $windowsKitCandidates |
            Where-Object { Test-Path (Join-Path $_ "DesignTime\CommonConfiguration\Neutral\UAP\$targetPlatformVersion\UAP.props") } |
            Select-Object -First 1
        if (-not $windowsKitRoot) {
            Write-Host "MSIX packaging requires the Windows SDK UAP platform $targetPlatformVersion (UAP.props). Install the matching Windows 10/11 SDK, then retry." -ForegroundColor Yellow
            exit 40
        }

        if ($Mode -eq 3) {
            [xml]$manifest = Get-Content -LiteralPath (Join-Path $root 'packaging/Package.appxmanifest') -Raw
            $publisher = $manifest.Package.Identity.Publisher
            $now = Get-Date
            $certificates = @(Get-ChildItem Cert:\CurrentUser\My | Where-Object {
                $_.Subject -eq $publisher -and $_.HasPrivateKey -and
                $_.NotBefore -le $now -and $_.NotAfter -gt $now -and
                ($_.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3')
            })
            if ($CertificateThumbprint) {
                $thumbprint = $CertificateThumbprint -replace '\s', ''
                $certificates = @($certificates | Where-Object Thumbprint -eq $thumbprint)
            }
            $certificate = $certificates | Sort-Object NotAfter -Descending | Select-Object -First 1
            if (-not $certificate) {
                throw "No valid code-signing certificate with a private key matching $publisher was found in Cert:\CurrentUser\My. Create a local test certificate first, or pass -CertificateThumbprint."
            }
            $signToolArchitecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
            $signTool = Join-Path $windowsKitRoot "bin/$targetPlatformVersion/$signToolArchitecture/signtool.exe"
            if (-not (Test-Path -LiteralPath $signTool)) {
                throw "SignTool was not found at $signTool. Install the signing tools for the configured Windows SDK."
            }
            if (-not (Confirm-ReplaceOutput $localTestOutput)) { exit 3 }
            $unsignedOutput = Join-Path $localTestOutput 'unsigned'
            New-Item -ItemType Directory -Path $unsignedOutput -Force | Out-Null
            # Build only the requested architecture, without touching Store upload outputs.
            & $msbuild $packagingProject '/restore' '/verbosity:minimal' '/p:GenerateAppxPackageOnBuild=true' '/p:Configuration=Release' "/p:Platform=$Architecture" '/p:AppxBundle=Never' "/p:AppxBundlePlatforms=$Architecture" '/p:UapAppxPackageBuildMode=SideloadOnly' '/p:AppxPackageIsForStore=false' '/p:BuildAppxUploadPackageForUap=false' '/p:BuildAppxSideloadPackageForUap=true' '/p:AppxPackageSigningEnabled=false' "/p:AppxPackageDir=$unsignedOutput\" "/p:VSToolsPath=$vsToolsPath"
            if ($LASTEXITCODE -ne 0) { exit 40 }
            $packageName = "PDFBlender.Package_$($manifest.Package.Identity.Version)_$Architecture.msix"
            $packages = @(Get-ChildItem -LiteralPath $unsignedOutput -Recurse -File -Filter $packageName)
            if ($packages.Count -ne 1) {
                throw "Expected one fresh $packageName in $unsignedOutput; found $($packages.Count)."
            }
            $testPackage = Join-Path $localTestOutput ($packageName -replace '\.msix$', '_LocalTest.msix')
            Copy-Item -LiteralPath $packages[0].FullName -Destination $testPackage
            & $signTool sign /fd SHA256 /sha1 $certificate.Thumbprint $testPackage
            if ($LASTEXITCODE -ne 0) { throw 'Local MSIX signing failed.' }
            & $signTool verify /pa $testPackage
            if ($LASTEXITCODE -ne 0) {
                throw 'Signature verification failed. For a self-signed test certificate, import its public .cer into LocalMachine\TrustedPeople, then retry. The signed file remains available for diagnosis.'
            }
            Write-Host "Signed local test package written to $testPackage" -ForegroundColor Green
            Write-Host "Certificate: $($certificate.Thumbprint). Local testing only; upload the mode 2 .msixupload to Microsoft Store."
            break
        }

        if (-not (Confirm-ReplaceOutput $installerOutput)) { exit 3 }
        # Keep each platform's intermediate output separate; a shared OutDir can reuse the wrong AppxManifest.xml.
        & $msbuild $packagingProject '/restore' '/verbosity:minimal' '/p:GenerateAppxPackageOnBuild=true' '/p:Configuration=Release' '/p:UapAppxPackageBuildMode=StoreAndSideload' '/p:AppxPackageIsForStore=true' '/p:BuildAppxUploadPackageForUap=true' '/p:BuildAppxSideloadPackageForUap=true' "/p:VSToolsPath=$vsToolsPath"
        if ($LASTEXITCODE -ne 0) { exit 40 }

        # Keep the three architecture packages together for manual installation and Store inspection.
        # The WAP project owns its per-platform intermediate directories; do not redirect them with OutDir.
        $packageOutput = Join-Path $installerOutput 'PDFBlender.Package'
        New-Item -ItemType Directory -Path $packageOutput -Force | Out-Null
        foreach ($platform in @('x86', 'x64', 'ARM64')) {
            $platformPackageDirectory = Join-Path $root "packaging/bin/$platform/Release"
            $platformPackages = Get-ChildItem -LiteralPath $platformPackageDirectory -Filter '*.msix' -File -ErrorAction SilentlyContinue
            if (-not $platformPackages) {
                Write-Host "MSIX package for $platform was not found in $platformPackageDirectory." -ForegroundColor Yellow
                exit 40
            }
            Copy-Item -LiteralPath $platformPackages.FullName -Destination $packageOutput -Force
        }
        Write-Host "MSIX installer written to $installerOutput" -ForegroundColor Green
    }
}

exit 0
