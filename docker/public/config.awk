# Validate QSettings without reserialising unrelated values. Exit 2 means repairable.
function trim(value) {
    sub(/^[[:space:]]+/, "", value)
    sub(/[[:space:]]+$/, "", value)
    return value
}
# Decode ASCII aliases only for identifying managed paths; retain other names verbatim.
function settingsName(value,    result, position, character, first, digits, encoded, number, digit, offset) {
    for (position = 1; position <= length(value);) {
        character = substr(value, position, 1)
        if (character == "\\") {
            result = result "/"
            position++
        } else if (character == "%") {
            first = position + 1
            digits = 2
            if (substr(value, first, 1) == "U") { first++; digits = 4 }
            encoded = substr(value, first, digits)
            if (length(encoded) == digits && encoded !~ /[^0-9a-fA-F]/) {
                number = 0
                for (offset = 1; offset <= digits; offset++) {
                    digit = index("0123456789abcdef", tolower(substr(encoded, offset, 1))) - 1
                    number = number * 16 + digit
                }
                if (number > 0 && number < 128) result = result sprintf("%c", number)
                else result = result substr(value, position, first + digits - position)
                position = first + digits
            } else {
                result = result character
                position++
            }
        } else {
            result = result character
            position++
        }
    }
    return result
}
# A physical line is safe only when Qt would finish the logical record here.
function record(value,    position, character, quoted) {
    for (position = 1; position <= length(value); position++) {
        character = substr(value, position, 1)
        if (character == "\\") {
            if (position == length(value)) { invalid = 1; return "" }
            position++
        } else if (character == "\"") {
            quoted = !quoted
        } else if (character == ";" && !quoted) {
            return substr(value, 1, position - 1)
        }
    }
    if (quoted) { invalid = 1; return "" }
    return value
}
function missing() {
    if (section == managedSection && mode == "write") {
        if (!seenEnabled) printf "%s%s", enabled "=true", ending
        if (!seenRoot) printf "%s%s", root "=/opt/qbtmud", ending
    }
}
BEGIN {
    managedSection = modern ? "WebUI" : "Preferences"
    enabled = modern ? "AlternativeUIEnabled" : "WebUI\\AlternativeUIEnabled"
    root = modern ? "RootFolder" : "WebUI\\RootFolder"
    ending = "\n"
}
{
    raw = $0
    cr = sub(/\r$/, "", raw)
    controls = raw
    gsub(/\t/, "", controls)
    if (controls ~ /[[:cntrl:]]/) { invalid = 1; exit 1 }
    if (NR == 1) ending = cr ? "\r\n" : "\n"
    line = trim(raw)
    if (line != "" && line !~ /^;/) {
        line = trim(record(raw))
        if (invalid) exit 1
    }
    if (line ~ /^\[/) {
        if (line !~ /^\[[^][]+\]$/) { invalid = 1; exit 1 }
        missing()
        sectionName = trim(substr(line, 2, length(line) - 2))
        if (tolower(sectionName) == "general") section = ""
        else if (tolower(sectionName) == "%general") section = substr(sectionName, 2)
        else section = settingsName(sectionName)
        if (section == "Preferences" || section == "WebUI") {
            if (++sections[section] > 1) { invalid = 1; exit 1 }
        }
    } else if (line != "" && line !~ /^[;#]/) {
        separator = index(line, "=")
        if (!separator || trim(substr(line, 1, separator - 1)) == "") {
            invalid = 1; exit 1
        }
        key = trim(substr(line, 1, separator - 1))
        value = trim(substr(line, separator + 1))
        path = (section == "" ? "" : section "/") settingsName(key)
        if (path == "Preferences/WebUI/AlternativeUIEnabled" || path == "Preferences/WebUI/RootFolder" ||
            path == "WebUI/AlternativeUIEnabled" || path == "WebUI/RootFolder") {
            # Aliases outside the canonical section/key are ambiguous: reject without rewriting.
            if (!((section == "Preferences" && (key == "WebUI\\AlternativeUIEnabled" || key == "WebUI\\RootFolder")) ||
                  (section == "WebUI" && (key == "AlternativeUIEnabled" || key == "RootFolder")))) {
                invalid = 1; exit 1
            }
            # Remove only managed keys from the other layout. Legacy values would
            # otherwise overwrite the new values during qBittorrent's migration.
            if (section != managedSection) { changed = 1; next }
            if (key == enabled) { count = ++seenEnabled; expected = "true" }
            else { count = ++seenRoot; expected = "/opt/qbtmud" }
            if (count > 1 || value != expected) changed = 1
            if (mode == "write" && count == 1) {
                printf "%s=%s%s", key, expected, cr ? "\r\n" : "\n"
            }
            next
        }
    }
    if (mode == "write") printf "%s%s", raw, cr ? "\r\n" : "\n"
}
END {
    if (invalid) exit 1
    missing()
    if (!sections[managedSection] && mode == "write") {
        if (NR) printf "%s", ending
        printf "[%s]%s%s=true%s%s=/opt/qbtmud%s", managedSection, ending, enabled, ending, root, ending
    }
    if (mode != "write" && (changed || sections[managedSection] != 1 || seenEnabled != 1 || seenRoot != 1)) exit 2
}
