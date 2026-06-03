# Contributing to qbtmud

Thanks for contributing to qbtmud.

## Before You Open Something

Use the right GitHub surface for the job:

- **Issues** are for actionable, scoped work that can be triaged into the roadmap.
- **Discussions** are for support questions, early-stage ideas, community chat, and showcases.
- **Pull requests** are for proposed code or documentation changes.

If you are unsure whether something is actionable yet, start in Discussions first.

## Where to Post

- **Bug reports**: open an issue with the bug report form.
- **Feature proposals**: open an issue when the request is specific enough to estimate and review.
- **UX improvements**: open an issue when you can describe the current workflow pain and the desired outcome.
- **Performance regressions**: open an issue with dataset size, browser, deployment mode, and measured impact.
- **Documentation or deployment guidance gaps**: open an issue with the deployment scenario and the missing guidance.
- **How do I...?** questions: use Discussions `Q&A`.
- **Broad ideas or exploratory suggestions**: use Discussions `Ideas`.
- **Screenshots, theme showcases, and integrations**: use Discussions `Show and tell`.

## What Makes a Good Issue

Issues should be specific, reproducible, and scoped.

Include:

- the qbtmud version or commit
- the qBittorrent version
- browser and device details where relevant
- deployment shape where relevant, such as direct alternative WebUI hosting, reverse proxy, or split API/UI hosting
- a clear description of the observed behaviour
- the expected behaviour
- reproduction steps or concrete workflow context
- screenshots, recordings, or logs when they materially help

## Triage Model

New issues should normally enter with `status/needs-triage`.

Maintainers then classify them with:

- one `type/*` label
- one or more `area/*` labels
- a `priority/*` label when priority is understood
- `needs/*` labels when more information or upstream validation is required

Issues that are not actionable may be redirected to Discussions or closed with an explanation.

## Feature Parity Guardrail

qbtmud aims for parity with qBittorrent's shipped WebUI while improving usability, installability, and polish.

When proposing a change, make it clear whether it is:

- parity work
- a usability improvement on an existing workflow
- deployment or documentation work
- a deliberate qbtmud-specific enhancement

If a proposal intentionally changes qBittorrent semantics, call that out explicitly.

## Pull Requests

Keep pull requests scoped.

- Explain the user-visible or reviewer-relevant intent in `Summary`.
- List the code and design changes in `What Changed`.
- Describe the coverage added or updated in `Testing`.
- Use `Notes` for rollout risks, reviewer guidance, or migration concerns.

For larger changes, link the tracking issue or discussion so reviewers have the full context.
