#!/bin/bash
set -euo pipefail

config=${1:-/config/qBittorrent/qBittorrent.conf}
mode=${2:-write}
parser=${QBTMUD_CONFIG_PARSER:-/usr/local/share/qbtmud/config.awk}
fail() { echo "[qbtmud] $1" >&2; exit 1; }
[[ $mode == write || $mode == verify ]] || fail 'Unknown configuration operation.'
[[ -f $config && ! -L $config ]] || fail 'Configuration must be an existing regular file, not a symbolic link.'
[[ -r $config ]] || fail 'Configuration cannot be read.'

status=0
awk -v mode=verify -f "$parser" "$config" || status=$?
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
awk -v mode=write -f "$parser" "$config" > "$temporary" || fail 'Configuration transformation failed; the original file was retained.'
awk -v mode=verify -f "$parser" "$temporary" || fail 'Transformed configuration failed validation.'
if ! cmp -s -- "$config" "$temporary"; then
    mv -f -- "$temporary" "$config"
    echo '[qbtmud] Alternative WebUI preferences configured.'
fi
