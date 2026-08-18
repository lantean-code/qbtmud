[CmdletBinding()]
param(
    [switch] $PruneOptions
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$ManifestPath = Join-Path $RepoRoot '.github\project-fields.json'

function Assert-Command {
    param(
        [Parameter(Mandatory)]
        [string] $Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required tool '$Name' is not installed."
    }
}

function Invoke-GhRaw {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $output = & gh @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0) {
        $message = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
        throw "gh command failed with exit code $exitCode.`n$message"
    }

    return $output
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $output = Invoke-GhRaw -Arguments $Arguments
    $json = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ([string]::IsNullOrWhiteSpace($json)) {
        return $null
    }

    return $json | ConvertFrom-Json
}

function Invoke-GraphQl {
    param(
        [Parameter(Mandatory)]
        [string] $Query,

        [Parameter(Mandatory)]
        [hashtable] $Variables
    )

    $payload = @{
        query = $Query
        variables = $Variables
    } | ConvertTo-Json -Depth 30 -Compress

    $previousErrorActionPreference = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $output = $payload | & gh api graphql --input - 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $json = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ($exitCode -ne 0) {
        throw "GitHub GraphQL request failed with exit code $exitCode.`n$json"
    }

    $response = $json | ConvertFrom-Json

    if ($response.PSObject.Properties.Name -contains 'errors' -and @($response.errors).Count -gt 0) {
        $errors = @($response.errors | ForEach-Object { $_.message }) -join [Environment]::NewLine
        throw "GitHub GraphQL request returned errors:`n$errors"
    }

    return $response
}

function Get-ProjectFields {
    param(
        [Parameter(Mandatory)]
        [string] $ProjectId
    )

    $query = @'
query($projectId: ID!) {
  node(id: $projectId) {
    ... on ProjectV2 {
      fields(first: 100) {
        nodes {
          __typename
          ... on ProjectV2Field {
            id
            name
            dataType
          }
          ... on ProjectV2SingleSelectField {
            id
            name
            dataType
            options {
              id
              name
              color
              description
            }
          }
        }
      }
    }
  }
}
'@

    $response = Invoke-GraphQl -Query $query -Variables @{
        projectId = $ProjectId
    }

    return @(
        $response.data.node.fields.nodes |
            Where-Object {
                $_.__typename -eq 'ProjectV2Field' -or
                $_.__typename -eq 'ProjectV2SingleSelectField'
            }
    )
}

function Update-SingleSelectOptions {
    param(
        [Parameter(Mandatory)]
        [string] $FieldId,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]] $ExistingOptions,

        [Parameter(Mandatory)]
        [string[]] $DesiredNames,

        [Parameter(Mandatory)]
        [bool] $RemoveUnmanagedOptions
    )

    $optionInputs = [System.Collections.Generic.List[object]]::new()

    foreach ($desiredName in $DesiredNames) {
        $existing = @(
            $ExistingOptions |
                Where-Object { [string] $_.name -eq $desiredName }
        ) | Select-Object -First 1

        if ($null -ne $existing) {
            $optionInputs.Add(@{
                id = [string] $existing.id
                name = $desiredName
                color = [string] $existing.color
                description = if ($null -eq $existing.description) { '' } else { [string] $existing.description }
            })
        }
        else {
            $optionInputs.Add(@{
                name = $desiredName
                color = 'GRAY'
                description = ''
            })
        }
    }

    if (-not $RemoveUnmanagedOptions) {
        foreach ($existing in $ExistingOptions) {
            $existingName = [string] $existing.name

            if ($existingName -in $DesiredNames) {
                continue
            }

            $optionInputs.Add(@{
                id = [string] $existing.id
                name = $existingName
                color = [string] $existing.color
                description = if ($null -eq $existing.description) { '' } else { [string] $existing.description }
            })
        }
    }

    $mutation = @'
mutation(
  $fieldId: ID!,
  $options: [ProjectV2SingleSelectFieldOptionInput!]!
) {
  updateProjectV2Field(
    input: {
      fieldId: $fieldId,
      singleSelectOptions: $options
    }
  ) {
    projectV2Field {
      ... on ProjectV2SingleSelectField {
        id
      }
    }
  }
}
'@

    Invoke-GraphQl -Query $mutation -Variables @{
        fieldId = $FieldId
        options = @($optionInputs)
    } | Out-Null
}

Assert-Command 'gh'

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Manifest not found: $ManifestPath"
}

$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$ProjectName = [string] $Manifest.project.name
$ProjectDescription = [string] $Manifest.project.description

Push-Location $RepoRoot

try {
    $RepoInfo = Invoke-GhJson -Arguments @(
        'repo'
        'view'
        '--json', 'owner,name'
    )
}
finally {
    Pop-Location
}

$Owner = [string] $RepoInfo.owner.login

$projectQuery = @'
query($login: String!) {
  organization(login: $login) {
    projectsV2(first: 100) {
      nodes {
        id
        number
        title
        shortDescription
      }
    }
  }
}
'@

$projectResponse = Invoke-GraphQl -Query $projectQuery -Variables @{
    login = $Owner
}

if ($null -eq $projectResponse.data.organization) {
    throw "Repository owner '$Owner' is not an organization or is not accessible with the current gh authentication."
}

$projects = @(
    $projectResponse.data.organization.projectsV2.nodes |
        Where-Object { [string] $_.title -eq $ProjectName }
)

if ($projects.Count -ne 1) {
    throw "Expected exactly one project named '$ProjectName' for '$Owner', found $($projects.Count)."
}

$Project = $projects[0]
$ProjectId = [string] $Project.id
$ProjectNumber = [string] $Project.number

Write-Host "Project: $ProjectName (#$ProjectNumber)"

Invoke-GhRaw -Arguments @(
    'project'
    'edit'
    $ProjectNumber
    '--owner', $Owner
    '--description', $ProjectDescription
) | Out-Null

Write-Host 'Updated project description.'

$CurrentFields = Get-ProjectFields -ProjectId $ProjectId

foreach ($ManifestField in $Manifest.fields) {
    $FieldName = [string] $ManifestField.name
    $ManifestType = ([string] $ManifestField.type).ToLowerInvariant()

    switch ($ManifestType) {
        'single_select' {
            $ExpectedDataType = 'SINGLE_SELECT'
        }
        'text' {
            $ExpectedDataType = 'TEXT'
        }
        default {
            throw "Unsupported project field type '$($ManifestField.type)' for '$FieldName'."
        }
    }

    $CurrentField = @(
        $CurrentFields |
            Where-Object { [string] $_.name -eq $FieldName }
    ) | Select-Object -First 1

    if ($null -eq $CurrentField) {
        $arguments = @(
            'project'
            'field-create'
            $ProjectNumber
            '--owner', $Owner
            '--name', $FieldName
            '--data-type', $ExpectedDataType
        )

        if ($ExpectedDataType -eq 'SINGLE_SELECT') {
            $DesiredNames = @(
                $ManifestField.options |
                    ForEach-Object { [string] $_ }
            )

            if ($DesiredNames.Count -eq 0) {
                throw "Single-select field '$FieldName' must define at least one option."
            }

            $arguments += @(
                '--single-select-options', ($DesiredNames -join ',')
            )
        }

        Invoke-GhRaw -Arguments $arguments | Out-Null
        Write-Host "Created field: $FieldName"

        $CurrentFields = Get-ProjectFields -ProjectId $ProjectId
        $CurrentField = @(
            $CurrentFields |
                Where-Object { [string] $_.name -eq $FieldName }
        ) | Select-Object -First 1

        if ($null -eq $CurrentField) {
            throw "Created project field '$FieldName' but could not retrieve it afterwards."
        }
    }

    if ([string] $CurrentField.dataType -ne $ExpectedDataType) {
        throw "Project field '$FieldName' has type '$($CurrentField.dataType)' but the manifest requires '$ExpectedDataType'."
    }

    if ($ExpectedDataType -ne 'SINGLE_SELECT') {
        Write-Host "Field is current: $FieldName"
        continue
    }

    $DesiredNames = @(
        $ManifestField.options |
            ForEach-Object { [string] $_ }
    )

    if ($DesiredNames.Count -eq 0) {
        throw "Single-select field '$FieldName' must define at least one option."
    }

    Update-SingleSelectOptions `
        -FieldId ([string] $CurrentField.id) `
        -ExistingOptions @($CurrentField.options) `
        -DesiredNames $DesiredNames `
        -RemoveUnmanagedOptions $PruneOptions.IsPresent

    if ($PruneOptions) {
        Write-Host "Synced field options (pruned): $FieldName"
    }
    else {
        Write-Host "Synced field options: $FieldName"
    }

    $CurrentFields = Get-ProjectFields -ProjectId $ProjectId
}

Write-Host ''
Write-Host 'Project field sync complete.'

if (-not $PruneOptions) {
    Write-Host 'Unmanaged single-select options were preserved. Use -PruneOptions to remove them.'
}
