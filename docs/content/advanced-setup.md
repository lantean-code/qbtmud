# Advanced Setup

Deployment settings reference for qbtmud.

## Covers

- you are changing `appsettings.json`
- you are hosting qbtmud behind a reverse proxy
- you are serving qbtmud under a subpath
- you are separating the UI and API
- you are debugging route refresh or API path issues

## Where to edit the settings

The deployment settings live in `appsettings.json` inside the deployed `public` directory.

Typical locations:

- release archive deployment: `<extract-root>/public/appsettings.json`
- local build deployment: `output/alternative-ui/public/appsettings.json`

Edit the deployed copy that the browser is loading, not the source copy in the repository.

## The supported settings

qbtmud supports these deployment keys:

```json
{
  "Api": {
    "BaseUrl": ""
  },
  "Routing": {
    "Mode": "Hash"
  }
}
```

## `Routing.Mode`

Supported values:

- `Hash`
- `Path`

`Hash` is the default mode. `Path` uses clean URLs such as `/login` and `/details/{hash}` and requires the host or proxy to return qbtmud's entry point for qbtmud routes.

## `Api.BaseUrl`

`Api.BaseUrl` tells qbtmud where the qBittorrent Web API should be reached from the browser.

Supported forms:

- empty string
- root-relative path such as `"/qbt/"`
- app-relative path such as `"./qbt/"`
- absolute `http` or `https` URL such as `"https://qbt.example.com/"`

qbtmud appends `api/v2/` automatically when you provide only a host or base path.

Valid examples:

```json
{ "Api": { "BaseUrl": "" } }
{ "Api": { "BaseUrl": "/" } }
{ "Api": { "BaseUrl": "/qbt/" } }
{ "Api": { "BaseUrl": "./qbt/" } }
{ "Api": { "BaseUrl": "https://qbt.example.com/" } }
{ "Api": { "BaseUrl": "https://qbt.example.com/api/v2/" } }
```

Unsupported forms fall back to the default API host. Common mistakes include:

- `"qbt/"` without a leading `/` or `./`
- host-like values without a scheme, such as `"qbt.example.com/qbt/"`
- non-HTTP schemes

## Choosing the right `Api.BaseUrl`

- `""` for the normal same-origin `/api/v2/` path
- `"/qbt/"` for an API published under a fixed root path
- `"./qbt/"` for an API path relative to the qbtmud app path
- an absolute URL for an API on another origin

## Subpath hosting

qbtmud currently ships with:

```html
<base href="/" />
```

That works as-is when qbtmud is served:

- directly by qBittorrent
- from the root of a dedicated host or subdomain

If qbtmud is published under a subpath such as `/qbtmud/`, you must rewrite the HTML response so the browser receives the matching base path:

```html
<base href="/qbtmud/" />
```

Without that rewrite, static assets, framework files, and client-side routes are resolved from `/`.

`Api.BaseUrl` does not fix this. It only affects API requests after the app has already loaded.

## Related guides

- [Build from source](build-from-source.md)
- [Reverse proxy hosting](reverse-proxy.md)
- [Separate API and UI hosting](separate-api-ui-hosting.md)
