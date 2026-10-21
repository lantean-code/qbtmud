#!/bin/bash
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
export QBTMUD_CONFIG_PARSER="$root/docker/public/config.awk"
target="$root/docker/public/configure.sh"
workspace=$(mktemp -d)
trap 'rm -rf -- "$workspace"' EXIT
config="$workspace/qBittorrent.conf"
passed=0
check() { "$@" || { echo "FAIL: $*" >&2; exit 1; }; }
repair() { bash "$target" "$config"; bash "$target" "$config" verify; passed=$((passed + 1)); }

printf '[Preferences]\nWebUI\\Password_PBKDF2=@ByteArray(secret==)\nWebUI\\RootFolder=/wrong\nWebUI\\AlternativeUIEnabled=false\nOther=@Variant(\\0\\x1)\n[Other]\nWebUI\\RootFolder=untouched\n' > "$config"
chmod 640 "$config"
repair
check grep -Fx 'WebUI\Password_PBKDF2=@ByteArray(secret==)' "$config"
check grep -Fx 'Other=@Variant(\0\x1)' "$config"
check grep -Fx 'WebUI\RootFolder=untouched' "$config"
check test "$(stat -c '%a' "$config")" = 640
before=$(stat -c '%u:%g:%a:%y' "$config")
cp "$config" "$workspace/expected"
repair
check cmp "$config" "$workspace/expected"
check test "$(stat -c '%u:%g:%a:%y' "$config")" = "$before"

printf '; comment\n[Other]\nValue=a=b\n' > "$config"
repair
check grep -Fx '; comment' "$config"
check grep -Fx 'Value=a=b' "$config"

printf '[Preferences]\nWebUI\\RootFolder=/first\nWebUI\\RootFolder=/second\nWebUI\\AlternativeUIEnabled=false\nWebUI\\AlternativeUIEnabled=true\n' > "$config"
repair
check test "$(grep -Fc 'WebUI\RootFolder=' "$config")" = 1
check test "$(grep -Fc 'WebUI\AlternativeUIEnabled=' "$config")" = 1

printf '[Preferences]\r\nOther=preserved\r\n' > "$config"
repair
check test "$(awk '/\r$/ { count++ } END { print count }' "$config")" = 4

printf '[Preferences]\n  WebUI\\RootFolder = /wrong\n WebUI\\AlternativeUIEnabled = false\n' > "$config"
repair
: > "$config"
repair
printf '[Preferences]\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true' > "$config"
cp "$config" "$workspace/expected"
repair
check cmp "$config" "$workspace/expected"

printf '[ \tPreferences \t]\nWebUI\\RootFolder=/wrong\nWebUI\\AlternativeUIEnabled=false\nOther%%20Name=preserved\n' > "$config"
repair
check grep -Fx 'Other%20Name=preserved' "$config"

canonical='[Preferences]\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\n'
for alias in \
    '[ Preferences ]\nWebUI\\RootFolder=/wrong\n' \
    '[\tPreferences\t]\nWebUI\\RootFolder=/wrong\n' \
    '[%50references]\nWebUI\\RootFolder=/wrong\n' \
    '%57ebUI\\AlternativeUIEnabled=false\n' \
    'WebUI\\%52ootFolder=/wrong\n' \
    '%U0057ebUI\\AlternativeUIEnabled=false\n' \
    'WebUI%2fRootFolder=/wrong\n' \
    'WebUI/RootFolder=/wrong\n' \
    '[Preferences\\WebUI]\nRootFolder=/wrong\n' \
    '[General]\nPreferences\\WebUI\\RootFolder=/wrong\n'; do
    printf '%b' "$canonical$alias" > "$config"
    cp "$config" "$workspace/expected"
    for mode in verify write; do
        if bash "$target" "$config" "$mode"; then echo 'Managed alias was accepted.' >&2; exit 1; fi
        check cmp "$config" "$workspace/expected"
    done
    passed=$((passed + 1))
done

printf '%b' "$canonical" > "$config"
printf '%s\n' 'Other="single line; value with \"quoted\" text"' 'Comment=plain ; "ignored quote' 'Path="C:\\path\\"' >> "$config"
cp "$config" "$workspace/expected"
repair
check cmp "$config" "$workspace/expected"

for multiline in \
    '[Preferences]\nOther="prefix\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\nSuffix=end"\n' \
    '[Preferences]\nOther=prefix\\\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\n' \
    '[Preferences]\n#Other="prefix\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\nSuffix=end"\n' \
    '[Preferences]\nWebUI\\RootFolder=\r/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\n' \
    '\f[Preferences]\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\n' \
    '[Preferences]\n; comment\r[Other]\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\n'; do
    printf '%b' "$multiline" > "$config"
    cp "$config" "$workspace/expected"
    for mode in verify write; do
        if bash "$target" "$config" "$mode"; then echo 'Multiline record was accepted.' >&2; exit 1; fi
        check cmp "$config" "$workspace/expected"
    done
    passed=$((passed + 1))
done

for input in '[Preferences]\ninvalid\n' '[Preferences]\n[Preferences]\n' '[broken\n'; do
    printf '%b' "$input" > "$config"
    cp "$config" "$workspace/expected"
    if bash "$target" "$config"; then echo 'Malformed input was accepted.' >&2; exit 1; fi
    check cmp "$config" "$workspace/expected"
    passed=$((passed + 1))
done
printf '[Preferences]\n' > "$config"
cp "$config" "$workspace/expected"
chmod 440 "$config"
if bash "$target" "$config"; then echo 'Read-only input was accepted.' >&2; exit 1; fi
check cmp "$config" "$workspace/expected"
chmod 640 "$config"
ln -s "$config" "$workspace/link"
if bash "$target" "$workspace/link"; then echo 'Symbolic link was accepted.' >&2; exit 1; fi
if bash "$target" "$workspace/missing"; then echo 'Missing input was accepted.' >&2; exit 1; fi
echo "Passed $passed configuration scenarios plus read-only, symlink and missing-file checks."
