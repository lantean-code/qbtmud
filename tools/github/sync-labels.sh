#!/usr/bin/env bash
set -euo pipefail

manifest_path=".github/labels.json"
prune="false"

usage() {
    cat <<'EOF'
Usage: sync-labels.sh [--prune]

Creates or updates labels defined in .github/labels.json for the current
repository.

By default this script is non-destructive and will not delete labels that are
not present in the manifest. Pass --prune to remove unmanaged labels.
EOF
}

require_tool() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required tool '$1' is not installed." >&2
        exit 1
    fi
}

url_encode() {
    jq -nr --arg value "$1" '$value|@uri'
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --prune)
            prune="true"
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

if [[ ! -f "$manifest_path" ]]; then
    echo "Manifest not found: $manifest_path" >&2
    exit 1
fi

repo_slug="$(gh repo view --json owner,name --jq '.owner.login + "/" + .name')"
owner="${repo_slug%/*}"
repo="${repo_slug#*/}"
labels_endpoint="repos/$owner/$repo/labels"

manifest_labels="$(jq -c '.labels[]' "$manifest_path")"

while IFS= read -r label; do
    name="$(jq -r '.name' <<<"$label")"
    color="$(jq -r '.color' <<<"$label")"
    description="$(jq -r '.description' <<<"$label")"
    encoded_name="$(url_encode "$name")"

    if gh api "$labels_endpoint/$encoded_name" >/dev/null 2>&1; then
        gh api --method PATCH "$labels_endpoint/$encoded_name" \
            -f new_name="$name" \
            -f color="$color" \
            -f description="$description" >/dev/null
        echo "Updated label: $name"
    else
        gh api --method POST "$labels_endpoint" \
            -f name="$name" \
            -f color="$color" \
            -f description="$description" >/dev/null
        echo "Created label: $name"
    fi
done <<<"$manifest_labels"

if [[ "$prune" != "true" ]]; then
    exit 0
fi

manifest_names="$(
    jq -r '.labels[].name' "$manifest_path" \
        | sort
)"

current_names="$(
    gh api "$labels_endpoint?per_page=100" --paginate \
        | jq -r '.[].name' \
        | sort
)"

while IFS= read -r current_name; do
    [[ -z "$current_name" ]] && continue

    if grep -Fxq "$current_name" <<<"$manifest_names"; then
        continue
    fi

    encoded_name="$(url_encode "$current_name")"
    gh api --method DELETE "$labels_endpoint/$encoded_name" >/dev/null
    echo "Deleted unmanaged label: $current_name"
done <<<"$current_names"
