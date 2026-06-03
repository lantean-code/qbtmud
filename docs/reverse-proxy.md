# Reverse Proxy Hosting

This page covers the considerations for running qbtmud behind a reverse proxy.

## Important routing constraint

If you use path-style routing through a reverse proxy, the proxy must be able to serve the qbtmud application entry point for qbtmud routes as well as pass API requests through correctly.

Without that fallback, deep links such as login or details routes can fail on refresh because the browser requests a route that the proxy does not map back to the app entry point.

## Questions to answer in your deployment

Before documenting or debugging a reverse proxy setup, be clear about:

- whether qbtmud is served from `/` or a subpath
- whether qBittorrent API requests stay on the same origin
- whether the proxy forwards unknown qbtmud routes back to the application entry point
- whether browser refresh on an internal qbtmud route succeeds

## Recommended validation

When testing a reverse proxy deployment, check at least these flows:

1. Open the root application URL.
2. Log in if required.
3. Navigate to a secondary route, such as a details page.
4. Refresh the browser on that route.
5. Confirm the app still loads correctly and can still call the qBittorrent API.

## Documentation status

This page is currently a guidance stub rather than a proxy-specific recipe.

Add concrete examples only after validating them against real configurations such as Nginx, Caddy, or Traefik.
