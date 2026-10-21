#!/bin/bash
set -euo pipefail

config=${1:-/config/qBittorrent/qBittorrent.conf}
mode=${2:-write}
parser=${QBTMUD_CONFIG_PARSER:-/usr/local/share/qbtmud/config.awk}
fail() { echo "[qbtmud] $1" >&2; exit 1; }
[[ $mode == write || $mode == verify ]] || fail 'Unknown configuration operation.'
[[ -f $config && ! -L $config ]] || fail 'Configuration must be an existing regular file, not a symbolic link.'
[[ -r $config ]] || fail 'Configuration cannot be read.'

# Even --version initialises qBittorrent settings. Isolate that probe from /config
# so a root-run startup hook cannot migrate or change ownership of user settings.
probe=$(mktemp -d)
trap 'rm -rf -- "$probe"' EXIT
version=$(PATH="$PATH:/app" qbittorrent-nox --profile="$probe" --version) || fail 'Cannot determine the qBittorrent version.'
rm -rf -- "$probe"
trap - EXIT
[[ $version =~ ^qBittorrent\ v([0-9]+)\.([0-9]+)\.([0-9]+) ]] || fail 'Unrecognised qBittorrent version.'
modern=0
if (( BASH_REMATCH[1] > 5 || (BASH_REMATCH[1] == 5 && BASH_REMATCH[2] >= 3) )); then
    modern=1
fi

status=0
awk -v mode=verify -v modern="$modern" -f "$parser" "$config" || status=$?
[[ $status != 1 ]] || fail 'Configuration is malformed; the original file was retained.'
if [[ $mode == verify ]]; then
    [[ $status == 0 ]] || fail 'Required alternative WebUI preferences are missing or incorrect.'
    exit 0
fi
[[ $status == 0 ]] && exit 0
[[ $status == 2 ]] || fail 'Configuration validation failed.'
permissions=$(stat -c '%a' "$config")
(( (8#$permissions & 0200) != 0 )) || fail 'Configuration owner has no write permission.'
directory=$(dirname "$config")
temporary=$(mktemp "$directory/.qbtmud.XXXXXX")
trap 'rm -f -- "$temporary"' EXIT
cp -p -- "$config" "$temporary"
awk -v mode=write -v modern="$modern" -f "$parser" "$config" > "$temporary" || fail 'Configuration transformation failed; the original file was retained.'
awk -v mode=verify -v modern="$modern" -f "$parser" "$temporary" || fail 'Transformed configuration failed validation.'
if ! cmp -s -- "$config" "$temporary"; then
    mv -f -- "$temporary" "$config"
    echo '[qbtmud] Alternative WebUI preferences configured.'
fi
