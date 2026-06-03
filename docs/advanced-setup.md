# Advanced Setup

This guide covers scenarios that go beyond the standard "extract release and point qBittorrent at it" installation model.

## When you need this

Use the advanced setup guidance if you are:

- building qbtmud from source
- serving qbtmud behind a reverse proxy
- separating API and UI hosting
- debugging refresh or deep-link routing problems

## Standard model versus non-standard model

The standard qbtmud setup is direct alternative WebUI hosting through qBittorrent itself.

That is the simplest and most reliable arrangement because qBittorrent serves the WebUI assets directly from the configured alternative WebUI directory.

More advanced hosting topologies can work, but they require you to think about:

- where the application entry point is served from
- whether deep links and refreshes resolve back to the app
- how the browser reaches the qBittorrent API
- whether the UI and API share an origin or cross-origin boundary

## Related guides

- [Build from source](build-from-source.md)
- [Reverse proxy hosting](reverse-proxy.md)
- [Separate API and UI hosting](separate-api-ui-hosting.md)

## Current documentation scope

These docs intentionally avoid claiming a one-size-fits-all configuration for every proxy or hosting topology.

As the deployment story becomes more formalised, expand these guides with validated recipes, diagrams, and known-good examples.
