# Separate API and UI Hosting

This page covers setups where qbtmud is served from a different host, port, or deployment target than the qBittorrent Web API.

## Why this is more complex

The default qBittorrent alternative WebUI model keeps the UI and API closely coupled.

Once the UI and API are hosted separately, you need to consider:

- how the browser reaches the qBittorrent API
- whether the UI and API are same-origin or cross-origin
- how authentication and cookies behave in the browser
- how application routes are served on refresh

## Minimum questions to answer

Before treating a split-host deployment as supported in your environment, answer these questions:

1. Where is qbtmud hosted?
2. Where is the qBittorrent API hosted?
3. How does the browser authenticate against the API?
4. Are cookies or credentials available in the way the browser expects?
5. Do qbtmud routes reload correctly when refreshed directly?

## Validation checklist

- load the application shell successfully
- authenticate successfully
- load torrent data successfully
- navigate to secondary routes successfully
- refresh on a secondary route successfully

## Documentation status

This page is a starting point for future split-host guidance.

When a validated setup exists, this document should grow into a concrete deployment recipe rather than staying at the checklist level.
