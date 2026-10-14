[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath
)

# Static package checks only: this does not install, launch, sign, or upload an app.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$sourceManifest = Get-Content (Join-Path $PSScriptRoot 'Package.appxmanifest') -Raw
$version = $sourceManifest.Package.Identity.Version
$expectedName = '57335Bitcookies.PDFBlender'
$expectedPublisher = 'CN=53636128-C1AF-456C-8B82-AC0D583B730C'

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Read-EntryBytes($Archive, [string]$Name) {
    $entry = $Archive.GetEntry($Name)
    Assert-Condition ($null -ne $entry) "Missing archive entry: $Name"
    $inputStream = $entry.Open()
    $outputStream = [IO.MemoryStream]::new()
    try {
        $inputStream.CopyTo($outputStream)
        return ,$outputStream.ToArray()
    } finally {
        $inputStream.Dispose()
        $outputStream.Dispose()
    }
}

function Read-EntryXml($Archive, [string]$Name) {
    return [xml][Text.Encoding]::UTF8.GetString((Read-EntryBytes $Archive $Name))
}

function Open-NestedZip($Archive, [string]$Name) {
    $stream = [IO.MemoryStream]::new((Read-EntryBytes $Archive $Name), $false)
    try { return [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Read) }
    catch { $stream.Dispose(); throw }
}

function Assert-Identity($Identity) {
    Assert-Condition ($Identity.Name -ceq $expectedName) 'Incorrect package identity name.'
    Assert-Condition ($Identity.Publisher -ceq $expectedPublisher) 'Incorrect package publisher.'
    Assert-Condition ($Identity.Version -ceq $version) 'Incorrect package version.'
}

function Assert-BlockMap($Archive) {
    [xml]$blockMap = Read-EntryXml $Archive 'AppxBlockMap.xml'
    Assert-Condition ($blockMap.BlockMap.HashMethod -ceq 'http://www.w3.org/2001/04/xmlenc#sha256') 'Block map must use SHA-256.'
    foreach ($file in $blockMap.BlockMap.File) {
        $name = $file.Name.Replace('\', '/')
        $entry = $Archive.GetEntry($name)
        Assert-Condition ($null -ne $entry) "Block map references missing file: $name"
        Assert-Condition ($entry.Length -eq [long]$file.Size) "Incorrect block map size: $name"
        $stream = $entry.Open()
        $hasher = [Security.Cryptography.SHA256]::Create()
        try {
            $buffer = [byte[]]::new(65536)
            foreach ($block in $file.Block) {
                $count = 0
                do {
                    $read = $stream.Read($buffer, $count, $buffer.Length - $count)
                    $count += $read
                } while ($read -gt 0 -and $count -lt $buffer.Length)
                $hash = $hasher.ComputeHash($buffer, 0, $count)
                Assert-Condition ([Convert]::ToBase64String($hash) -ceq $block.Hash) "Incorrect block hash: $name"
            }
            Assert-Condition ($stream.ReadByte() -eq -1) "Unmapped trailing data: $name"
        } finally { $hasher.Dispose(); $stream.Dispose() }
    }
}

function Assert-FileMatches($Archive, [string]$EntryName, [string]$SourcePath) {
    $packageHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData((Read-EntryBytes $Archive $EntryName)))
    Assert-Condition ($packageHash -ceq (Get-FileHash -LiteralPath $SourcePath -Algorithm SHA256).Hash) "Stale or changed packaged file: $EntryName"
}

function Assert-NativeMachine($Archive, [string]$Name, [int]$Machine) {
    $data = Read-EntryBytes $Archive $Name
    Assert-Condition ($data.Length -gt 64 -and [BitConverter]::ToUInt16($data, 0) -eq 0x5A4D) "Invalid PE file: $Name"
    $offset = [BitConverter]::ToInt32($data, 60)
    Assert-Condition ($offset -ge 0 -and $offset + 6 -le $data.Length) "Invalid PE header offset: $Name"
    Assert-Condition ([BitConverter]::ToUInt32($data, $offset) -eq 0x4550) "Invalid PE signature: $Name"
    Assert-Condition ([BitConverter]::ToUInt16($data, $offset + 4) -eq $Machine) "Wrong PE architecture: $Name"
}

function Assert-SymbolMatches($App, $Symbols, [string]$AssemblyName) {
    $pdbEntries = @($Symbols.Entries | Where-Object Name -CEQ "$AssemblyName.pdb")
    Assert-Condition ($pdbEntries.Count -eq 1) "Expected one matching symbol file for $AssemblyName."
    $peStream = [IO.MemoryStream]::new((Read-EntryBytes $App "PDFBlender.App/$AssemblyName.dll"), $false)
    $pdbStream = [IO.MemoryStream]::new((Read-EntryBytes $Symbols $pdbEntries[0].FullName), $false)
    $pe = $null
    $pdb = $null
    try {
        $pe = [Reflection.PortableExecutable.PEReader]::new($peStream)
        $pdb = [Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream)
        $pdbId = $pdb.GetMetadataReader().DebugMetadataHeader.Id
        $pdbGuid = [Guid]::new([byte[]]$pdbId[0..15])
        $pdbStamp = [BitConverter]::ToUInt32([byte[]]$pdbId[16..19], 0)
        $matched = $false
        foreach ($debugEntry in $pe.ReadDebugDirectory()) {
            if ($debugEntry.Type -eq [Reflection.PortableExecutable.DebugDirectoryEntryType]::CodeView) {
                $codeView = $pe.ReadCodeViewDebugDirectoryData($debugEntry)
                if ($codeView.Guid -eq $pdbGuid -and $debugEntry.Stamp -eq $pdbStamp) { $matched = $true }
            }
        }
        Assert-Condition $matched "Symbols do not match packaged $AssemblyName.dll."
    } finally {
        if ($pdb) { $pdb.Dispose() }
        if ($pe) { $pe.Dispose() }
        $pdbStream.Dispose()
        $peStream.Dispose()
    }
}

$upload = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
$bundle = $null
try {
    $bundleName = "PDFBlender.Package_${version}_x86_x64_ARM64.msixbundle"
    $expectedEntries = @($bundleName)
    foreach ($architecture in @('x86', 'x64', 'ARM64')) {
        $expectedEntries += "PDFBlender.Package_${version}_$architecture.appxsym"
    }
    Assert-Condition ($upload.Entries.Count -eq $expectedEntries.Count) 'Unexpected upload archive contents.'
    foreach ($name in $expectedEntries) { Assert-Condition ($null -ne $upload.GetEntry($name)) "Missing upload file: $name" }
    $bundle = Open-NestedZip $upload $bundleName
    Assert-Condition ($null -eq $bundle.GetEntry('AppxSignature.p7x')) 'Store bundle must remain unsigned.'
    [xml]$bundleManifest = Read-EntryXml $bundle 'AppxMetadata/AppxBundleManifest.xml'
    Assert-Identity $bundleManifest.Bundle.Identity
    Assert-BlockMap $bundle
    $applicationPackages = @($bundleManifest.Bundle.Packages.Package | Where-Object Type -EQ 'application')
    Assert-Condition ($applicationPackages.Count -eq 3) 'Expected exactly three application architectures.'
    $languages = @($bundleManifest.SelectNodes('//*[local-name()="Resource"]/@Language') | ForEach-Object Value)
    Assert-Condition ($languages -icontains 'en-us' -and $languages -icontains 'zh-cn') 'Bundle must support English and Chinese.'
    foreach ($architecture in @('x86', 'x64', 'ARM64')) {
        $packages = @($applicationPackages | Where-Object Architecture -IEQ $architecture)
        Assert-Condition ($packages.Count -eq 1) "Missing or duplicate architecture: $architecture"
        $app = Open-NestedZip $bundle $packages[0].FileName
        $symbols = $null
        try {
            [xml]$manifest = Read-EntryXml $app 'AppxManifest.xml'
            Assert-Identity $manifest.Package.Identity
            Assert-Condition ($manifest.Package.Identity.ProcessorArchitecture -ieq $architecture) 'Application architecture disagrees with bundle.'
            Assert-Condition ($manifest.Package.Properties.PublisherDisplayName -ceq 'Bitcookies') 'Incorrect publisher display name.'
            $capabilities = @($manifest.Package.Capabilities.ChildNodes | Where-Object NodeType -EQ 'Element')
            Assert-Condition ($capabilities.Count -eq 1 -and $capabilities[0].Name -ceq 'runFullTrust') 'Unexpected capabilities.'
            $application = $manifest.Package.Applications.Application
            Assert-Condition ($application.EntryPoint -ceq 'Windows.FullTrustApplication') 'Incorrect desktop entry point.'
            Assert-Condition ($application.VisualElements.BackgroundColor -ceq 'transparent') 'Incorrect tile background.'
            Assert-Condition ($application.VisualElements.DefaultTile.ShortName -ceq 'PDF Blender') 'Missing tile name.'
            Assert-Condition (@($application.VisualElements.DefaultTile.ShowNameOnTiles.ShowOn).Count -eq 3) 'Missing tile name-display declarations.'
            Assert-Condition ($null -eq $app.GetEntry('AppxSignature.p7x')) 'Store application package must remain unsigned.'
            $forbidden = @($app.Entries | Where-Object {
                $_.FullName -match '(?i)\.(pdb|cer|pfx|pdf)$|(^|/)(\.git|PDF-Poisoning|参考图|tests|samples|TestResults)(/|$)'
            })
            Assert-Condition ($forbidden.Count -eq 0) 'Private symbols, certificates, PDF samples, or development files in application package.'
            Assert-BlockMap $app
            Assert-FileMatches $app 'PDFBlender.App/LICENSE' (Join-Path $root 'LICENSE')
            Assert-FileMatches $app 'PDFBlender.App/THIRD-PARTY-NOTICES.md' (Join-Path $root 'THIRD-PARTY-NOTICES.md')
            foreach ($license in Get-ChildItem (Join-Path $root 'licenses') -File) {
                Assert-FileMatches $app "PDFBlender.App/licenses/$($license.Name)" $license.FullName
            }
            foreach ($asset in Get-ChildItem (Join-Path $PSScriptRoot 'Assets') -Filter '*scale-200.png' -File) {
                Assert-FileMatches $app "Assets/$($asset.Name)" $asset.FullName
            }
            $machine = switch ($architecture) { 'x86' { 0x14C } 'x64' { 0x8664 } 'ARM64' { 0xAA64 } }
            foreach ($nativeFile in @('PDFBlender.App.exe', 'pdfium.dll', 'libSkiaSharp.dll', 'coreclr.dll')) {
                Assert-NativeMachine $app "PDFBlender.App/$nativeFile" $machine
            }
            $runtime = [Text.Encoding]::UTF8.GetString((Read-EntryBytes $app 'PDFBlender.App/PDFBlender.App.runtimeconfig.json')) | ConvertFrom-Json
            Assert-Condition ($runtime.runtimeOptions.includedFrameworks.Count -ge 2) 'Expected self-contained .NET/WPF runtime.'
            foreach ($framework in $runtime.runtimeOptions.includedFrameworks) {
                Assert-Condition ($framework.version -ceq '10.0.12') 'Runtime version changed; review and refresh the bundled runtime license texts.'
            }
            $symbols = Open-NestedZip $upload "PDFBlender.Package_${version}_$architecture.appxsym"
            foreach ($assembly in @('PDFBlender.App', 'PDFBlender.Core', 'PDFBlender.Pdf')) {
                Assert-SymbolMatches $app $symbols $assembly
            }
            Write-Host "$architecture : identity, licenses, assets, SHA-256 blocks, native PE architecture and matching symbols verified."
        } finally {
            if ($symbols) { $symbols.Dispose() }
            $app.Dispose()
        }
    }
    foreach ($resourcePackage in @($bundleManifest.Bundle.Packages.Package | Where-Object Type -EQ 'resource')) {
        $resource = Open-NestedZip $bundle $resourcePackage.FileName
        try {
            Assert-Identity (Read-EntryXml $resource 'AppxManifest.xml').Package.Identity
            Assert-Condition ($null -eq $resource.GetEntry('AppxSignature.p7x')) 'Store resource package must remain unsigned.'
            Assert-BlockMap $resource
        } finally { $resource.Dispose() }
    }
} finally {
    if ($bundle) { $bundle.Dispose() }
    $upload.Dispose()
}

Write-Host 'Static Store package verification passed. Installation, UI smoke testing and WACK are separate checks.' -ForegroundColor Green
Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256