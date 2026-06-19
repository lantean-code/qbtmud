# Separate API and UI Hosting

This page covers the split-host setup where qbtmud is served from one origin and the qBittorrent API is served from another.

If a same-origin reverse proxy is possible, see [Reverse proxy hosting](reverse-proxy.md).

## Why this setup is harder

In the normal qBittorrent alternative WebUI model, the browser reaches the UI and API together.

In a split-host model, the browser must cross an origin boundary to reach the API. That introduces extra requirements around:

- the API URL
- CORS
- credentials and cookies
- route handling on the UI host

qbtmud includes credentials with API requests. If the API host does not allow that cross-origin flow, the deployment fails.

## Basic setup flow

1. Deploy the qbtmud `public` directory to the UI host.
2. Identify the public absolute URL for the qBittorrent API host.
3. Edit the deployed `public/appsettings.json`.
4. Set `Api.BaseUrl` to an absolute `http` or `https` URL.
5. If the UI host serves qbtmud under a subpath, rewrite `<base href="/">` in the HTML response.
6. Keep `Routing.Mode` on `"Hash"` unless the UI host also supports proper path-route fallback.

Example:

```json
{
  "Api": {
    "BaseUrl": "https://qbt.example.com/"
  },
  "Routing": {
    "Mode": "Hash"
  }
}
```

qbtmud appends `api/v2/` automatically if the configured absolute URL does not already include it.

## Choosing `Api.BaseUrl`

Split-host examples:

- `"https://qbt.example.com/"`
- `"https://qbt.example.com/qbt/"`
- `"https://qbt.example.com/api/v2/"`

## Configure qBittorrent for CORS

If the browser is calling the qBittorrent API directly from another origin, qBittorrent must return CORS headers on its WebUI responses.

Because qbtmud sends credentialed browser requests, the API response must:

- allow the exact qbtmud UI origin
- allow credentials
- not use `*` as the allowed origin

### Minimum header set

For a qbtmud UI hosted at `https://ui.example.com`:

```text
Access-Control-Allow-Origin: https://ui.example.com
Access-Control-Allow-Credentials: true
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type
```

Do not use:

```text
Access-Control-Allow-Origin: *
```

That is not valid for credentialed browser requests.

### Configure the headers in qBittorrent

In qBittorrent:

1. Open the Web UI settings.
2. Enable custom HTTP headers.
3. Add one header per line using the values above.

If you edit `qBittorrent.conf` directly instead, use:

```ini
[Preferences]
WebUI\CustomHTTPHeadersEnabled=true
WebUI\CustomHTTPHeaders=Access-Control-Allow-Origin: https://ui.example.com\Access-Control-Allow-Credentials: true\Access-Control-Allow-Methods: GET, POST, OPTIONS\Access-Control-Allow-Headers: Content-Type
```

In `qBittorrent.conf`, the custom headers are separated by `\`.

### qBittorrent limitations

qBittorrent's custom headers are static.

They are a poor fit when:

- you need multiple allowed origins
- you need origin-specific logic
- you want more advanced CORS behaviour than fixed headers

In those cases, a same-origin proxy design is usually a better fit.

## Other requirements

Requirements:

1. The browser can reach the API URL from the UI origin.
2. The API host accepts credentialed requests from that UI origin.
3. Any cookies or session state required for qBittorrent authentication are actually sent and accepted.
4. The UI host serves the qbtmud entry point correctly for the routing mode you selected.
5. If the UI is under a subpath, the HTML response rewrites `<base href="/">` to that subpath.

## Troubleshooting

- If the app shell loads but login fails immediately, treat it as an API reachability, cookie, or CORS problem first.
- If the browser says `Access-Control-Allow-Origin` is missing or invalid, fix the qBittorrent custom headers before changing qbtmud.
- If the browser says credentials are not allowed, add `Access-Control-Allow-Credentials: true` and replace any wildcard origin with the exact qbtmud UI origin.
- If requests are going to the wrong origin, correct `Api.BaseUrl` in the deployed `public/appsettings.json`.
- If the app shell fails under a subpath, fix the HTML `<base href="...">` rewrite on the UI host before changing API settings again.
- If refreshes fail on the UI host, stay on `Hash` or add path-route fallback before trying `Path`.
