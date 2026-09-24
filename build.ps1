[CmdletBinding()]
param(
    [ValidateSet('build', 'test', 'portable', 'msix')]
    [string]$Action = 'build'
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$solution = Join-Path $PSScriptRoot 'PDFBlender.slnx'
$appProject = Join-Path $PSScriptRoot 'src/PDFBlender.App/PDFBlender.App.csproj'

switch ($Action) {
    'build' {
        dotnet build $solution --configuration Release
        if ($LASTEXITCODE -ne 0) { exit 10 }
        exit 0
    }
    'test' {
        dotnet test $solution --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { exit 20 }
        exit 0
    }
    'portable' {
        $output = Join-Path $PSScriptRoot 'artifacts/portable'
        dotnet publish $appProject --configuration Release --runtime win-x64 --self-contained false --output $output
        if ($LASTEXITCODE -ne 0) { exit 30 }
        exit 0
    }
    'msix' {
        Write-Error 'MSIX packaging is not configured yet. Add the Store identity from SPEC.md before packaging.'
        exit 40
    }
}
