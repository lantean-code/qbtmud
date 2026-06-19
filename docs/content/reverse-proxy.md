# Reverse Proxy Hosting

Running qbtmud behind a reverse proxy.

The examples here assume familiarity with the chosen proxy product.

## Basic setup flow

1. Deploy the qbtmud `public` directory to the location your proxy will serve.
2. Choose the public qbtmud URL.
3. Choose the public qBittorrent API URL on the same public origin if possible.
4. Edit the deployed `public/appsettings.json`.
5. Start with:

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

6. If qbtmud is being published under a subpath, add the required HTML rewrite for `<base href="/">`.
7. Reload qbtmud through the proxy.

## Subpath hosting requirement

If qbtmud is published under a subpath such as `https://example.com/qbtmud/`, the HTML response must contain:

```html
<base href="/qbtmud/" />
```

qbtmud currently ships with `<base href="/" />`, so subpath hosting requires response rewriting.

## Choosing `Api.BaseUrl`

Typical public forms:

- `""` for the normal same-origin `/api/v2/` path
- `"/qbt/"` for an API at `https://example.com/qbt/api/v2/`
- `"./qbt/"` for an API path relative to the qbtmud app path

## Proxy examples for `<base href>` rewriting

### Nginx

```nginx
location /qbtmud/ {
    proxy_pass http://qbtmud_upstream/;
    proxy_set_header Accept-Encoding "";

    sub_filter_types text/html;
    sub_filter_once off;
    sub_filter '<base href="/">' '<base href="/qbtmud/">';
}
```

### Caddy

This requires a Caddy build that includes the `replace-response` module.

```caddyfile
{
    order replace after encode
}

example.com {
    handle /qbtmud/* {
        reverse_proxy http://qbtmud-upstream {
            header_up Accept-Encoding identity
        }

        replace "<base href=\"/\">" "<base href=\"/qbtmud/\">"
    }
}
```

### Traefik

This requires the `rewritebody` plugin.

Static configuration:

```yaml
experimental:
  plugins:
    rewritebody:
      modulename: github.com/traefik/plugin-rewritebody
      version: v0.3.1
```

Dynamic configuration:

```yaml
http:
  middlewares:
    qbtmud-base-href:
      plugin:
        rewritebody:
          rewrites:
            - regex: '<base href="/">'
              replacement: '<base href="/qbtmud/">'
```

Attach that middleware to the router serving the qbtmud HTML entry point.

## Clean URLs with `Path` routing

`Path` routing requires the proxy to return the qbtmud entry point for qbtmud routes.

1. Set `Routing.Mode` to `"Path"` in `public/appsettings.json`.
2. Keep the API configuration the same unless your public API path also changed.
3. Add route fallback in the proxy for qbtmud routes.
4. Test refreshes on deep links such as `/login` or `/details/{hash}`.

## Troubleshooting

- If login fails or data never loads, check `Api.BaseUrl` first.
- If deep-link refreshes fail, stay on `Hash` or add route fallback before retrying `Path`.
- If the app shell or CSS fails under a subpath, check the rewritten `<base href="...">` value first.
- If requests are going to the wrong host or path, review [Advanced setup](advanced-setup.md).
