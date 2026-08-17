# GitHub Governance Tools

This directory contains repo-side helpers for keeping qbtmud's GitHub configuration intentional and reviewable.

## Files

- `.github/labels.json`: canonical managed label set
- `.github/project-fields.json`: intended GitHub Project field and view model
- `.github/issue-triage.yml`: triage workflow and routing guidance

## Scripts

### `sync-labels.sh`

Creates or updates managed labels from `.github/labels.json`.

Default behaviour is non-destructive:

- labels present in the manifest are created or updated
- labels not present in the manifest are left alone

The script assumes it is being run from this repository and targets the current GitHub repository from `gh repo view`.

Pass `--prune` only when you deliberately want to delete labels that are not in the manifest.

## Suggested Rollout

1. Apply labels with `sync-labels.sh`.
2. Create issue forms and supporting docs through a normal pull request.
3. Once a token with project scopes is available, create the roadmap project using `.github/project-fields.json` as the source of truth.
