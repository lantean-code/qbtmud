#!/bin/bash
set -euo pipefail

state=/run/qbtmud
config=/config/qBittorrent/qBittorrent.conf
fail() { echo "[qbtmud] $1" >&2; exit 1; }
validate_ui() {
    s6-setuidgid abc /bin/bash -c '
        test -r /opt/qbtmud/public/index.html && test -s /opt/qbtmud/public/index.html &&
        test -r /opt/qbtmud/public/appsettings.json &&
        test -d /opt/qbtmud/public/_framework && test -d /opt/qbtmud/public/i18n &&
        sha256sum -c /usr/local/share/qbtmud/assets.sha256 >/dev/null 2>&1 &&
        test -z "$(find /opt/qbtmud -type f ! -readable -print -quit)" &&
        test -z "$(find /opt/qbtmud -type d ! -executable -print -quit)"
    ' || fail 'Bundled UI assets are missing or unreadable by qBittorrent.'
}
case ${1:-} in
    reset)
        [[ $(id -u) == 0 ]] || fail 'Start as root and use PUID/PGID; Docker --user is not supported.'
        mkdir -p "$state"
        chmod 0700 "$state"
        rm -f "$state/success" "$state/generation"
        cat /proc/sys/kernel/random/uuid > "$state/generation"
        ;;
    configure)
        rm -f "$state/success"
        [[ -s $state/generation ]] || fail 'Startup state was not initialised.'
        validate_ui
        /usr/local/libexec/qbtmud/configure "$config"
        cp "$state/generation" "$state/success"
        ;;
    verify)
        [[ ${S6_BEHAVIOUR_IF_STAGE2_FAILS:-} == 2 ]] || fail 'S6_BEHAVIOUR_IF_STAGE2_FAILS must remain 2.'
        [[ -s $state/generation && -s $state/success ]] || fail 'Configuration hook did not complete successfully.'
        cmp -s "$state/generation" "$state/success" || fail 'Configuration success belongs to a different startup.'
        validate_ui
        /usr/local/libexec/qbtmud/configure "$config" verify
        s6-setuidgid abc test -w "$config" || fail 'Configuration is not writable by qBittorrent.'
        echo '[qbtmud] Startup validation passed.'
        ;;
    *) fail 'Unknown startup operation.' ;;
esac
