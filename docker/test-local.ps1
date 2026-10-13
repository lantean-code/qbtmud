#requires -Version 7.0
[CmdletBinding()]
param([string] $Tag = 'qbtmud:local')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'find-docker.ps1')
& docker image inspect $Tag | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Image $Tag is unavailable. Run ./docker/build-local.ps1 first." }
$python = $null
foreach ($candidate in @('python3', 'python')) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) {
        $version = & $candidate --version 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0 -and $version -match '^Python 3\.') { $python = $candidate; break }
    }
}
if (-not $python) { throw 'Python 3 is required for automated smoke tests. Install Python 3 and retry.' }
& $python (Join-Path $PSScriptRoot 'tests/smoke-test.py') $Tag
if ($LASTEXITCODE -ne 0) { throw 'Container smoke tests failed.' }
Write-Host "All container smoke tests passed for $Tag."
