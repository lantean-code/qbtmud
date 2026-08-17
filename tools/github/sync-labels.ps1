[CmdletBinding()]
param(
    [switch] $Prune
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ManifestPath = '.github/labels.json'

function Assert-Command {
    param(
        [Parameter(Mandatory)]
        [string] $Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required tool '$Name' is not installed."
    }
}

function Invoke-Gh {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    & gh @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "gh command failed with exit code $LASTEXITCODE."
    }
}

Assert-Command 'gh'

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Manifest not found: $ManifestPath"
}

$RepoSlug = & gh repo view --json owner,name --jq '.owner.login + "/" + .name'

if ($LASTEXITCODE -ne 0) {
    throw 'Unable to determine the current GitHub repository.'
}

$Owner, $Repo = $RepoSlug -split '/', 2
$LabelsEndpoint = "repos/$Owner/$Repo/labels"

$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json

foreach ($Label in $Manifest.labels) {
    $Name = [string] $Label.name
    $Color = [string] $Label.color
    $Description = [string] $Label.description
    $EncodedName = [Uri]::EscapeDataString($Name)

    & gh api "$LabelsEndpoint/$EncodedName" *> $null
    $Exists = $LASTEXITCODE -eq 0

    if ($Exists) {
        Invoke-Gh @(
            'api'
            '--method', 'PATCH'
            "$LabelsEndpoint/$EncodedName"
            '-f', "new_name=$Name"
            '-f', "color=$Color"
            '-f', "description=$Description"
        )

        Write-Host "Updated label: $Name"
    }
    else {
        Invoke-Gh @(
            'api'
            '--method', 'POST'
            $LabelsEndpoint
            '-f', "name=$Name"
            '-f', "color=$Color"
            '-f', "description=$Description"
        )

        Write-Host "Created label: $Name"
    }
}

if (-not $Prune) {
    return
}

$ManifestNames = @(
    $Manifest.labels |
        ForEach-Object { [string] $_.name }
)

$CurrentNames = @(
    & gh api "${LabelsEndpoint}?per_page=100" --paginate --jq '.[].name'

    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to retrieve the current repository labels.'
    }
)

foreach ($CurrentName in $CurrentNames) {
    if ($CurrentName -in $ManifestNames) {
        continue
    }

    $EncodedName = [Uri]::EscapeDataString($CurrentName)

    Invoke-Gh @(
        'api'
        '--method', 'DELETE'
        "$LabelsEndpoint/$EncodedName"
    )

    Write-Host "Deleted unmanaged label: $CurrentName"
}