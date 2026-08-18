[CmdletBinding()]
param(
    [switch] $Prune
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$ManifestPath = Join-Path $RepoRoot '.github\labels.json'

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

function Invoke-GhJson {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $Output = & gh @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "gh command failed with exit code $LASTEXITCODE."
    }

    return ($Output -join [Environment]::NewLine) | ConvertFrom-Json
}

Assert-Command 'gh'

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Manifest not found: $ManifestPath"
}

Push-Location $RepoRoot

try {
    $RepoInfo = Invoke-GhJson @(
        'repo'
        'view'
        '--json', 'owner,name'
    )
}
finally {
    Pop-Location
}

$Owner = [string] $RepoInfo.owner.login
$Repo = [string] $RepoInfo.name
$LabelsEndpoint = "repos/$Owner/$Repo/labels"

$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json

$CurrentLabels = @(
    Invoke-GhJson @(
        'api'
        "${LabelsEndpoint}?per_page=100"
        '--paginate'
        '--slurp'
    ) |
        ForEach-Object { $_ }
)

$CurrentLabelsByName = @{}

foreach ($Label in $CurrentLabels) {
    foreach ($CurrentLabel in $Label) {
        $CurrentLabelsByName[[string] $CurrentLabel.name] = $CurrentLabel
    }
}

foreach ($Label in $Manifest.labels) {
    $Name = [string] $Label.name
    $Color = [string] $Label.color
    $Description = [string] $Label.description
    $EncodedName = [Uri]::EscapeDataString($Name)

    if ($CurrentLabelsByName.ContainsKey($Name)) {
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

foreach ($CurrentName in $CurrentLabelsByName.Keys) {
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