# Docker installation

Run qBittorrent with the international qbtmud WebUI already installed and enabled using `ghcr.io/lantean-code/qbtmud`. The image is based on LinuxServer's qBittorrent container and supports `linux/amd64` and `linux/arm64`.

## Before you start

You need Docker with Linux containers and Docker Compose. Docker Desktop provides both on Windows and macOS; on Linux, use Docker Engine with the Compose plugin.

If you already run qBittorrent in Docker, see [Migrating an existing LinuxServer container](#migrating-an-existing-linuxserver-container) before creating a new configuration.

## 1. Create your Compose file

Create a directory for the container and save this as `compose.yaml`:

```yaml
services:
  qbittorrent:
    image: ghcr.io/lantean-code/qbtmud:latest
    container_name: qbtmud
    environment:
      PUID: "1000"
      PGID: "1000"
      TZ: Etc/UTC
      WEBUI_PORT: "8080"
      TORRENTING_PORT: "6881"
    volumes:
      - ./config:/config
      - ./downloads:/downloads
    ports:
      - "8080:8080"
      - "6881:6881"
      - "6881:6881/udp"
    restart: unless-stopped
```

Set `TZ` to your timezone. On Linux, run `id` to find your user/group IDs, set `PUID`/`PGID` accordingly, and ensure that user can write to the mounted directories. For Docker Desktop, the example IDs are a starting point.

`./config` stores settings, credentials and torrent state. `./downloads` stores downloaded files. These directories are relative to your Compose file and persist when the container is replaced. You can use absolute paths or additional download mounts instead; keep the paths consistent with your qBittorrent settings.

## 2. Start the container

From the directory containing `compose.yaml`, run:

```sh
docker compose pull
docker compose up -d
```

Open [http://localhost:8080](http://localhost:8080). If Docker runs on another computer, replace `localhost` with that computer's address.

There is no release ZIP to download or alternative WebUI path to configure. qbtmud's files are embedded in the image at `/opt/qbtmud/public`.

## 3. Sign in and finish setup

Read the startup logs:

```sh
docker compose logs qbittorrent
```

For a fresh configuration, sign in as `admin` using the temporary password printed in the logs. Set a permanent password in qBittorrent's WebUI settings, then complete qbtmud's first-run setup. If you reuse an existing `/config`, your existing credentials are retained.

For access through a public hostname, follow the [reverse proxy guide](reverse-proxy.md).

## Ports and permissions

The normal LinuxServer options, including `PUID`, `PGID`, `TZ`, `WEBUI_PORT`, `TORRENTING_PORT`, `UMASK` and persistent mounts, are inherited. See the [LinuxServer qBittorrent documentation](https://docs.linuxserver.io/images/docker-qbittorrent/) for their usage.

If port 8080 is already used, change `WEBUI_PORT` and both sides of its port mapping together; for example, `WEBUI_PORT: "8081"` and `"8081:8081"`. For a different torrent port, change `TORRENTING_PORT` and both TCP/UDP mappings together. Configure your firewall/router for the chosen torrent port if you want incoming peer connections.

Use `PUID`/`PGID` to choose the application identity. Do not add Docker `--user` or Compose `user:`: initialisation starts as root, then LinuxServer runs qBittorrent as its normal `abc` user. `/config` must be writable. Advanced read-only operation has not been validated for this derivative.

At every startup, the image enables the alternative WebUI and restores its root to `/opt/qbtmud`. Other qBittorrent settings are retained. Do not mount over `/opt/qbtmud` or replace the bundled `/custom-cont-init.d` directory; doing so can hide the UI or its required startup hook. Custom scripts/mods must preserve those settings and the startup checks.

This is a qbtmud derivative image. LinuxServer does not maintain or support it; use [qbtmud support](https://github.com/lantean-code/qbtmud/blob/develop/SUPPORT.md) for problems specific to this image.

## Choosing a version

| Image tag | Behaviour |
| --- | --- |
| `latest` | Latest eligible stable qbtmud release with a successfully tested LinuxServer base. |
| `3.0.0` | qbtmud 3.0.0; its base can advance after upstream updates pass tests. |
| `3.0.0-qbt5.2.4-lt2.0.15-ls480-r1` | Immutable combination of UI, qBittorrent/libtorrent, LinuxServer build and packaging revision. |
| `@sha256:<published-index-digest>` | Exact multi-platform image; use `ghcr.io/lantean-code/qbtmud@sha256:…` in Compose. |

Find available tags and digests on the [GHCR package page](https://github.com/orgs/lantean-code/packages/container/package/qbtmud). New stable UI releases trigger publication, and LinuxServer's stable libtorrent 2 base is checked daily. Both architectures must pass the container tests before aliases are updated.

## Updating and stopping

Back up `/config` before upgrading, especially when the image includes a newer qBittorrent version. To update a moving tag:

```sh
docker compose pull
docker compose up -d
```

Your mounted configuration and downloads remain in place. Reload the browser after an update. If you use an immutable tag or digest, edit `image:` to select a new release before pulling.

To stop and remove the container while retaining the example's bind-mounted data:

```sh
docker compose down
```

## Migrating an existing LinuxServer container

These image-only migration steps apply to compatible LinuxServer qBittorrent deployments that use `/config/qBittorrent/qBittorrent.conf` and the same torrent-state layout. Other images may store the profile elsewhere or bundle features such as VPN routing; migrate their configuration, torrent state and network features explicitly before switching. Keeping a mount named `/config` alone does not guarantee compatibility.

1. Stop the existing qBittorrent container and back up its `/config` directory.
2. Change its image to `ghcr.io/lantean-code/qbtmud:latest` or a chosen fixed tag. Check the included qBittorrent version before upgrading.
3. Keep the existing `/config`, download mounts, container download paths, environment variables and port mappings.
4. Remove mounts that hide `/opt/qbtmud` or the bundled `/custom-cont-init.d` directory, and review any custom scripts that alter the WebUI configuration.
5. Pull the image and recreate the container. Sign in using your existing credentials.

Do not run both containers against the same `/config` simultaneously. Standalone desktop installations should continue using the [release archive instructions](installation.md) unless you are also migrating qBittorrent itself into Docker.

## Troubleshooting

- **Container exits during startup:** inspect `docker compose logs qbittorrent` for `[qbtmud]` diagnostics. Check configuration permissions, custom mounts and scripts. The image stops if configuration or bundled UI validation fails.
- **Cannot open the WebUI:** check `docker compose ps`, the host address, firewall, and matching `WEBUI_PORT`/port mapping. Check for another application using the port.
- **Permission denied for settings/downloads:** check mount paths and the configured `PUID`/`PGID` against directory ownership on Linux.
- **Old UI after an update:** confirm the container was recreated from the new image and refresh the browser without cached files.
