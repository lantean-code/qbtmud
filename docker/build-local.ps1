#requires -Version 7.0
[CmdletBinding()]
param(
    [string] $BaseImage,
    [string] $Archive,
    [string] $ArchiveSha256,
    [string] $UiVersion,
    [string] $UiCommit,
    [string] $UpstreamTag,
    [string] $Tag = 'qbtmud:local',
    [string] $Platform = 'linux/amd64',
    [string] $PackageRevision = 'r1',
    [string] $ReleaseId = '',
    [string] $AssetId = '',
    [switch] $StageOnly,
    [switch] $Run,
    [switch] $Test
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$base = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'base-image.json') -Raw | ConvertFrom-Json
$release = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ui-release.json') -Raw | ConvertFrom-Json
if ($StageOnly -and ($Run -or $Test)) { throw 'StageOnly cannot be combined with Run or Test.' }
if (-not $BaseImage) { $BaseImage = $base.image }
if (-not $UiVersion) { $UiVersion = $release.version }
if ($UiVersion -eq $release.version) {
    if (-not $ArchiveSha256) { $ArchiveSha256 = $release.sha256 }
    if (-not $UiCommit) { $UiCommit = $release.commit }
}
if ($BaseImage -notmatch '^lscr\.io/linuxserver/qbittorrent:[A-Za-z0-9._-]+@sha256:[a-f0-9]{64}$') {
    throw 'BaseImage must contain a LinuxServer version tag and pinned index digest.'
}
if ($ArchiveSha256 -notmatch '^[a-fA-F0-9]{64}$' -or $UiCommit -notmatch '^[a-f0-9]{40}$') {
    throw 'For an unpinned release, supply ArchiveSha256 and UiCommit, or update ui-release.json with verified release metadata.'
}
if (-not $UpstreamTag) { $UpstreamTag = ($BaseImage -split '@')[0].Split(':')[-1] }
if (-not $StageOnly) {
    . (Join-Path $PSScriptRoot 'find-docker.ps1')
    & docker info --format '{{.OSType}}' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Docker is not running. Start your Docker engine, then retry.' }
}
$buildRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '.build'))
if ((Test-Path -LiteralPath $buildRoot) -and ((Get-Item -LiteralPath $buildRoot).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw 'The generated build directory must not be a symbolic link or junction.'
}
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
if (-not $Archive) {
    if ($UiVersion -ne $release.version) { throw 'For an unpinned release, supply Archive or update ui-release.json.' }
    if ($release.archive -match '[\\/:]' -or $release.archive -in @('.', '..')) { throw 'The pinned archive name must be a file name, not a path.' }
    $Archive = Join-Path $buildRoot $release.archive
    if (-not (Test-Path -LiteralPath $Archive)) {
        Write-Host "Downloading international qbtmud $UiVersion..."
        $download = Join-Path $buildRoot ('download-' + [guid]::NewGuid().ToString('N') + '.zip')
        try {
            Invoke-WebRequest -Uri $release.url -OutFile $download
            if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $ArchiveSha256) {
                throw 'Downloaded release archive checksum does not match.'
            }
            Move-Item -LiteralPath $download -Destination $Archive
        } finally {
            if (Test-Path -LiteralPath $download) { Remove-Item -LiteralPath $download -Force }
        }
    }
}
$archivePath = (Resolve-Path -LiteralPath $Archive).Path
if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $ArchiveSha256) {
    throw 'Release archive checksum does not match.'
}
$staging = Join-Path $buildRoot 'alternative-ui'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName -notmatch '^public/' -or $entry.FullName -match '(^|/)\.\.(/|$)|\\|:') {
            throw "Unexpected archive entry: $($entry.FullName)"
        }
    }
} finally {
    $zip.Dispose()
}
$freshStaging = Join-Path $buildRoot ('alternative-ui-' + [guid]::NewGuid().ToString('N'))
try {
    Expand-Archive -LiteralPath $archivePath -DestinationPath $freshStaging
    foreach ($asset in @('public/index.html', 'public/appsettings.json', 'public/_framework', 'public/i18n')) {
        if (-not (Test-Path -LiteralPath (Join-Path $freshStaging $asset))) { throw "Missing release asset: $asset" }
    }
    $bootSettings = Get-Content -LiteralPath (Join-Path $freshStaging 'public/js/boot.settings.js') -Raw
    if (-not $bootSettings.Contains("'$UiVersion-all'")) { throw 'UI version/international flavour does not match boot.settings.js.' }
    # Only replace this generated tree, and never traverse links during cleanup.
    if (Test-Path -LiteralPath $staging) {
        $existing = Get-Item -LiteralPath $staging
        if (-not $existing.PSIsContainer -or ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'The generated staging tree must be a regular directory.'
        }
        if (Get-ChildItem -LiteralPath $staging -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } | Select-Object -First 1) {
            throw 'The generated staging tree must not contain symbolic links or junctions.'
        }
        if ([IO.Path]::GetFullPath($staging) -ne (Join-Path $buildRoot 'alternative-ui')) { throw 'Unsafe staging path.' }
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
    Move-Item -LiteralPath $freshStaging -Destination $staging
} finally {
    if (Test-Path -LiteralPath $freshStaging) {
        if ([IO.Path]::GetFullPath($freshStaging).StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $freshStaging -Recurse -Force
        }
    }
}
$packagingCommit = git -C $repoRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve packaging source commit.' }
$dirty = @(git -C $repoRoot status --porcelain --ignore-submodules)
$metadata = [ordered]@{
    uiVersion = $UiVersion; uiCommit = $UiCommit; archiveSha256 = $ArchiveSha256.ToLowerInvariant()
    baseImage = $BaseImage; upstreamTag = $UpstreamTag; packagingCommit = $packagingCommit
    packagingDirty = ($dirty.Count -gt 0); publicationEligible = $false
}
$metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot '.build/build-inputs.json') -Encoding utf8
if ($StageOnly) { Write-Host "Staged international qbtmud $UiVersion."; return }
Write-Host "Building $Tag with qbtmud $UiVersion and LinuxServer $UpstreamTag..."
& docker build --platform $Platform --file (Join-Path $PSScriptRoot 'public/Dockerfile') --tag $Tag `
    --build-arg "BASE_IMAGE=$BaseImage" --build-arg "QBTMUD_VERSION=$UiVersion" `
    --build-arg "UI_COMMIT=$UiCommit" --build-arg "UI_SHA256=$ArchiveSha256" `
    --build-arg "PACKAGING_COMMIT=$packagingCommit" --build-arg "UPSTREAM_TAG=$UpstreamTag" `
    --build-arg "PACKAGE_REVISION=$PackageRevision" --build-arg "RELEASE_ID=$ReleaseId" --build-arg "ASSET_ID=$AssetId" `
    --build-arg "PACKAGING_DIRTY=$($metadata.packagingDirty.ToString().ToLowerInvariant())" `
    --build-arg "BUILD_DATE=$([DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))" `
    --build-arg "UPSTREAM_DIGEST=$($BaseImage.Split('@')[1])" $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Local Docker build failed.' }
if ($Test) { & (Join-Path $PSScriptRoot 'test-local.ps1') -Tag $Tag }
if ($Run) {
    $previousImage = $env:QBTMUD_IMAGE
    try {
        $env:QBTMUD_IMAGE = $Tag
        & docker compose --project-name qbtmud-local-test --file (Join-Path $PSScriptRoot 'compose.yaml') up --detach
        if ($LASTEXITCODE -ne 0) { throw 'Cannot start the local test container. Check Docker Compose and port availability.' }
    } finally { $env:QBTMUD_IMAGE = $previousImage }
    Write-Host 'Local UI: http://localhost:18080'
    Write-Host 'Find the initial admin password with: docker compose -p qbtmud-local-test -f docker/compose.yaml logs'
    Write-Host 'Stop the local test container with: docker compose -p qbtmud-local-test -f docker/compose.yaml down'
}
