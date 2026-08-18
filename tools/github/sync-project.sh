#!/usr/bin/env bash
set -euo pipefail

prune_options=false

usage() {
    cat <<'USAGE'
Usage: sync-project.sh [--prune-options]

Synchronizes the project description, managed fields, and single-select options
from .github/project-fields.json.

By default, existing single-select options that are not present in the manifest
are preserved. Pass --prune-options to remove unmanaged options from fields
that are defined in the manifest.
USAGE
}

require_tool() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required tool '$1' is not installed." >&2
        exit 1
    fi
}

graphql() {
    local query="$1"
    local variables="$2"
    local response

    response="$({
        jq -cn \
            --arg query "$query" \
            --argjson variables "$variables" \
            '{query: $query, variables: $variables}'
    } | gh api graphql --input -)"

    if jq -e '.errors != null and (.errors | length) > 0' >/dev/null <<<"$response"; then
        echo "GitHub GraphQL request returned errors:" >&2
        jq -r '.errors[].message' <<<"$response" >&2
        exit 1
    fi

    printf '%s\n' "$response"
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --prune-options)
            prune_options=true
            shift
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

require_tool gh
require_tool jq

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
manifest_path="$repo_root/.github/project-fields.json"

if [[ ! -f "$manifest_path" ]]; then
    echo "Manifest not found: $manifest_path" >&2
    exit 1
fi

repo_info="$(cd "$repo_root" && gh repo view --json owner,name)"
owner="$(jq -r '.owner.login' <<<"$repo_info")"
project_name="$(jq -r '.project.name' "$manifest_path")"
project_description="$(jq -r '.project.description // ""' "$manifest_path")"

project_query='query($login: String!) {
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
}'

project_variables="$(jq -cn --arg login "$owner" '{login: $login}')"
project_response="$(graphql "$project_query" "$project_variables")"

if [[ "$(jq -r '.data.organization == null' <<<"$project_response")" == "true" ]]; then
    echo "Repository owner '$owner' is not an organization or is not accessible with the current gh authentication." >&2
    exit 1
fi

project_count="$(
    jq \
        --arg name "$project_name" \
        '[.data.organization.projectsV2.nodes[] | select(.title == $name)] | length' \
        <<<"$project_response"
)"

if [[ "$project_count" -ne 1 ]]; then
    echo "Expected exactly one project named '$project_name' for '$owner', found $project_count." >&2
    exit 1
fi

project_id="$(
    jq -r \
        --arg name "$project_name" \
        '.data.organization.projectsV2.nodes[] | select(.title == $name) | .id' \
        <<<"$project_response"
)"

project_number="$(
    jq -r \
        --arg name "$project_name" \
        '.data.organization.projectsV2.nodes[] | select(.title == $name) | .number' \
        <<<"$project_response"
)"

echo "Project: $project_name (#$project_number)"

gh project edit "$project_number" \
    --owner "$owner" \
    --description "$project_description" >/dev/null

echo "Updated project description."

fields_query='query($projectId: ID!) {
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
}'

get_fields() {
    local variables
    variables="$(jq -cn --arg projectId "$project_id" '{projectId: $projectId}')"
    graphql "$fields_query" "$variables"
}

update_options_mutation='mutation(
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
}'

fields_response="$(get_fields)"

field_count="$(jq '.fields | length' "$manifest_path")"

for ((field_index = 0; field_index < field_count; field_index++)); do
    manifest_field="$(jq -c ".fields[$field_index]" "$manifest_path")"
    field_name="$(jq -r '.name' <<<"$manifest_field")"
    manifest_type="$(jq -r '.type | ascii_downcase' <<<"$manifest_field")"

    case "$manifest_type" in
        single_select)
            expected_data_type='SINGLE_SELECT'
            ;;
        text)
            expected_data_type='TEXT'
            ;;
        *)
            echo "Unsupported project field type '$manifest_type' for '$field_name'." >&2
            exit 1
            ;;
    esac

    current_field="$(
        jq -c \
            --arg name "$field_name" \
            '.data.node.fields.nodes[] |
             select((.__typename == "ProjectV2Field" or .__typename == "ProjectV2SingleSelectField") and .name == $name)' \
            <<<"$fields_response" |
            head -n 1
    )"

    if [[ -z "$current_field" ]]; then
        create_args=(
            project field-create "$project_number"
            --owner "$owner"
            --name "$field_name"
            --data-type "$expected_data_type"
        )

        if [[ "$expected_data_type" == 'SINGLE_SELECT' ]]; then
            desired_count="$(jq '.options | length' <<<"$manifest_field")"

            if [[ "$desired_count" -eq 0 ]]; then
                echo "Single-select field '$field_name' must define at least one option." >&2
                exit 1
            fi

            desired_csv="$(jq -r '.options | join(",")' <<<"$manifest_field")"
            create_args+=(--single-select-options "$desired_csv")
        fi

        gh "${create_args[@]}" >/dev/null
        echo "Created field: $field_name"

        fields_response="$(get_fields)"
        current_field="$(
            jq -c \
                --arg name "$field_name" \
                '.data.node.fields.nodes[] |
                 select((.__typename == "ProjectV2Field" or .__typename == "ProjectV2SingleSelectField") and .name == $name)' \
                <<<"$fields_response" |
                head -n 1
        )"

        if [[ -z "$current_field" ]]; then
            echo "Created project field '$field_name' but could not retrieve it afterwards." >&2
            exit 1
        fi
    fi

    current_data_type="$(jq -r '.dataType' <<<"$current_field")"

    if [[ "$current_data_type" != "$expected_data_type" ]]; then
        echo "Project field '$field_name' has type '$current_data_type' but the manifest requires '$expected_data_type'." >&2
        exit 1
    fi

    if [[ "$expected_data_type" != 'SINGLE_SELECT' ]]; then
        echo "Field is current: $field_name"
        continue
    fi

    desired_names="$(jq -c '.options' <<<"$manifest_field")"
    desired_count="$(jq 'length' <<<"$desired_names")"

    if [[ "$desired_count" -eq 0 ]]; then
        echo "Single-select field '$field_name' must define at least one option." >&2
        exit 1
    fi

    existing_options="$(jq -c '.options // []' <<<"$current_field")"

    if [[ "$prune_options" == true ]]; then
        prune_json=true
    else
        prune_json=false
    fi

    option_inputs="$(
        jq -cn \
            --argjson desired "$desired_names" \
            --argjson existing "$existing_options" \
            --argjson prune "$prune_json" '
                [
                  $desired[] as $name |
                  ($existing | map(select(.name == $name)) | .[0]) as $match |
                  if $match == null then
                    {
                      name: $name,
                      color: "GRAY",
                      description: ""
                    }
                  else
                    {
                      id: $match.id,
                      name: $name,
                      color: $match.color,
                      description: ($match.description // "")
                    }
                  end
                ]
                +
                (if $prune then
                   []
                 else
                   [
                     $existing[] as $option |
                     select(($desired | index($option.name)) == null) |
                     {
                       id: $option.id,
                       name: $option.name,
                       color: $option.color,
                       description: ($option.description // "")
                     }
                   ]
                 end)
            '
    )"

    field_id="$(jq -r '.id' <<<"$current_field")"
    update_variables="$(
        jq -cn \
            --arg fieldId "$field_id" \
            --argjson options "$option_inputs" \
            '{fieldId: $fieldId, options: $options}'
    )"

    graphql "$update_options_mutation" "$update_variables" >/dev/null

    if [[ "$prune_options" == true ]]; then
        echo "Synced field options (pruned): $field_name"
    else
        echo "Synced field options: $field_name"
    fi

    fields_response="$(get_fields)"
done

echo
echo "Project field sync complete."

if [[ "$prune_options" != true ]]; then
    echo "Unmanaged single-select options were preserved. Use --prune-options to remove them."
fi
