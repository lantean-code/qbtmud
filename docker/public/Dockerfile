ARG BASE_IMAGE=lscr.io/linuxserver/qbittorrent:5.3.0_v2.1.2-ls484@sha256:e887804fa9de4c60f5f0fb1f3a7e4febe40a8f69fe736f70c4e8589b1f868dab
FROM ${BASE_IMAGE}

ARG QBTMUD_VERSION
ARG UI_COMMIT
ARG PACKAGING_COMMIT
ARG PACKAGING_DIRTY=false
ARG UI_SHA256
ARG UPSTREAM_TAG
ARG UPSTREAM_DIGEST
ARG PACKAGE_REVISION=r1
ARG BUILD_DATE
ARG RELEASE_ID
ARG ASSET_ID
LABEL org.opencontainers.image.source="https://github.com/lantean-code/qbtmud" \
      org.opencontainers.image.title="qbtmud with LinuxServer qBittorrent" \
      org.opencontainers.image.version="${QBTMUD_VERSION}" \
      org.opencontainers.image.revision="${PACKAGING_COMMIT}" \
      org.opencontainers.image.created="${BUILD_DATE}" \
      io.github.lantean-code.qbtmud.ui.commit="${UI_COMMIT}" \
      io.github.lantean-code.qbtmud.ui.release-tag-commit="${UI_COMMIT}" \
      io.github.lantean-code.qbtmud.ui.release-id="${RELEASE_ID}" \
      io.github.lantean-code.qbtmud.ui.asset-id="${ASSET_ID}" \
      io.github.lantean-code.qbtmud.ui.sha256="${UI_SHA256}" \
      io.github.lantean-code.qbtmud.upstream.tag="${UPSTREAM_TAG}" \
      io.github.lantean-code.qbtmud.upstream.digest="${UPSTREAM_DIGEST}" \
      io.github.lantean-code.qbtmud.packaging.revision="${PACKAGE_REVISION}"
LABEL io.github.lantean-code.qbtmud.packaging.dirty="${PACKAGING_DIRTY}"

ENV S6_BEHAVIOUR_IF_STAGE2_FAILS=2
COPY --chown=0:0 docker/.build/alternative-ui/ /opt/qbtmud/
COPY --chown=0:0 LICENSE /opt/qbtmud/LICENSE
COPY --chown=0:0 docker/public/THIRD-PARTY-NOTICES.md /opt/qbtmud/THIRD-PARTY-NOTICES.md
COPY --chown=0:0 docker/public/config.awk /usr/local/share/qbtmud/config.awk
COPY --chown=0:0 --chmod=0755 docker/public/configure.sh /usr/local/libexec/qbtmud/configure
COPY --chown=0:0 --chmod=0755 docker/public/startup.sh /usr/local/libexec/qbtmud/startup
COPY --chown=0:0 --chmod=0755 docker/public/10-configure-qbtmud.sh /custom-cont-init.d/10-configure-qbtmud

# Refuse a base with a different service graph. Keep the inherited runner intact.
RUN test -f /etc/s6-overlay/s6-rc.d/init-custom-files/type && \
    test -f /etc/s6-overlay/s6-rc.d/svc-qbittorrent/run && \
    test -d /etc/s6-overlay/s6-rc.d/svc-qbittorrent/dependencies.d && \
    mkdir -p /etc/s6-overlay/s6-rc.d/init-qbtmud-state/dependencies.d \
             /etc/s6-overlay/s6-rc.d/init-qbtmud-verify/dependencies.d && \
    printf 'oneshot\n' > /etc/s6-overlay/s6-rc.d/init-qbtmud-state/type && \
    printf '/usr/bin/with-contenv /usr/local/libexec/qbtmud/startup reset\n' > /etc/s6-overlay/s6-rc.d/init-qbtmud-state/up && \
    touch /etc/s6-overlay/s6-rc.d/init-qbtmud-state/dependencies.d/init-mods-end && \
    touch /etc/s6-overlay/s6-rc.d/init-custom-files/dependencies.d/init-qbtmud-state && \
    printf 'oneshot\n' > /etc/s6-overlay/s6-rc.d/init-qbtmud-verify/type && \
    printf '/usr/bin/with-contenv /usr/local/libexec/qbtmud/startup verify\n' > /etc/s6-overlay/s6-rc.d/init-qbtmud-verify/up && \
    touch /etc/s6-overlay/s6-rc.d/init-qbtmud-verify/dependencies.d/init-custom-files && \
    touch /etc/s6-overlay/s6-rc.d/svc-qbittorrent/dependencies.d/init-qbtmud-verify && \
    chmod -R a+rX /opt/qbtmud && \
    test -s /opt/qbtmud/public/index.html && \
    test -s /opt/qbtmud/public/appsettings.json && \
    test -s /opt/qbtmud/public/_framework/blazor.webassembly.js && \
    test -s /opt/qbtmud/public/_framework/dotnet.js && \
    test -s /opt/qbtmud/public/_framework/dotnet.native.wasm && \
    test -s /opt/qbtmud/public/js/boot.settings.js && \
    test -s /opt/qbtmud/public/js/boot.resources.js && \
    test -s /opt/qbtmud/public/i18n/webui_en.json && \
    find /opt/qbtmud/public -type f -exec sha256sum {} + > /usr/local/share/qbtmud/assets.sha256 && \
    test -s /usr/local/share/qbtmud/assets.sha256 && \
    chmod 0644 /usr/local/share/qbtmud/assets.sha256
