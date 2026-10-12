[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$magick = (Get-Command magick -ErrorAction Stop).Source
$source = Join-Path $PSScriptRoot '../assets/logo.png'
$output = Join-Path $PSScriptRoot 'Assets'
New-Item -ItemType Directory -Path $output -Force | Out-Null

function Write-Logo([string]$Name, [int]$Width, [int]$Height, [int]$ArtworkWidth) {
    # Trim the transparent source canvas before fitting the actual artwork.
    & $magick $source '-trim' '+repage' '-resize' "${ArtworkWidth}x${ArtworkWidth}" '-gravity' 'center' '-background' 'none' '-extent' "${Width}x${Height}" (Join-Path $output $Name)
    if ($LASTEXITCODE -ne 0) { throw "Failed to generate $Name." }
}

# Tile artwork is deliberately smaller than the canvas. At 100% scale, a medium
# tile uses a 48 px icon, leaving room for the Windows-rendered application name.
$logos = @(
    @{ Name = 'Square44x44Logo'; Width = 44; Height = 44; Artwork = 36 },
    @{ Name = 'Square150x150Logo'; Width = 150; Height = 150; Artwork = 48 },
    @{ Name = 'Square310x310Logo'; Width = 310; Height = 310; Artwork = 96 },
    @{ Name = 'Wide310x150Logo'; Width = 310; Height = 150; Artwork = 48 },
    @{ Name = 'StoreLogo'; Width = 50; Height = 50; Artwork = 50 }
)
foreach ($logo in $logos) {
    Write-Logo "$($logo.Name).png" $logo.Width $logo.Height $logo.Artwork
    foreach ($scale in @(125, 150, 200, 400)) {
        Write-Logo "$($logo.Name).scale-$scale.png" ([int][Math]::Ceiling($logo.Width * $scale / 100)) ([int][Math]::Ceiling($logo.Height * $scale / 100)) ([int][Math]::Ceiling($logo.Artwork * $scale / 100))
    }
}

# Target-size assets are used by the shell's app list/taskbar. The unplated
# alternate form explicitly requests a transparent icon without a tile backdrop.
foreach ($size in @(16, 24, 32, 48, 256)) {
    $artwork = [Math]::Max(1, [int]($size * 0.9))
    Write-Logo "Square44x44Logo.targetsize-$size.png" $size $size $artwork
    Write-Logo "Square44x44Logo.targetsize-$size`_altform-unplated.png" $size $size $artwork
}
Write-Host 'MSIX logo assets regenerated from assets/logo.png.'
