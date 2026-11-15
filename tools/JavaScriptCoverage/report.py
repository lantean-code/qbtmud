import argparse
import bisect
import heapq
import json
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import urlsplit


SCRIPT_PATHS = {
    "boot.resources.js": "src/Lantean.QBTMud/wwwroot/js/boot.resources.js",
    "interop.js": "src/Lantean.QBTMud/wwwroot/js/interop.js",
}


def utf16_length(text):
    return len(text.encode("utf-16-le")) // 2


def executable_positions(source):
    offsets = [0]
    for character in source:
        offsets.append(offsets[-1] + (2 if ord(character) > 0xFFFF else 1))
    line_starts = [0] + [match.end() for match in re.finditer(r"\r\n|[\r\n\u2028\u2029]", source)]
    positions = []

    def emit(index):
        positions.append((offsets[index], bisect.bisect_right(line_starts, index)))

    def skip_quote(index, quote):
        index += 1
        while index < len(source):
            if source[index] == "\\":
                index += 2
            elif source[index] == quote:
                return index + 1
            else:
                index += 1
        return index

    def scan_template(index):
        index += 1
        while index < len(source):
            if source[index] == "\\":
                index += 2
            elif source[index] == "`":
                return index + 1
            elif source.startswith("${", index):
                index = scan_code(index + 2, True)
            else:
                index += 1
        return index

    def regex_end(index):
        index += 1
        in_class = False
        while index < len(source) and source[index] not in "\r\n\u2028\u2029":
            character = source[index]
            if character == "\\":
                index += 2
                continue
            if character == "[":
                in_class = True
            elif character == "]":
                in_class = False
            elif character == "/" and not in_class:
                index += 1
                while index < len(source) and source[index].isalpha():
                    index += 1
                return index
            index += 1
        return None

    def scan_code(index, stop_at_brace=False):
        depth = 0
        expect_expression = True
        while index < len(source):
            character = source[index]
            if source.startswith("//", index):
                match = re.search(r"[\r\n\u2028\u2029]", source[index + 2:])
                index = len(source) if match is None else index + 2 + match.start()
            elif source.startswith("/*", index):
                end = source.find("*/", index + 2)
                index = len(source) if end < 0 else end + 2
            elif character in "\"'":
                emit(index)
                index = skip_quote(index, character)
                expect_expression = False
            elif character == "`":
                emit(index)
                index = scan_template(index)
                expect_expression = False
            elif character == "/" and expect_expression and regex_end(index) is not None:
                emit(index)
                index = regex_end(index)
                expect_expression = False
            elif character.isalnum() or character in "_$":
                end = index + 1
                while end < len(source) and (source[end].isalnum() or source[end] in "_$"):
                    end += 1
                emit(index)
                expect_expression = source[index:end] in {"return", "throw", "case", "delete", "void", "typeof", "new", "in", "instanceof", "yield", "await", "of", "else", "do"}
                index = end
            elif character == "}" and stop_at_brace and depth == 0:
                return index + 1
            else:
                if character == "{":
                    depth += 1
                elif character == "}":
                    depth -= 1
                if not character.isspace() and character not in "{}[]();,":
                    emit(index)
                if not character.isspace():
                    expect_expression = character in "([{,;:=?!&|+-*%/^~<>"
                index += 1
        return index

    scan_code(0)
    return positions


def read_ranges(functions, source_length):
    if not isinstance(functions, list) or not functions:
        raise ValueError("Script coverage must contain native V8 function ranges.")
    ranges = []
    identities = {}
    for function in functions:
        if not isinstance(function, dict):
            raise ValueError("Each V8 function must be an object.")
        function_ranges = function.get("ranges")
        if not isinstance(function_ranges, list) or not function_ranges:
            raise ValueError("Each V8 function must contain ranges.")
        root = function_ranges[0]
        for item in function_ranges:
            if not isinstance(item, dict):
                raise ValueError("Each V8 range must be an object.")
            start = item.get("startOffset")
            end = item.get("endOffset")
            count = item.get("count")
            if not all(type(value) is int for value in (start, end, count)):
                raise ValueError("V8 offsets and counts must be integers.")
            if not 0 <= start < end <= source_length or count < 0:
                raise ValueError("V8 range is outside the UTF-16 source or has a negative count.")
            if start < root["startOffset"] or end > root["endOffset"]:
                raise ValueError("A V8 block range must be inside its function range.")
            ranges.append((start, end, count))
            identity = (root["startOffset"], root["endOffset"], start, end)
            identities[identity] = max(identities.get(identity, 0), count)
    return ranges, identities


def line_counts(positions, ranges):
    ordered = sorted(ranges)
    active = []
    next_range = 0
    counts = {}
    for offset, line in positions:
        while next_range < len(ordered) and ordered[next_range][0] <= offset:
            start, end, count = ordered[next_range]
            heapq.heappush(active, (end - start, -count, end, next_range))
            next_range += 1
        while active and active[0][2] <= offset:
            heapq.heappop(active)
        if active:
            count = -active[0][1]
            counts[line] = max(counts.get(line, 0), count)
        else:
            counts.setdefault(line, 0)
    return counts


def collect_coverage(input_directory, repository_root):
    report_paths = sorted(Path(input_directory).rglob("*.json"))
    if not report_paths:
        raise ValueError("No JavaScript test coverage reports were found.")
    expected = {
        name: (Path(repository_root) / path).read_bytes().decode("utf-8")
        for name, path in SCRIPT_PATHS.items()
    }
    positions = {name: executable_positions(source) for name, source in expected.items()}
    scripts = {}
    for report_path in report_paths:
        payload = json.loads(report_path.read_text(encoding="utf-8"))
        if not isinstance(payload, dict):
            raise ValueError(f"Coverage report must be an object: {report_path}.")
        entries = payload.get("scripts")
        if not isinstance(entries, list) or not entries:
            raise ValueError(f"No scripts were recorded in {report_path}.")
        matched = False
        for entry in entries:
            if not isinstance(entry, dict):
                raise ValueError(f"Script coverage must be an object in {report_path}.")
            name = Path(urlsplit(entry.get("url", "")).path).name
            if name not in expected:
                continue
            matched = True
            if entry.get("source") != expected[name]:
                raise ValueError(f"Stale or mismatched source for {name} in {report_path}.")
            ranges, identities = read_ranges(entry.get("functions"), utf16_length(expected[name]))
            script = scripts.setdefault(name, {"path": SCRIPT_PATHS[name], "lines": {}, "ranges": {}})
            for line, count in line_counts(positions[name], ranges).items():
                script["lines"][line] = max(script["lines"].get(line, 0), count)
            for identity, count in identities.items():
                script["ranges"][identity] = max(script["ranges"].get(identity, 0), count)
        if not matched:
            raise ValueError(f"No application scripts were recorded in {report_path}.")
    missing = set(SCRIPT_PATHS) - set(scripts)
    if missing:
        raise ValueError("Missing script coverage: " + ", ".join(sorted(missing)))
    return scripts, len(report_paths)


def parse_changed_lines(diff):
    changed = {path: set() for path in SCRIPT_PATHS.values()}
    current_path = None
    line = None
    for text in diff.splitlines():
        if text.startswith("diff --git "):
            current_path = None
            line = None
        elif line is None and text.startswith("+++ "):
            current_path = text[6:] if text.startswith("+++ b/") else None
            line = None
        elif text.startswith("@@ "):
            match = re.match(r"@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@", text)
            if match is None:
                raise ValueError("Unable to parse a git diff hunk.")
            line = int(match.group(1))
        elif line is not None and text.startswith("+"):
            if current_path in changed:
                changed[current_path].add(line)
            line += 1
        elif line is not None and text.startswith(" "):
            line += 1
    return changed


def changed_lines(repository_root, base):
    if not base or base.startswith("-"):
        raise ValueError("The diff base must be a git revision, not an option.")
    result = subprocess.run(
        ["git", "diff", "--no-ext-diff", "--no-textconv", "--unified=0", base, "--", *SCRIPT_PATHS.values()],
        cwd=repository_root, check=True, capture_output=True, text=True, encoding="utf-8"
    )
    return parse_changed_lines(result.stdout)


def make_summary(scripts, report_count, changed):
    summary = {"test_reports": report_count, "scripts": {}, "uncovered_changed_lines": {}, "passed": True}
    for name, script in sorted(scripts.items()):
        lines = script["lines"]
        executable_changes = sorted(set(lines) & changed.get(script["path"], set()))
        uncovered = [line for line in executable_changes if lines[line] == 0]
        summary["scripts"][name] = {
            "path": script["path"],
            "lines": {"covered": sum(count > 0 for count in lines.values()), "total": len(lines)},
            "v8_ranges": {"covered": sum(count > 0 for count in script["ranges"].values()), "total": len(script["ranges"])},
            "changed_executable_lines": {"covered": len(executable_changes) - len(uncovered), "total": len(executable_changes)},
        }
        if uncovered:
            summary["uncovered_changed_lines"][script["path"]] = uncovered
            summary["passed"] = False
    return summary


def write_reports(output_directory, scripts, summary):
    output = Path(output_directory)
    output.mkdir(parents=True, exist_ok=True)
    lcov = []
    for name, script in sorted(scripts.items()):
        lines = script["lines"]
        lcov.extend(["TN:", f"SF:{script['path']}"])
        lcov.extend(f"DA:{line},{count}" for line, count in sorted(lines.items()))
        lcov.extend([f"LF:{len(lines)}", f"LH:{sum(count > 0 for count in lines.values())}", "end_of_record"])
    (output / "coverage.lcov").write_text("\n".join(lcov) + "\n", encoding="utf-8")
    (output / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    readable = [f"JavaScript coverage from {summary['test_reports']} test reports"]
    for name, script in summary["scripts"].items():
        lines = script["lines"]
        ranges = script["v8_ranges"]
        changed = script["changed_executable_lines"]
        readable.append(f"{name}: lines {lines['covered']}/{lines['total']}; V8 ranges {ranges['covered']}/{ranges['total']}; changed executable lines {changed['covered']}/{changed['total']}")
    for path, lines in summary["uncovered_changed_lines"].items():
        readable.extend(f"UNCOVERED {path}:{line}" for line in lines)
    readable.append("Changed-line coverage gate: " + ("PASS" if summary["passed"] else "FAIL"))
    text = "\n".join(readable) + "\n"
    (output / "summary.txt").write_text(text, encoding="utf-8")
    return text


def main(arguments=None):
    parser = argparse.ArgumentParser(description="Aggregate native V8 JavaScript coverage and gate changed executable lines.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--base", required=True)
    parser.add_argument("--repo-root", required=True, type=Path)
    options = parser.parse_args(arguments)
    try:
        scripts, count = collect_coverage(options.input, options.repo_root)
        changed = changed_lines(options.repo_root, options.base)
        summary = make_summary(scripts, count, changed)
        print(write_reports(options.output, scripts, summary), end="")
        return 0 if summary["passed"] else 1
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        print(f"JavaScript coverage failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
