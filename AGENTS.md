# AGENTS.md (root)

> Scope: This file gives high-level context and guardrails for the repository root. Deeper folders may add their own `AGENTS.md` files which take precedence for their subtrees (e.g., `./src/AGENTS.md`, `./test/AGENTS.md`).

## Project overview
- qbtmud is a drop-in replacement for qBittorrent's default WebUI, aiming for full feature parity with a modern UI.
- Primary goals: parity with the default WebUI, excellent UX, reliability, and easy installation.
- Non-goals: diverging from qBittorrent semantics without explicit design approval.

## Repository layout
- Solution: `Lantean.QBTMud.sln`
- Projects:
  - `Lantean.QBTMud` — Web UI host and published assets.
  - `Lantean.QBitTorrentClient` — client library for qBittorrent Web API.
  - `Lantean.QBTMud.Test` — unit tests.
  - `Lantean.QBitTorrentClient.Test` — unit tests.
- Config/conventions: `.editorconfig`, `.gitattributes`, `nuget.config`, `global.json` (SDK pin).

## Build, test, publish
- Prerequisites: .NET 10 SDK (use the version pinned by `global.json` if present).
  - Agents must verify the pinned SDK is available in the current environment; if `dotnet --info` does not list the required version, install it (e.g., via `dotnet-install.sh`) before running restore/build/test commands.
- Restore & build:
  - `dotnet restore --artifacts-path=/tmp/artifacts/qbtmud`
  - `dotnet build  --artifacts-path=/tmp/artifacts/qbtmud`
- Run tests:
  - `dotnet test --artifacts-path=/tmp/artifacts/qbtmud`
- Publish Web UI:
  - `dotnet publish Lantean.QBTMud -c Release`
  - Output (static assets): `Lantean.QBTMud/bin/Release/net9.0/publish/wwwroot/`

## Coding and test standards
- Source code rules and generation constraints live in `./src/AGENTS.md` (authoritative for code style, design, docs).
- Unit test rules live in `./test/AGENTS.md` (authoritative for test structure, naming, mocks, coverage).
- If rules conflict, the deeper file (closer to the change) wins; otherwise, follow both.

## How to work in this repo (for agents)
1. Read this file, then the relevant folder `AGENTS.md` (e.g., `src` or `test`).
2. When referencing MudBlazor or qBittorrent source:
   - Ensure the repos are checked out under `./ref/mudblazor` and `./ref/qBittorrent`.
   - Check out a tagged release, not a branch (do not use `develop`, `dev`, `master`, or `main`).
   - MudBlazor must match the installed package version used by this repo.
   - qBittorrent must match the most recent tag.
2. Before modifying code:
   - Confirm SDK target, nullable context, analyzers, and editorconfig rules.
   - Keep public surface consistent; do not break qBittorrent Web API expectations without approval.
3. When generating code:
   - Follow `./src/AGENTS.md` exactly (naming, formatting, docs, DI, async, security).
   - Prefer minimal, maintainable changes; avoid churn to unrelated files.
4. When writing tests:
   - Follow `./test/AGENTS.md` exactly (class/method naming, `_target`, mocks, coverage).
5. Before opening a PR:
   - Build succeeds, tests are green.
   - Public XML docs added/updated.
   - Changelog notes in the PR description (what changed, why, risks, testing).

## PR and review checklist
- [ ] Change is scoped and well-justified; no unrelated edits.
- [ ] Code adheres to `./src/AGENTS.md` standards.
- [ ] Tests adhere to `./test/AGENTS.md` and achieve required coverage.
- [ ] No secrets, tokens, or user-specific paths committed.
- [ ] Builds with the pinned SDK; `dotnet restore`, `build`, `test`, and `publish` succeed.
- [ ] Error messages and logs are clear and actionable.

## Communication & assumptions
- Do not guess. If any requirement, API contract, or behavior is unclear, ask for clarification.
- Prefer concise diffs and explicit rationale in commit messages and PR descriptions.
