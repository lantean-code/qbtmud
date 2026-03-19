#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
webui_port="${WEBUI_PORT:-18080}"
torrenting_port="${TORRENTING_PORT:-23001}"
work_root="${TMPDIR:-/tmp}/qbtmud-readme-screenshots"
publish_root="$work_root/publish"
profile_root="$work_root/profile-root"
download_root="$work_root/downloads"
capture_state="$work_root/capture-state.json"
output_dir="$repo_root/docs/readme-assets"
tool_project="$repo_root/tools/ReadmeScreenshots/ReadmeScreenshots.csproj"
publish_web_root="$publish_root/wwwroot"
alternative_ui_root="$work_root/alternative-ui"
profile_name="readme-screenshots"
profile_config_root="$profile_root/qBittorrent_$profile_name/config"
profile_config_path="$profile_config_root/qBittorrent.conf"
log_root="$work_root/logs"
qbittorrent_log="$log_root/qbittorrent.log"
qbittorrent_username="${QBITTORRENT_USERNAME:-admin}"
qbittorrent_password="${QBITTORRENT_PASSWORD:-adminadmin}"
playwright_version="$(awk -F'\"' '/Microsoft.Playwright/ { print $4; exit }' "$tool_project")"
playwright_node="$HOME/.nuget/packages/microsoft.playwright/$playwright_version/.playwright/node/linux-x64/node"
playwright_cli="$HOME/.nuget/packages/microsoft.playwright/$playwright_version/.playwright/package/cli.js"

rm -rf "$work_root" "$output_dir"
mkdir -p "$profile_config_root" "$output_dir" "$log_root" "$alternative_ui_root/public"

dotnet publish "$repo_root/src/Lantean.QBTMud/Lantean.QBTMud.csproj" -c Release -o "$publish_root" --artifacts-path=/tmp/artifacts/qbtmud
cp -a "$publish_web_root/." "$alternative_ui_root/public/"
dotnet run --project "$tool_project" -- generate-fixtures --repo-root "$repo_root"
password_hash="$(dotnet run --project "$tool_project" -- hash-password --password "$qbittorrent_password")"

if [[ ! -x "$playwright_node" || ! -f "$playwright_cli" ]]; then
    echo "Microsoft.Playwright package assets were not found at $playwright_version." >&2
    exit 1
fi

if ! compgen -G "$HOME/.cache/ms-playwright/chromium-*" >/dev/null; then
    "$playwright_node" "$playwright_cli" install chromium
fi

cat >"$profile_config_path" <<EOF
[Preferences]
WebUI\\Enabled=true
WebUI\\Address=127.0.0.1
WebUI\\Port=$webui_port
WebUI\\Username=$qbittorrent_username
WebUI\\Password_PBKDF2="@ByteArray($password_hash)"
WebUI\\LocalHostAuth=false
WebUI\\AlternativeUIEnabled=true
WebUI\\RootFolder=$alternative_ui_root
WebUI\\HostHeaderValidation=false
WebUI\\CSRFProtection=false
WebUI\\ClickjackingProtection=false
WebUI\\SecureCookie=false
General\\ExitConfirm=false
General\\UseCustomUITheme=false
Search\\SearchEnabled=true

[LegalNotice]
Accepted=true

[BitTorrent]
Session\\DefaultSavePath=$download_root
Session\\QueueingSystemEnabled=true
Session\\RefreshInterval=1500
Session\\Port=$torrenting_port

[Application]
GUI\\Notifications\\TorrentAdded=false
EOF

qbittorrent_pid=""

cleanup() {
    if [[ -n "${qbittorrent_pid:-}" ]]; then
        kill "$qbittorrent_pid" >/dev/null 2>&1 || true
        wait "$qbittorrent_pid" 2>/dev/null || true
    fi
}
trap cleanup EXIT

QT_QPA_PLATFORM=offscreen qbittorrent \
    --profile="$profile_root" \
    --configuration="$profile_name" \
    --webui-port="$webui_port" \
    --torrenting-port="$torrenting_port" \
    --no-splash \
    >"$qbittorrent_log" 2>&1 &
qbittorrent_pid="$!"

dotnet run --project "$tool_project" -- seed --repo-root "$repo_root" --api-base-url "http://127.0.0.1:$webui_port/api/v2/" --download-root "$download_root" --capture-state "$capture_state" --username "$qbittorrent_username" --password "$qbittorrent_password"
dotnet run --project "$tool_project" -- capture --app-base-url "http://127.0.0.1:$webui_port/" --output-dir "$output_dir" --capture-state "$capture_state" --username "$qbittorrent_username" --password "$qbittorrent_password"
