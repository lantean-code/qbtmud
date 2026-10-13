#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$dockerRoot = Split-Path $PSScriptRoot -Parent
$buildRoot = [IO.Path]::GetFullPath((Join-Path $dockerRoot '.build'))
if ((Test-Path -LiteralPath $buildRoot) -and ((Get-Item -LiteralPath $buildRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'The fixture parent must not be a symbolic link or junction.' }
$fixture = Join-Path $buildRoot ('helper-regression-' + [guid]::NewGuid().ToString('N'))
if (-not $fixture.StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) { throw 'Unsafe fixture path.' }
try {
    $helperRoot = Join-Path $fixture 'docker'
    New-Item -ItemType Directory -Path (Join-Path $helperRoot '.build') -Force | Out-Null
    foreach ($name in @('build-local.ps1', 'base-image.json', 'ui-release.json')) {
        Copy-Item -LiteralPath (Join-Path $dockerRoot $name) -Destination $helperRoot
    }
    $payload = Join-Path $fixture 'payload'
    foreach ($name in @('public/_framework', 'public/i18n', 'public/js')) {
        New-Item -ItemType Directory -Path (Join-Path $payload $name) -Force | Out-Null
    }
    Set-Content -LiteralPath (Join-Path $payload 'public/index.html') -Value 'fixture'
    Set-Content -LiteralPath (Join-Path $payload 'public/appsettings.json') -Value '{}'
    Set-Content -LiteralPath (Join-Path $payload 'public/js/boot.settings.js') -Value "window.__assetVersion = '0.0.0-all';"
    $archive = Join-Path $fixture 'fixture.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($payload, $archive)
    $arguments = @{
        StageOnly = $true; Archive = $archive
        ArchiveSha256 = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
        UiVersion = '0.0.0'; UiCommit = ('1' * 40)
    }
    $helper = Join-Path $helperRoot 'build-local.ps1'
    & $helper @arguments
    $staged = Join-Path $helperRoot '.build/alternative-ui/public/index.html'
    if (-not (Test-Path -LiteralPath $staged)) { throw 'Hidden build directory staging failed.' }
    # Repeat with an existing staged tree, as local and CI retries do.
    & $helper @arguments
    if ((Get-Content -LiteralPath $staged -Raw).Trim() -ne 'fixture') { throw 'Repeat staging failed.' }
    Write-Host 'PASS: pre-existing hidden build directory and repeat staging'
} finally {
    if (Test-Path -LiteralPath $fixture) {
        if ([IO.Path]::GetFullPath($fixture).StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $fixture -Recurse -Force
        }
    }
}
